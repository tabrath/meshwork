//
// EventArgs.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006-2008 FileFind.net (http://filefind.net)
//

using System;
using FileFind.Meshwork.Filesystem;
using FileFind.Meshwork.Protocol;

namespace FileFind.Meshwork
{
    public class ReceivedKeyEventArgs : EventArgs
	{
        public Node Node { get; }
        public KeyInfo Key { get; }
        public LocalNodeConnection Connection { get; }

        public ReceivedKeyEventArgs(Node node, KeyInfo keyInfo)
            : base()
		{
			Node = node;
			Key = keyInfo;
		}
		
		public ReceivedKeyEventArgs(LocalNodeConnection connection, KeyInfo keyInfo)
            : base()
		{
			Connection = connection;
			Key = keyInfo;
		}		
	}
}
