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
	public abstract class DestinationBase : IDestination
	{
		public abstract ITransport CreateTransport(ulong connectionType);
		public abstract DestinationInfo CreateDestinationInfo();
        public abstract bool IsExternal { get; }
        public abstract bool CanConnect { get; }

        public bool IsOpenExternally { get; protected set; } = false;

		protected IList<IDestination> parentList;

		public IList<IDestination> ParentList {
			get {
				return parentList;
			}
			internal set {
				parentList = value;
			}
		}

		public string FriendlyTypeName
        {
            get { return GetType().GetCustomAttribute<DestinationAttribute>().Name; }
		}
		
        protected DestinationBase(bool isOpenExternally)
        {
            IsOpenExternally = isOpenExternally;
        }

		public abstract int CompareTo(IDestination other);
	}
	
}
