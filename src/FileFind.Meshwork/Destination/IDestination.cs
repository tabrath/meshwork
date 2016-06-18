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
	public interface IDestination : IComparable<IDestination>
	{
        bool IsExternal { get; }
        bool CanConnect { get; }
        bool IsOpenExternally { get; }
        IList<IDestination> ParentList { get; }
        string FriendlyTypeName { get; }
      
        DestinationInfo CreateDestinationInfo();
		ITransport CreateTransport(ulong connectionType);
	}
}
