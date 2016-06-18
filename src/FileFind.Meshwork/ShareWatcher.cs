//
// ShareWatcher.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2008 FileFind.net (http://filefind.net)
//

// XXX:
// CRAP. Directories can have more than one local path, becuase they get merged.
// We dont want to store this in the db at all, local_path should be nil for directories.
//


using System;
using System.Linq;
using System.Data;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using MFS = FileFind.Meshwork.Filesystem;
using System.ComponentModel.Composition;
using Meshwork.Logging;

namespace FileFind.Meshwork
{
    //TODO: cleanup - IShareWatcher
    [Export(typeof(IShareWatcher)), PartCreationPolicy(CreationPolicy.Shared)]
    internal class ShareWatcher : IShareWatcher
	{
		Dictionary<string, FileSystemWatcher> watchers = new Dictionary<string, FileSystemWatcher>();
		
		bool running;
		AutoResetEvent mutex = new AutoResetEvent(false);
		Thread changedFilesThread;
		Dictionary<string, ChangedFileInfo> changedFiles = new Dictionary<string, ChangedFileInfo>();
        private readonly ILoggingService loggingService;

        [ImportingConstructor]
        public ShareWatcher(ILoggingService loggingService)
		{
            this.loggingService = loggingService;
			changedFilesThread = new Thread(ChangedFileWatcher);
		}

		public void Start()
		{
			running = true;
			changedFilesThread.Start();
			foreach (string path in Core.Settings.SharedDirectories)
            {
				FileSystemWatcher watcher = new FileSystemWatcher(path);
				watcher.IncludeSubdirectories = true;
				watcher.Created += watcher_Changed;
				watcher.Changed += watcher_Changed;
				watcher.Deleted += watcher_Deleted;
				watchers.Add(path, watcher);
				watcher.EnableRaisingEvents = true;
			}
		}

		public void Stop()
		{
			running = false;
			if (changedFilesThread.IsAlive)
            {
				changedFilesThread.Join();
			}
		}

		private void watcher_Changed(object sender, FileSystemEventArgs args)
		{
			try
            {
				if (System.IO.Directory.Exists(args.FullPath))
                {
					HandleDirectoryChanged(args.FullPath);
				}
                else
                {
					lock (changedFiles)
                    {
						if (!changedFiles.ContainsKey(args.FullPath))
                        {
							ChangedFileInfo info = new ChangedFileInfo();
							info.LastChangeSeen = DateTime.Now;
							info.FileSize = new FileInfo(args.FullPath).Length;
							changedFiles.Add(args.FullPath, info);
						}
                        else 
                        {
							ChangedFileInfo info = changedFiles[args.FullPath];
							info.LastChangeSeen = DateTime.Now;
						}
					}
					mutex.Set();
				}
			}
            catch (Exception ex)
            {
				this.loggingService.LogError(ex);
			}
		}	
		
		private void ChangedFileWatcher()
		{
			try
            {
				while (running)
                {
					int count = 0;
					lock (changedFiles) 
                    {
						count = changedFiles.Count;
					}
					if (count == 0)
                    {
						mutex.WaitOne();
					} 
                    else 
                    {
						Thread.Sleep(1000);
					}
					lock (changedFiles)
                    {
						List<string> toRemove = new List<string>();
						foreach (KeyValuePair<string, ChangedFileInfo> pair in changedFiles)
                        {
							if ((DateTime.Now - pair.Value.LastChangeSeen).TotalSeconds >= 5) 
                            {
								long size = new FileInfo(pair.Key).Length;
								if (size == pair.Value.FileSize) 
                                {
									HandleFileChanged(pair.Key);
									toRemove.Add(pair.Key);
								}
                                else
                                {
									pair.Value.FileSize = size;
								}
							}
						}
						foreach (string key in toRemove) 
                        {
							changedFiles.Remove(key);
						}
					}
				}
			}
            catch (Exception ex)
            {
				this.loggingService.LogError(ex);
			}
		}

