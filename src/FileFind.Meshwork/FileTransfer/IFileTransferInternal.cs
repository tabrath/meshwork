//
// IFileTransfer.cs: 
//
// Author:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2008 FileFind.net (http://filefind.net)
//

using FileFind.Meshwork.Errors;

namespace FileFind.Meshwork.FileTransfer
{
    internal interface IFileTransferInternal
	{
		void DetailsReceived();
		void ErrorReceived(Node node, FileTransferError error);
	}
	
}
