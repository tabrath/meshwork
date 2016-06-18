//
// ShareBuilder.cs: Index shared directories
//
// Author:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2007 FileFind.net (http://filefind.net)
//

using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using IO = System.IO;
using System.Threading;
using FileFind.Meshwork.Filesystem;
using System.Data;
using System.ComponentModel.Composition;
using System.Collections.Concurrent;
using Meshwork.Logging;

namespace FileFind.Meshwork
{
    [Export(typeof(IShareBuilder)), PartCreationPolicy(CreationPolicy.Shared)]
    internal class ShareBuilder : IShareBuilder
	{
		private Thread thread = null;
        private readonly IShareHasher hasher;
        private readonly ILoggingService loggingService;
        private BlockingCollection<QueueItem> queue;
        private CancellationTokenSource cancellation;

        private struct QueueItem
        {
            public LocalDirectory Parent { get; }
            public IO.DirectoryInfo Directory { get; }

            public QueueItem(LocalDirectory parent, IO.DirectoryInfo directory)
            {
                Parent = parent;
                Directory = directory;
            }
        }

		public event EventHandler StartedIndexing;
		public event EventHandler FinishedIndexing;
		public event EventHandler StoppedIndexing;
		public event EventHandler<FilenameEventArgs> IndexingFile;
		public event EventHandler<ErrorEventArgs> ErrorIndexing;

        public bool Going
        {
            get { return thread != null; }
        }

        [ImportingConstructor]
        public ShareBuilder(IShareHasher hasher, ILoggingService loggingService)
		{
            this.hasher = hasher;
            this.loggingService = loggingService;
            this.queue = new BlockingCollection<QueueItem>();
            this.cancellation = new CancellationTokenSource();
		}

		public void Start()
		{
			if (thread != null)
                throw new InvalidOperationException("Already in progress.");

            thread = new Thread(DoStart) { IsBackground = true };
			thread.Start();
		}

		private void DoStart()
		{
			this.loggingService.LogInfo("Started re-index of shared files...");

            if (this.queue == null)
                this.queue = new BlockingCollection<QueueItem>();

            if (this.cancellation == null)
                this.cancellation = new CancellationTokenSource();

            StartedIndexing?.Invoke(this, EventArgs.Empty);

			LocalDirectory myDirectory = Core.FileSystem.RootDirectory.MyDirectory;
			
			// Remove files/directories from db that no longer exist on the filesystem.
			Core.FileSystem.PurgeMissing();
			
			// If any dirs were removed from the list in settings, remove them from db.
			foreach (LocalDirectory dir in myDirectory.Directories)
            {
				if (!Core.Settings.SharedDirectories.Contains(dir.LocalPath))
                {
					dir.Delete();
				}
			}
			
			TimeSpan lastScanAgo = (DateTime.Now - Core.Settings.LastShareScan);
			if (Math.Abs(lastScanAgo.TotalHours) >= 1)
            {
				this.loggingService.LogDebug("Starting directory scan. Last scan was {0} minutes ago.", Math.Abs(lastScanAgo.TotalMinutes));				
				foreach (string directoryName in Core.Settings.SharedDirectories)
                {
					var info = new IO.DirectoryInfo(directoryName);	
					if (info.Exists)
                    {
                        this.queue.Add(new QueueItem(myDirectory, info), this.cancellation.Token);
					}
                    else
                    {
						this.loggingService.LogWarning("Directory does not exist: {0}.", info.FullName);
					}
				}

                try
                {
                    QueueItem item;
                    while (this.queue.TryTake(out item, 1000, this.cancellation.Token))
                    {
                        ProcessDirectory(item.Parent, item.Directory);
                    }
                }
                catch (ThreadAbortException)
                {
                    this.loggingService.LogInfo("Aborted indexing of shared files...");
                }
				
				Core.Settings.LastShareScan = DateTime.Now;
				
			} else
            {
				this.loggingService.LogDebug("Skipping directory scan because last scan was {0} minutes ago.", Math.Abs(lastScanAgo.TotalMinutes));
			}
			
			this.loggingService.LogInfo("Finished re-index of shared files...");
			
			thread = null;
			
            FinishedIndexing?.Invoke(this, EventArgs.Empty);
		}

		public void Stop()
		{
            if (this.cancellation != null && !this.cancellation.IsCancellationRequested)
            {
                this.cancellation.Cancel();
                this.cancellation.Dispose();
            }

            if (this.queue != null)
            {
                this.queue.CompleteAdding();
                this.queue.Dispose();
            }

			if (thread != null)
            {
				thread.Abort();
				thread = null;
				
				this.loggingService.LogInfo("Aborted re-index of shared files...");
				
                StoppedIndexing?.Invoke(this, EventArgs.Empty);
			}
		}

		private void ProcessDirectory(LocalDirectory parentDirectory, IO.DirectoryInfo directoryInfo)
		{
			if (parentDirectory == null)
                throw new ArgumentNullException(nameof(parentDirectory));

			if (directoryInfo == null)
                throw new ArgumentNullException(nameof(directoryInfo));

			try
            {
				LocalDirectory directory = (LocalDirectory)parentDirectory.GetSubdirectory(directoryInfo.Name);

				if (directory == null)
					directory = parentDirectory.CreateSubDirectory(directoryInfo.Name, directoryInfo.FullName);

                foreach (var fileInfo in directoryInfo.EnumerateFiles().Where(f => !f.Name.StartsWith(".")))
                {
                    IndexingFile?.Invoke(this, new FilenameEventArgs(fileInfo.FullName));
					
					LocalFile file = (LocalFile)directory.GetFile(fileInfo.Name);
					if (file == null)
                    {
						file = directory.CreateFile(fileInfo);
					}
                    else
                    {
						// XXX: Update file info
					}

					if (string.IsNullOrEmpty(file.InfoHash))
                    {
						this.hasher.HashFile(file);
					}
				}

                foreach (var subDirectoryInfo in directoryInfo.EnumerateDirectories().Where(d => !d.Name.StartsWith(".")))
                {
					//ProcessDirectory(directory, subDirectoryInfo);
                    this.queue.Add(new QueueItem(directory, subDirectoryInfo), this.cancellation.Token);
				}
			}
            catch (ThreadAbortException)
            {
				// Canceled, ignore error.
			}
            catch (Exception ex)
            {
				this.loggingService.LogError("Error while re-indexing shared files:", ex);

                ErrorIndexing?.Invoke(this, new ErrorEventArgs(ex));
			}
		}
	}
}
