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
    [Destination(Name = "TCP (IPv4)", Socket = SocketType.Stream, Protocol = ProtocolType.Tcp, Family = AddressFamily.InterNetwork)]
	public class TCPIPv4Destination : IPv4Destination
	{
		public TCPIPv4Destination(DestinationInfo info)
            : base(IPAddress.Parse(info.Data[0]), UInt32.Parse(info.Data[1]), false)
		{
			if (IsExternal)
				IsOpenExternally = info.IsOpenExternally;
		}

        public TCPIPv4Destination(IPAddress address, uint port, bool isOpenExternally)
            : base(address, port, isOpenExternally)
		{
		}

		public override ITransport CreateTransport(ulong connectionType)
		{
			return new TcpTransport(Address, (int)Port, connectionType);
		}

		public override string ToString()
		{
			return string.Format("{0}:{1}", Address, Port);
		}
	}
}
