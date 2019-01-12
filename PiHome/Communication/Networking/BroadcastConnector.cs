using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace Communication.Networking
{
	public class BroadcastConnector : INetworkConnector, IDisposable
	{
		private readonly int _port = 9876;
		private UdpClient _client;
		private Thread _thread;

		public BroadcastConnector(int port = 0)
		{
			if (port != 0)
			{
				_port = port;
			}
			_client = new UdpClient();
			_client.EnableBroadcast = true;
		}

		public void Send(object data)
		{
			var ser = JsonConvert.SerializeObject(data);
			var b = Encoding.UTF8.GetBytes(ser);
			_client.Send(b, b.Length, IPAddress.Broadcast.ToString(), _port);
		}

		public void Listen()
		{
			if (_thread != null && _thread.IsAlive)
			{
				_thread.Abort();
			}
			_client.Client.Bind(new IPEndPoint(IPAddress.Any, _port));
			_thread = new Thread(() =>
			{
				var from = new IPEndPoint(0, 0);
				byte[] buffer;
				while (_client.Client.IsBound)
				{
					try
					{
						buffer = _client.Receive(ref from);
					}
					catch (SocketException e)
					{
						if (e.ErrorCode != 10004)
						{
							throw e;
						}
						break;
					}
					var data = JsonConvert.DeserializeObject<dynamic>(Encoding.UTF8.GetString(buffer));
					OnDataRecived?.Invoke(this, new TransmissionEventArgs(data, from.Address));
				}
			});
			_thread.Start();
		}

		public void StopListening()
		{

			if (_client.Client.IsBound)
			{
				try
				{
					_client.Client.Shutdown(SocketShutdown.Send);
					_client.Client.Close();
				}
				catch (ObjectDisposedException e)
				{
					//already happend
				}
			}
			if (_thread != null && _thread.IsAlive)
			{
				_thread.Abort();
			}
		}

		public event EventHandler<TransmissionEventArgs> OnDataRecived;
		public void Dispose()
		{
			StopListening();
		}
	}
}
