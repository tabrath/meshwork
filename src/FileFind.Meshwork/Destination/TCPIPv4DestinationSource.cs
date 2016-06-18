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
	public class TCPIPv4DestinationSource : TCPIPDestinationSource
	{
		public override Type DestinationType
        { 
            get { return typeof(TCPIPv4Destination); }
		}

		public override Type ListenerType
        {
            get { return typeof(TcpTransportListener); }
		}

        public TCPIPv4DestinationSource()
            : base(AddressFamily.InterNetwork)
        {
        }

        public override IDestination CreateDestination(InterfaceAddress nic, int port, bool isOpenExternally)
        {
            return new TCPIPv4Destination(nic.Address, (uint)port, isOpenExternally);
        }
	}
	
}
