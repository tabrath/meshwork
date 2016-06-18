//
// ITransportListener.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;

namespace FileFind.Meshwork.Transport
{
	public interface ITransportListener
	{
		void StartListening();
		void StopListening();
	}
}
