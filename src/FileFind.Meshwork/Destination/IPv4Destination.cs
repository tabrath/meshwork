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

namespace FileFind.Meshwork.Destination
{
    public abstract class IPv4Destination : IPDestination
	{
        public override bool CanConnect
        {
            get
            {
                if (IsExternal)
                    return IsOpenExternally;

                var destinations = Core.DestinationManager.Destinations.OfType<IPv4Destination>();
                var locals = destinations.Where(d => !d.IsExternal);

                // Make sure we don't also have this (local) address.
                if (locals.Any(d => d.Address.Equals(Address)))
                    return false;

                // Only connect to local IPs that fall under a matching subnet.
                if (!locals.Any(d => d.Address.IsInSameSubnet(Address, FindInterfaceWithIP(d.Address).SubnetMask)))
                    return false;

                // If this is an IPv4 address, we can connect only
                // if one of our external Destinations matches another one
                // of their (external) Destinations. This means that we both
                // have the same external IP address (both are behind the
                // same NAT router). Under certain situations, a NAT'd
                // network may have multiple public IP Addresses. We do not
                // currently support this case.
                // Multiple interfaces with private addresses are not currently well supported either.

                return destinations.Where(d => d.IsExternal).Any(d => parentList.OfType<IPv4Destination>().Any(p => p.IsExternal && d.Address.Equals(p.Address)));
            }
        }

        protected IPv4Destination(IPAddress address, uint port, bool isOpenExternally)
            : base(address, port, isOpenExternally)
		{
            if (address.AddressFamily != AddressFamily.InterNetwork)
                throw new ArgumentException(String.Format("ip must be IPv4 (was {0})", address));
		}

		protected IPv4Destination()
            : base()
		{
		}
	
		private InterfaceAddress FindInterfaceWithIP(IPAddress ip)
		{
            var address = Core.OS.GetInterfaceAddresses().SingleOrDefault(x => x.Equals(ip));
            if (address == null)
                throw new Exception("No interface found with address " + ip.ToString());

            return address;
		}

        public override DestinationInfo CreateDestinationInfo()
        {
            return new DestinationInfo
            {
                IsOpenExternally = IsOpenExternally,
                TypeName = GetType().ToString(),
                Data = new string[] { Address.ToString(), Port.ToString() }
            };
		}
		
		public override int CompareTo(IDestination other)
		{
			if (other is IPv4Destination)
            {
				// Prefer internal IP addresses
				if (IsExternal && !other.IsExternal)
					return 1;

                if (!IsExternal && other.IsExternal)
					return -1;
			}
            else if (other is IPv6Destination)
            {
				// Prefer IPv6 to IPv4
				return -1;
			}
			return 0;
		}
	}
	
}
