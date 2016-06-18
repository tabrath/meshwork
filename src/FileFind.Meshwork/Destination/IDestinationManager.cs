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
    public interface IDestinationManager
    {
        IDestinationSource[] Sources { get; }
        IDestination[] Destinations { get; }
        DestinationInfo[] DestinationInfos { get; }
        
        void RegisterSource(IDestinationSource source);
        void UnregisterSource(IDestinationSource source);
        bool SupportsDestinationType(string typeName);
        void SyncFromSettings();
    }
    
}
