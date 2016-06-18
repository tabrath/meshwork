//
// IPDestination.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2008 FileFind.net (http://filefind.net)
//

using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using FileFind.Meshwork;
using FileFind.Meshwork.Transport;

namespace FileFind.Meshwork.Destination
{
	public abstract class IPv6Destination : IPDestination
	{
        public string NetworkPrefix
        {
            get { return IPv6Util.GetNetworkPrefix(PrefixLength, Address); }
        }

        public int PrefixLength { get; } = 0;

        public override bool CanConnect
        {
            get
            {
                if (!Common.SupportsIPv6)
                    return false;

                if (IsExternal)
                    return IsOpenExternally && Common.HasExternalIPv6;

                // Can't connect to link-local addresses if no interface is set.
                if (Core.Settings.IPv6LinkLocalInterfaceIndex == -1)
                    return false;

                var externals = Core.DestinationManager.Destinations.Where(d => d.IsExternal);

                // If this is an IPv6 address, we can connect only if
                // one of our Destinations is also IPv6 and has the
                // same network prefix (excluding link-local).
                if (externals.OfType<IPv6Destination>().Any(d => d.NetworkPrefix == NetworkPrefix))
                    return true;

                // In many (most?) cases, two nodes on the same LAN will
                // have only link-local IPv6 addresses. If we have a
                // matching external IPv4 address (i.e., we are behind
                // the same IPv4 NAT router), then we can connect.
                if (externals.OfType<IPv4Destination>().Any(d => parentList.OfType<IPv4Destination>().Any(p => p.IsExternal && d.Address.Equals(p.Address))))
                    return true;

                return false;
            }
        }

        protected IPv6Destination(int prefixLength, IPAddress address, uint port, bool isOpenExternally)
            : base(address, port, isOpenExternally)
		{
			if (address.AddressFamily != AddressFamily.InterNetworkV6)
				throw new ArgumentException("ip must be IPv6");

			PrefixLength = prefixLength;
		}

		protected IPv6Destination()
            : base()
		{
		}

        public override DestinationInfo CreateDestinationInfo()
        {
            return new DestinationInfo
            {
                IsOpenExternally = IsOpenExternally,
                TypeName = GetType().ToString(),
                Data = new string[] { Address.ToString(), PrefixLength.ToString(), Port.ToString() }
            };
		}
		
		public override int CompareTo(IDestination other)
		{
			if (other is IPv6Destination)
            {
				// Prefer internal IP addresses
				if (IsExternal && !other.IsExternal)
					return 1;

                if (!IsExternal && other.IsExternal)
					return -1;
			}
            else if (other is IPv4Destination)
            {
				// Prefer IPv6 to IPv4
				return 1;
			}
			return 0;
		}
	}
	
}
