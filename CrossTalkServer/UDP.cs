using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CrossTalkServer
{
	public struct Received
	{
		public IPEndPoint Sender;
		public byte[] Payload;
	}

	abstract class UdpBase
	{
		protected UdpClient Client;

		protected UdpBase()
		{
			Client = new UdpClient();
		}

		public async Task<Received> Receive()
		{
			try
			{
				UdpReceiveResult result = await Client.ReceiveAsync();
				return new Received()
				{
					Payload = result.Buffer,
					Sender = result.RemoteEndPoint
				};
			}
			catch (Exception)
			{
				// SWALLOW
				return new Received();
			}
		}
	}

	//Server
	class UdpListener : UdpBase
	{
		private IPEndPoint _listenOn;

		public UdpListener() : this(new IPEndPoint(IPAddress.Any, 32123))
		{
		}

		public UdpListener(IPEndPoint endpoint)
		{
			_listenOn = endpoint;
			Client = new UdpClient(_listenOn);
		}

		public void Reply(byte[] datagram, IPEndPoint endpoint)
		{
			Client.SendAsync(datagram, datagram.Length, endpoint);
		}

		public void Close()
		{
			Client.Close();
		}

	}

	//Client
	class UdpUser : UdpBase
	{
		private UdpUser() { }

		public static UdpUser ConnectTo(string hostname, int port)
		{
			UdpUser connection = new UdpUser();
			connection.Client.AllowNatTraversal(true);
			connection.Client.Connect(hostname, port);
			return connection;
		}

		public void Send(byte[] datagram)
		{
			Client.SendAsync(datagram, datagram.Length);
			//Console.WriteLine(DateTime.Now.Second + "." + DateTime.Now.Millisecond + " // " + datagram.Length);
		}

		public void Close()
		{
			Client.Close();
		}


	}
}
