using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Communication.Networking
{
    public class MulticastConnector : IDisposable, INetworkConnector, IHostedService
    {
        private const int BufferLength = 2048;
        private const int Ttl = 2;
        private readonly IPAddress _multicastAddress = new IPAddress(new byte[] { 224, (byte)'r', (byte)'p', (byte)'i' });
        private readonly int _listeningPort = 7489;
        private readonly Socket _listeningSocket;
        private readonly ILogger<MulticastConnector> logger;
        private Thread _thread;
        private CancellationTokenSource canceller;

        public MulticastConnector(ILogger<MulticastConnector> logger, int listeningPort = 0, string ip = "")
        {
            this.logger = logger;
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
            _listeningSocket.ExclusiveAddressUse = false;
            canceller = new CancellationTokenSource();
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
            using (var sendingSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                sendingSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, Ttl);
                sendingSocket.ExclusiveAddressUse = false;
                sendingSocket.Connect(ipep);
                sendingSocket.Send(b, b.Length, SocketFlags.None);
                sendingSocket.Close();
            }
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
                    _listeningSocket.Shutdown(SocketShutdown.Receive);
                    _listeningSocket.Close();
                }
                catch (ObjectDisposedException e)
                {
                    //already happend
                }
                catch (SocketException e)
                {
                    //fuck sockets
                }
            }
            if (_thread != null && _thread.IsAlive)
            {
                canceller.Cancel();
            }
            logger.LogInformation("Multicast connector terminated");
        }

        public event EventHandler<TransmissionEventArgs> OnDataRecived;

        public void Dispose()
        {
            StopListening();
            logger.LogInformation("Multicast connector disposed");
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Listen();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            StopListening();
            return Task.CompletedTask;
        }
    }
}
