//
// IPDestination.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2008 FileFind.net (http://filefind.net)
//

using System;
using System.Net;
using System.Net.Sockets;
using FileFind.Meshwork;
using FileFind.Meshwork.Transport;

namespace FileFind.Meshwork.Destination
{
	public abstract class IPDestination : DestinationBase
	{
        public IPAddress Address { get; }
        public uint Port { get; }

        public override bool IsExternal
        {
            get { return !Address.IsInternalIP(); }
        }

        protected IPDestination(IPAddress address, uint port, bool isOpenExternally)
            : base(isOpenExternally)
		{
            Address = address;
			Port = port;
		}

		protected IPDestination()
            : base(false)
		{
            Address = IPAddress.None;
            Port = 0;
		}
	}
}
