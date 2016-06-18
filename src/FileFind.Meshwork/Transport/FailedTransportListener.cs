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
	public class FailedTransportListener
	{
        public ITransportListener Listener { get; }
        public Exception Error { get; }

		public FailedTransportListener(ITransportListener listener, Exception error)
		{
			Listener = listener;
			Error = error;
		}
	}
}
