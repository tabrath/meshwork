//
// TransportManager.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006-2008 FileFind.net (http://filefind.net)
//

using System;
using System.Security.Cryptography;
using System.Runtime.Remoting.Messaging;
using System.Collections;
using System.Collections.Generic;
using Org.Mentalis.Security.Cryptography;

namespace FileFind.Meshwork.Transport
{
    public class TransportEventArgs : EventArgs
    {
        public ITransport Transport { get; }

        public TransportEventArgs(ITransport transport)
            : base()
        {
            Transport = transport;
        }
    }
	
}
