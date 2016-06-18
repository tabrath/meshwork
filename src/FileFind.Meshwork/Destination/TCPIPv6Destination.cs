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
using System.Linq;
using Meshwork.Destination;

namespace FileFind.Meshwork.Destination
{
    [Destination(Name = "TCP (IPv6)", Socket = SocketType.Stream, Protocol = ProtocolType.Tcp, Family = AddressFamily.InterNetworkV6)]
	public class TCPIPv6Destination : IPv6Destination
	{
		public TCPIPv6Destination(DestinationInfo info)
            : base(Int32.Parse(info.Data[1]), IPAddress.Parse(info.Data[0]), UInt32.Parse(info.Data[2]), false)
		{
			if (IsExternal)
				IsOpenExternally = info.IsOpenExternally;
		}

        public TCPIPv6Destination(int prefixLength, IPAddress address, uint port, bool isOpenExternally)
            : base(prefixLength, address, port, isOpenExternally)
		{
		}

		public override ITransport CreateTransport(ulong connectionType)
		{
			return new TcpTransport(Address, (int)Port, connectionType);
		}

		public override string ToString()
		{
			return string.Format("[{0}/{1}]:{2}", Address, PrefixLength, Port);
		}
	}
	
}
