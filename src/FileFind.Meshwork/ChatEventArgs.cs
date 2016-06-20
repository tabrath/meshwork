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
    public class ChatEventArgs : EventArgs
	{
        public Node Node { get; }
        public ChatRoom Room { get; }

        public ChatEventArgs(Node node, ChatRoom room)
            : base()
		{
			Node = node;
			Room = room;
		}
	}
}
