using System;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;

namespace FileFind.Stun
{
	public static class StunClient
	{
		public static async Task<IPAddress> GetExternalAddressAsync(string server = "stun.ekiga.net")
		{
            var entry = await Dns.GetHostEntryAsync(server);
			var endPoint = new IPEndPoint(entry.AddressList[0], 3478);
            IPAddress result = null;

            using (var client = new UdpClient())
            {
                client.Connect(endPoint);

                // send
                var header = new MessageHeader() { MessageType = MessageType.BindingRequest };
                var bytes = header.GetBytes();
                await client.SendAsync(bytes, bytes.Length);

                // receive
                header = new MessageHeader((await client.ReceiveAsync()).Buffer);
                if (header.MessageType != MessageType.BindingResponse)
                    throw new Exception("Wrong response message!");

                // check message
                var attr = header.MessageAttributes.OfType<MappedAddressAttribute>().SingleOrDefault();
                if (attr == null)
                    throw new Exception("Response was missing Mapped-address!");

                result = attr.Address;
            }

            return result;
		}
	}
}
