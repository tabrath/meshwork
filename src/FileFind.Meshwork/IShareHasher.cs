//
// ShareHasher.cs: Hashes files in the user's share
//
// Author:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2007 FileFind.net (http://filefind.net)
//

using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using MonoTorrent.Common;
using FileFind.Meshwork.Filesystem;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.ComponentModel.Composition;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading.Tasks.Schedulers;
using System.Collections.ObjectModel;

/* TODO
 * 
 * Replace Started/Finished/HashingFile events with just "Changed"
 * 
 */

namespace FileFind.Meshwork
{
    public interface IShareHasher
    {
        event EventHandler QueueChanged;
        event EventHandler<FilenameEventArgs> StartedHashingFile;
        event EventHandler<FilenameEventArgs> FinishedHashingFile;

        bool Going { get; }
        int FilesRemaining { get; }
        int CurrentFileCount { get; }
        IEnumerable<string> CurrentFiles { get; }

        Task HashFile(LocalFile file);
        void Start();
        void Stop();
    }

    //TODO: DataFlow or TaskFactory/Scheduler? ConcurrentBag<string> for current tasks?
    
}