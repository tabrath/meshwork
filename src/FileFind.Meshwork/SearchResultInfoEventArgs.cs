//
// EventArgs.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006-2008 FileFind.net (http://filefind.net)
//

using System;
using FileFind.Meshwork.Protocol;

namespace FileFind.Meshwork
{
    public class SearchResultInfoEventArgs : EventArgs
	{
        public Node Node { get; }
        public SearchResultInfo Info { get; }

        public SearchResultInfoEventArgs(Node node, SearchResultInfo info)
            : base()
		{
			Node = node;
			Info = info;
		}
	}
}
