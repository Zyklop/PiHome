using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace Communication.Networking
{
	public class MulticastConnector : IDisposable, INetworkConnector
	{
		private const int BufferLength = 2048;
		private const int Ttl = 10;
		private readonly IPAddress _multicastAddress = new IPAddress(new byte []{224, (byte)'r', (byte)'p', (byte)'i'});
		private readonly int _listeningPort = 7489;
		private readonly Socket _listeningSocket;
		private Thread _thread;
		private CancellationTokenSource canceller;

		public MulticastConnector(int listeningPort = 0, string ip = "")
		{
			if (!string.IsNullOrWhiteSpace(ip))
			{
				var ips = ip.Split('.');
				foreach (var s in ips)
				{
					int i;
					if (!int.TryParse(s, out i))
					{
						var c = s[0];
						ip = ip.Replace(c.ToString(), ((int)c).ToString());
					}
				}
				_multicastAddress = IPAddress.Parse(ip);
			}
			if (listeningPort != 0)
			{
				_listeningPort = listeningPort;
			}

			// We want to reuse this socket
			_listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		}

		~MulticastConnector()
		{
			StopListening();
		}

		public void Send(object data)
		{
			var ser = JsonConvert.SerializeObject(data);
			var b = Encoding.UTF8.GetBytes(ser);

			var ipep = new IPEndPoint(_multicastAddress, _listeningPort);

			// Some weird memory exception occurs if I reuse the socket
			var sendingSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			sendingSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(_multicastAddress));
			sendingSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, Ttl);
			sendingSocket.Connect(ipep);
			sendingSocket.Send(b, b.Length, SocketFlags.None);

			sendingSocket.Close();
		}

		public void Listen()
		{
			if (_thread != null && _thread.IsAlive)
			{
				canceller.Cancel();
			}
			canceller = new CancellationTokenSource();
			_thread = new Thread(() =>
			{
				var ipep = new IPEndPoint(IPAddress.Any, _listeningPort);
				_listeningSocket.Bind(ipep);
				_listeningSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(_multicastAddress, IPAddress.Any));

				var token = canceller.Token;
				while (!token.IsCancellationRequested)
				{
					var b = new byte[BufferLength];
					var remoteEp = (EndPoint)ipep;
					try
					{
						_listeningSocket.ReceiveFrom(b, ref remoteEp);
					}
					catch (SocketException e)
					{
						if (e.ErrorCode != 10004)
						{
							throw e;
						}
						break;
					}
					var data = JsonConvert.DeserializeObject<dynamic>(Encoding.UTF8.GetString(b).Trim());
					OnDataRecived?.Invoke(this, new TransmissionEventArgs(data, ((IPEndPoint)remoteEp).Address));
				}
			});
			_thread.Start();
		}

		public void StopListening()
		{
			if (_listeningSocket.IsBound)
			{
				try
				{
					_listeningSocket.Shutdown(SocketShutdown.Send);
					_listeningSocket.Close();
				}
				catch (ObjectDisposedException e)
				{
					//already happend
				}
			}
			if (_thread != null && _thread.IsAlive)
			{
				canceller.Cancel();
			}
		}
		
		public event EventHandler<TransmissionEventArgs> OnDataRecived;

		public void Dispose()
		{
			StopListening();
		}
	}
}