		object directoryChangeLock = new object();
		private void HandleDirectoryChanged(string path)
		{
			// Do these one at a time.
			lock (directoryChangeLock)
            {
				DirectoryInfo info = new DirectoryInfo(path);
				MFS.IDirectoryItem item = GetFromLocalPath(path);
				if (item == null && info != null)
                {
					// New Directory!

					MFS.LocalDirectory parentDirectory = GetParentDirectory(info);
					if (parentDirectory != null)
                    {
						this.loggingService.LogDebug("NEW DIR !! " + path);
						parentDirectory.CreateSubDirectory(info.Name, info.FullName);
					}
                    else 
                    {
						// No parent directory, this happens because
						// we can get events out of order.
						this.loggingService.LogDebug("NEW DIR NO PARENT !! " + path);
						CreateDirectoryForLocalPath(path);
					}
				}
			}
		}

		private void HandleFileChanged(string path)
		{
			FileInfo info = new FileInfo(path);
			MFS.IDirectoryItem item = GetFromLocalPath(path);

			if (item == null) 
            {
				// New File!
				MFS.LocalDirectory parentDirectory = GetParentDirectory(info);

				this.loggingService.LogDebug("NEW FILE!! IN " + parentDirectory.FullPath);
			}
            else
            {
				// Updated File!
				this.loggingService.LogWarning("NOTE: Changed file detected, however handling this is not currently supported. Path: {0}", item.FullPath);
			}
		}

		private void watcher_Deleted(object sender, FileSystemEventArgs args)
		{
			try
            {
				MFS.ILocalDirectoryItem item = GetFromLocalPath(args.FullPath);
				if (item != null)
                {
					item.Delete();
				}
			}
            catch (Exception ex) 
            {
				this.loggingService.LogError(ex);
			}
		}

		private class ChangedFileInfo
		{
			public long FileSize;
			public DateTime LastChangeSeen;
		}

		// XXX: Move this elsewhere.
		private MFS.ILocalDirectoryItem GetFromLocalPath(string localPath)
		{
			this.loggingService.LogDebug("GET FROM LOCAL !!! " + localPath);
			return Core.FileSystem.UseConnection<MFS.ILocalDirectoryItem>(delegate (IDbConnection connection) 
            {
				IDbCommand cmd = connection.CreateCommand();
				cmd.CommandText = "SELECT * FROM directoryitems WHERE local_path = @local_path";
				Core.FileSystem.AddParameter(cmd, "@local_path", localPath);
				DataSet ds = Core.FileSystem.ExecuteDataSet(cmd);
				if (ds.Tables[0].Rows.Count > 0)
                {
					string type = ds.Tables[0].Rows[0]["type"].ToString();
					if (type == "F") {
						return MFS.LocalFile.FromDataRow(ds.Tables[0].Rows[0]);
					} else {
						return MFS.LocalDirectory.FromDataRow(ds.Tables[0].Rows[0]);
					}
				}
                else
                {
					return null;
				}
			});
		}

		// XXX: Move this too!
		private void CreateDirectoryForLocalPath(string localPath)
		{
			/*
			DirectoryInfo directoryInfo = new DirectoryInfo(localPath);
			Directory directory;

			while (directory == null) {
				if (Core.SharedDirectories.Contains(directoryInfo.FullName)) {
					// We've gone up too high, give up!
					throw new Exception("Eeep");
				}

				directory = (MFS.Directory) GetFromLocalPath(directoryInfo.FullName);
				if (directory != null) {
					
					// OK, We have a place to start. Create from here.

				} else {
					directoryInfo = directoryInfo.Parent;
				}
			}

			*/
		}

		// XXX: Move this too!
		private MFS.LocalDirectory GetParentDirectory(FileSystemInfo info)
		{
			DirectoryInfo directoryInfo = (info is FileInfo) ? ((FileInfo)info).Directory : (DirectoryInfo)info;

			this.loggingService.LogDebug("GET PARENT DIRECTORY " + directoryInfo.FullName);

			if (Core.Settings.SharedDirectories.Contains(directoryInfo.FullName))
            {
				return Core.MyDirectory;
			}
            else
            {
				return (MFS.LocalDirectory)GetFromLocalPath(directoryInfo.Parent.FullName);
			}
		}
	}
}
