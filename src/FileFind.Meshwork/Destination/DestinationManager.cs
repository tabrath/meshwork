//
// DestinationManager.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2008 FileFind.net (http://filefind.net)
//

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace FileFind.Meshwork.Destination
{
    [Export(typeof(IDestinationManager)), PartCreationPolicy(CreationPolicy.Shared)]
    internal class DestinationManager : IDestinationManager
	{
        internal static IDestination[] GetConnectableDestinations(IDestination[] destinations)
        {
            return destinations.Where(d => d.CanConnect)
                               .OrderByDescending(d => d)
                               .ToArray();
        }

        /// <summary>Returns a list of all local destinations.</summary>
        public IDestination[] Destinations
        {
            get
            {
                return this.sources.Values
                           .SelectMany(source => source.Destinations)
                           .Concat(this.destinationsFromSettings.Values)
                           .ToArray();
            }
        }

        public DestinationInfo[] DestinationInfos
        {
            get { return Destinations.Select(d => d.CreateDestinationInfo()).ToArray(); }
        }

        public IDestinationSource[] Sources
        {
            get { return sources.Values.ToArray(); }
        }

		private readonly Dictionary<DestinationInfo, IDestination> destinationsFromSettings;
		private readonly Dictionary<string, IDestinationSource> sources;

        [ImportingConstructor]
		public DestinationManager()
		{
			this.sources = new Dictionary<string, IDestinationSource>();
			this.destinationsFromSettings = new Dictionary<DestinationInfo, IDestination>();

			SyncFromSettings();
		}

		public void RegisterSource(IDestinationSource source)
		{
			this.sources[source.DestinationType.ToString()] = source;

			source.DestinationAdded += SourceDestinationAdded;
			source.DestinationRemoved += SourceDestinationRemoved;
            source.Destinations.ToList().ForEach(d => SourceDestinationAdded(source, new DestinationEventArgs(d)));
		}
		
		public void UnregisterSource(IDestinationSource source)
		{
			source.DestinationAdded -= SourceDestinationAdded;
			source.DestinationRemoved -= SourceDestinationRemoved;
            source.Destinations.ToList().ForEach(d => SourceDestinationAdded(source, new DestinationEventArgs(d)));

            this.sources.Remove(source.DestinationType.ToString());
		}

		public bool SupportsDestinationType(string typeName)
		{
			return this.sources.ContainsKey(typeName);
		}

		public void SyncFromSettings()
		{
			// Remove old destinations
            this.destinationsFromSettings.Where(pair => !Core.Settings.SavedDestinationInfos.Contains(pair.Key)).ToList()
                                         .ForEach(pair => this.destinationsFromSettings.Remove(pair.Key));

            // Add new destinations
            Core.Settings.SavedDestinationInfos.Where(info => !this.destinationsFromSettings.ContainsKey(info)).ToList()
                .ForEach(info =>
                {
                    info.Local = true;
                    this.destinationsFromSettings[info] = info.CreateDestination();
                });
		}

        private void SourceDestinationAdded(object sender, DestinationEventArgs e) { }
        private void SourceDestinationRemoved(object sender, DestinationEventArgs e) { }
	}
}
