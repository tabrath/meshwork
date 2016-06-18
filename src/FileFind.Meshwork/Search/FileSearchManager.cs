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
using Meshwork.Logging;

namespace FileFind.Meshwork.Search
{
    [Export(typeof(IFileSearchManager)), PartCreationPolicy(CreationPolicy.Shared)]
    internal class FileSearchManager : IFileSearchManager
	{
		private readonly List<FileSearch> fileSearches = new List<FileSearch>();
        private readonly ILoggingService loggingService;

		public event EventHandler<FileSearchEventArgs> SearchAdded;
		public event EventHandler<FileSearchEventArgs> SearchRemoved;

        [ImportingConstructor]
        public FileSearchManager(ILoggingService loggingService)
		{
            this.loggingService = loggingService;

			Core.NetworkAdded   += Core_NetworkAdded;
			Core.NetworkRemoved += Core_NetworkRemoved;
		}

		public FileSearch NewFileSearch(string query, string networkId)
		{
            var search = new FileSearch { Name = query, Query = query };

            if (!string.IsNullOrEmpty(networkId))
				search.NetworkIds.Add(networkId);
			
			AddFileSearch(search);

			return search;
		}

		public void AddFileSearch(FileSearch search)
		{
			fileSearches.Add(search);

            SearchAdded?.Invoke(this, new FileSearchEventArgs(search));

            Core.Networks.Where(n => search.NetworkIds.Count == 0 || search.NetworkIds.IndexOf(n.NetworkID) > -1)
                .ToList()
                .ForEach(n => n.FileSearch(search));
		}

		public void RemoveFileSearch (FileSearch search)
		{
			if (!fileSearches.Contains(search))
                throw new InvalidOperationException("Search is not known.");

            fileSearches.Remove(search);
            SearchRemoved?.Invoke(this, new FileSearchEventArgs(search));
		}

		private void Core_NetworkAdded (Network network)
		{
			network.ReceivedSearchResult += network_ReceivedSearchResult;
		}

		private void Core_NetworkRemoved (Network network)
		{
			network.ReceivedSearchResult -= network_ReceivedSearchResult;
		}

		private void network_ReceivedSearchResult(Network network, SearchResultInfoEventArgs args)
		{
            var search = this.fileSearches.SingleOrDefault(s => s.Id == args.Info.SearchId);
            if (search != null)
                search.AppendResults(args.Node, args.Info);
            else
    			this.loggingService.LogWarning("Unexpected search reply.");
		}
	}
}
