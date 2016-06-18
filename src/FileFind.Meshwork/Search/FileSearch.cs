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
using System.Collections.ObjectModel;

namespace FileFind.Meshwork.Search
{
	public class FileSearch
	{
		private string query;
		[NonSerialized] List<SearchResult> results;
		[NonSerialized] Dictionary<string, List<SearchResult>> allFileResults;

        public event EventHandler<SearchResultsEventArgs> NewResults;
		public event EventHandler ClearedResults;

        public string Name { get; set; }

        public string Query
        {
            get { return this.query; }
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentNullException();

                this.query = value.ToLower();
            }
        }

        public int Id { get; private set; }
        public bool FiltersEnabled { get; set; }
        public List<FileSearchFilter> Filters { get; }
        public List<string> NetworkIds { get; }

        [XmlIgnore]
        public IEnumerable<SearchResult> Results
        {
            get { return this.results.ToArray(); }
        }

        [XmlIgnore]
        public IReadOnlyDictionary<string, List<SearchResult>> AllFileResults
        {
            // XXX: Can we make the List<SearchResult> readonly too?
            get { return new ReadOnlyDictionary<string, List<SearchResult>>(this.allFileResults); }
        }

		public FileSearch()
		{
			Filters = new List<FileSearchFilter>();
			NetworkIds = new List<string>();
            Id = new Random().Next();

            this.results = new List<SearchResult>();
			this.allFileResults = new Dictionary<string, List<SearchResult>>();
		}
		
		public void Repeat()
		{
			Id = new Random().Next();
			
            this.results.Clear();
			this.allFileResults.Clear();

            ClearedResults?.Invoke(this, EventArgs.Empty);

            Core.Networks
                .Where(n => NetworkIds.Count == 0 || NetworkIds.IndexOf(n.NetworkID) > -1).ToList()
                .ForEach(n => n.FileSearch(this));
		}

		public void AppendResults(Node node, SearchResultInfo resultInfo)
		{
			if (resultInfo.SearchId != Id)
				throw new ArgumentException("Results are for a different search.");


            var dirs = resultInfo.Directories.Select(d => new SearchResult(this, node, d));
            var files = resultInfo.Files.Select(f => new SearchResult(this, node, f)).ToList();
            var newResults = dirs.Concat(files).ToList();
            this.results.AddRange(newResults);

            files.ForEach(f =>
            {
                if (!this.allFileResults.ContainsKey(f.InfoHash))
                    this.allFileResults.Add(f.InfoHash, new List<SearchResult>());

                this.allFileResults[f.InfoHash].Add(f);
            });

            NewResults?.Invoke(this, new SearchResultsEventArgs(newResults));
		}

		public bool CheckAllFilters(SearchResult result)
		{
            return Filters.All(f => f.Check(result));
		}
	}
}
