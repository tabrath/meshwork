//
// TCPDestination.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2008 FileFind.net (http://filefind.net)
//

using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using FileFind.Meshwork.Transport;

namespace FileFind.Meshwork.Destination
{

	public class TCPIPv6DestinationSource : TCPIPDestinationSource
	{
		public override Type DestinationType
        {
            get { return typeof(TCPIPv6Destination); }
		}

        // We piggyback on TCPIPv4DestinationSource's listener.
        public override Type ListenerType
        {
            get { return null; }
		}

        public TCPIPv6DestinationSource()
            : base(AddressFamily.InterNetworkV6)
        {
        }

        public override IDestination CreateDestination(InterfaceAddress nic, int port, bool isOpenExternally)
        {
            var address = nic.Address;
            address.ScopeId = 0;

            return new TCPIPv6Destination(nic.IPv6PrefixLength, address, (uint)port, isOpenExternally);
        }
	}
	
}
