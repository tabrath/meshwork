//
// IDestination.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2008 FileFind.net (http://filefind.net)
//

using System;
using System.Reflection;
using System.Net;
using FileFind.Meshwork.Transport;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace FileFind.Meshwork.Destination
{
	public interface IDestinationSource 
	{
		event EventHandler<DestinationEventArgs> DestinationAdded;
		event EventHandler<DestinationEventArgs> DestinationRemoved;

        Type DestinationType { get; }
        Type ListenerType { get; }
        IList<IDestination> Destinations { get; }

        void Update();
        IDestination CreateDestination(InterfaceAddress nic, int port, bool isOpenExternally);
	}
}
