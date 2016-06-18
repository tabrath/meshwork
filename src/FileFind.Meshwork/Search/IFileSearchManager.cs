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
using System.ComponentModel.Composition;

namespace FileFind.Meshwork.Search
{
    public interface IFileSearchManager
    {
        event EventHandler<FileSearchEventArgs> SearchAdded;
        event EventHandler<FileSearchEventArgs> SearchRemoved;

        FileSearch NewFileSearch(string query, string networkId);
        void AddFileSearch(FileSearch search);
        void RemoveFileSearch(FileSearch search);
    }
    
}
