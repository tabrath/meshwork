//
// FileSearchManager.cs: Keeps track of active file searches.
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2008 FileFind.net (http://filefind.net)
//

using System;
using FileFind.Meshwork;
using FileFind.Meshwork.Protocol;
using System.Collections.Generic;
using System.Linq;

namespace FileFind.Meshwork.Search
{
    public class FileSearchEventArgs : EventArgs
    {
        public FileSearch Search { get; }

        public FileSearchEventArgs(FileSearch search)
            : base()
        {
            Search = search;
        }
    }
	
}
