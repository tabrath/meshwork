using System;
using System.ComponentModel.Composition;
using System.Net.Sockets;
using FileFind.Meshwork.Destination;

namespace Meshwork.Destination
{
    [AttributeUsage(AttributeTargets.Class)]
    public class DestinationAttribute : Attribute
    {
        public string Name { get; set; }
        public AddressFamily Family { get; set; }
        public ProtocolType Protocol { get; set; }
        public SocketType Socket { get; set; }

        public DestinationAttribute()
            : base()
        {
            Name = string.Empty;
            Family = AddressFamily.InterNetwork;
            Protocol = ProtocolType.Tcp;
            Socket = SocketType.Stream;
        }
    }
}

