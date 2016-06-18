//
// FileSearch.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2007 FileFind.net (http://filefind.net)
//

using System;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Collections;
using FileFind.Collections;
using FileFind.Meshwork.Protocol;
using FileFind.Meshwork.Filesystem;
using System.Linq;

namespace FileFind.Meshwork.Search
{
    public class SearchResultsEventArgs : EventArgs
    {
        public IEnumerable<SearchResult> Results { get; }

        public SearchResultsEventArgs(IEnumerable<SearchResult> results)
            : base()
        {
            Results = results;
        }
    }
	
}
