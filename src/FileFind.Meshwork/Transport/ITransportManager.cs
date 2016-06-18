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
using System.ComponentModel.Composition;

namespace FileFind.Meshwork.Transport
{
    public interface ITransportManager
    {
        event EventHandler<TransportEventArgs> NewTransportAdded;
        event EventHandler<TransportEventArgs> TransportRemoved;
        event EventHandler<ErrorEventArgs> TransportError;

        ITransport[] Transports { get; }
        int TransportCount { get; }

        string GetFriendlyName(Type type);
        void Add(ITransport transport);
        void Add(ITransport transport, TransportCallback connectCallback);
        void Remove(ITransport transport);
    }
    
}
