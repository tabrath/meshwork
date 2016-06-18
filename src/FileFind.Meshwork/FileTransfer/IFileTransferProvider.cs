using System;
using FileFind.Meshwork.Filesystem;
using FileFind.Meshwork.Transport;

namespace FileFind.Meshwork.FileTransfer
{
	public interface IFileTransferProvider
	{
		int GlobalUploadSpeedLimit { get; set; }
		int GlobalDownloadSpeedLimit { get; set; }

        IFileTransfer CreateFileTransfer(IFile file);
        void RemoveFileTransfer(IFileTransfer transfer);
        void HandleTransport(ITransport transport, object state = null);
	}
}
