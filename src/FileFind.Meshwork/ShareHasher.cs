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
using Meshwork.Logging;

/* TODO
 * 
 * Replace Started/Finished/HashingFile events with just "Changed"
 * 
 */

namespace FileFind.Meshwork
{
    [Export(typeof(IShareHasher)), PartCreationPolicy(CreationPolicy.Shared)]
    internal class ShareHasher : IShareHasher
    {
        // Keeps track of worker threads and what their current task is, if any.
        private readonly ConcurrentDictionary<Thread, TaskCompletionSource<bool>> threads;
        private BlockingCollection<TaskCompletionSource<bool>> queue;
        private CancellationTokenSource cancellation;
        private readonly int threadCount;
        private readonly ILoggingService loggingService;

        public event EventHandler QueueChanged;
        public event EventHandler<FilenameEventArgs> StartedHashingFile;
        public event EventHandler<FilenameEventArgs> FinishedHashingFile;

        public int FilesRemaining
        {
            get { return this.queue.Count; }
        }

        public bool Going
        {
            get { return (!this.queue.IsCompleted && this.queue.Count > 0 && threads.Count > 0); }
        }

        public int CurrentFileCount
        {
            get { return threads.Where(t => t.Value != null && !t.Value.Task.IsCompleted).Count(); }
        }

        public IEnumerable<string> CurrentFiles
        {
            get { return threads.Where(t => t.Value != null).Select(t => ((LocalFile)t.Value.Task.AsyncState).LocalPath).ToArray(); }
        }

        [ImportingConstructor]
        public ShareHasher(ILoggingService loggingService)
        {
            this.loggingService = loggingService;
            this.threadCount = System.Environment.ProcessorCount;
            this.threads = new ConcurrentDictionary<Thread, TaskCompletionSource<bool>>();
            this.queue = new BlockingCollection<TaskCompletionSource<bool>>();
            this.cancellation = new CancellationTokenSource();

        }

        public Task HashFile(LocalFile file)
        {
            if (file.LocalPath == null)
                throw new ArgumentNullException(nameof(file));

            if (!System.IO.File.Exists(file.LocalPath))
                throw new ArgumentException("File does not exist");

            if (this.queue.Any(t => ((LocalFile)t.Task.AsyncState).LocalPath == file.LocalPath))
                throw new InvalidOperationException("File is already in queue");

            var tcs = new TaskCompletionSource<bool>(state: file);
            if (this.queue.TryAdd(tcs, 1000, this.cancellation.Token))
            {
                QueueChanged?.Invoke(this, EventArgs.Empty);
            }

            Start();

            return tcs.Task;
        }

        public void Start()
        {
            while (threads.Count < threadCount)
            {
                Thread thread = new Thread(DoHashing) { IsBackground = true };
                thread.Start();
                threads.TryAdd(thread, null);
            }
        }

        public void Stop()
        {
            if (this.cancellation != null)
            {
                this.cancellation.Dispose();
                this.cancellation = null;
            }

            if (this.queue != null)
            {
                this.queue.Dispose();
                this.queue = null;
            }

            foreach (Thread thread in threads.Keys)
            {
                thread.Abort();
            }
            threads.Clear();
        }

        private void DoHashing()
        {
            try
            {
                TaskCompletionSource<bool> task;
                while (!this.cancellation.IsCancellationRequested && this.queue.TryTake(out task, -1, this.cancellation.Token))
                {
                    threads[Thread.CurrentThread] = task;
                    QueueChanged?.Invoke(this, EventArgs.Empty);

                    if (!task.Task.IsCanceled && !task.Task.IsCompleted)
                    {
                        try
                        {
                            Hash(task);
                        }
                        catch (ThreadAbortException)
                        {
                            this.loggingService.LogInfo("Aborting hashing of file.");
                        }
                        catch (Exception ex)
                        {
                            // XXX: Do something here!
                            this.loggingService.LogError("Problem while hashing file.", ex);
                        }
                    }

                    threads[Thread.CurrentThread] = null;
                }
            }
            catch (ThreadAbortException)
            {
                // Someone called Stop(), that's OK.

            }
            catch (Exception ex)
            {
                // XXX: Do something here, we've aborted
                // everything!
                this.loggingService.LogError("AAHHHH!!!", ex);
                Stop();
            }
        }

        private void Hash(TaskCompletionSource<bool> task)
        {
            var file = (LocalFile)task.Task.AsyncState;

            this.loggingService.LogDebug("trying to hash " + file.FullPath);

            StartedHashingFile?.Invoke(this, new FilenameEventArgs(file.FullPath));

            try
            {
                /* Create the torrent */
                TorrentCreator creator = new TorrentCreator();
                // Have to put something bogus here, otherwise MonoTorrent crashes!
                creator.Announces.Add(new MonoTorrentCollection<string>());
                creator.Announces[0].Add(String.Empty);

                creator.Path = file.LocalPath;
                Torrent torrent = Torrent.Load(creator.Create());

                /* Update the database */
                string[] pieces = new string[torrent.Pieces.Count];
                for (int x = 0; x < torrent.Pieces.Count; x++)
                {
                    byte[] hash = torrent.Pieces.ReadHash(x);
                    pieces[x] = Common.BytesToString(hash);
                }

                file.Update(Common.BytesToString(torrent.InfoHash.ToArray()),
                                 Common.BytesToString(torrent.Files[0].SHA1),
                             torrent.PieceLength, pieces);

                FinishedHashingFile?.Invoke(this, new FilenameEventArgs(file.FullPath));

                task.TrySetResult(true);
            }
            catch (OperationCanceledException)
            {
                task.TrySetCanceled();
            }
            catch (Exception e)
            {
                task.TrySetException(e);
            }
		}
	}
}