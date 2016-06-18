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
using Meshwork.Destination;

namespace FileFind.Meshwork.Destination
{
	[Serializable]
	public class DestinationInfo
	{
        public string TypeName { get; set; }
        public string[] Data { get; set; }

		[XmlIgnore]
        public bool Local { get; set; }

        public bool IsOpenExternally { get; set; }

		[XmlIgnore]
		public bool Supported
        {
            get { return Core.DestinationManager.SupportsDestinationType(TypeName); }
		}

        public string FriendlyName
        {
            get { return Type.GetType(TypeName).GetCustomAttribute<DestinationAttribute>().Name; }
        }

		public IDestination CreateDestination()
		{
			if (!Local)
				throw new InvalidOperationException("May not call CreateDestination() on non-local DestinationInfo. Use CreateAndAddDestination() instead.");
			
			return (IDestination)Activator.CreateInstance(Type.GetType(TypeName), new object[] { this });
		}

		public IDestination CreateAndAddDestination(List<IDestination> parentList)
		{
			var destination = (IDestination)Activator.CreateInstance(Type.GetType(TypeName), new object[] { this });

			((DestinationBase)destination).ParentList = parentList.AsReadOnly();
			parentList.Add(destination);
			
			return destination;
		}
	}
	
}
