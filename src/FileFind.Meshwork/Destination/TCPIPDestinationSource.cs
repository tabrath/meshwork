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

namespace FileFind.Meshwork.Destination
{
	public abstract class TCPIPDestinationSource : IDestinationSource
	{
		List<IDestination> destinations = new List<IDestination>();

        public event EventHandler<DestinationEventArgs> DestinationAdded;
		public event EventHandler<DestinationEventArgs> DestinationRemoved;

		public IList<IDestination> Destinations {
			get { 
				return destinations.AsReadOnly();
			}
		}

        public abstract Type DestinationType { get; }
        public abstract Type ListenerType { get; }

        public int ListenPort { get; set; } = TcpTransport.DefaultPort;

        protected TCPIPDestinationSource(AddressFamily addressFamily)
		{
			ListenPort = Core.Settings.TcpListenPort;

			// XXX: Use NetworkManager to support IP changes,
			// etc. without restarting Meshwork.

            foreach (InterfaceAddress address in Core.OS.GetInterfaceAddresses()
                     .Where(i => !IPAddress.IsLoopback(i.Address) && i.Address.AddressFamily == addressFamily)) {

                IDestination destination = CreateDestination(address, ListenPort, address.Address.IsInternalIP());

				destinations.Add(destination);

                DestinationAdded?.Invoke(this, new DestinationEventArgs(destination));
			}
		}

        public virtual void Update()
		{
			throw new NotImplementedException();
		}

        public abstract IDestination CreateDestination(InterfaceAddress nic, int port, bool isOpenExternally);
	}
}
