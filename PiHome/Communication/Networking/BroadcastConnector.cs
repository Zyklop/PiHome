using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Communication.Networking
{
    public class BroadcastConnector : INetworkConnector, IDisposable, IHostedService
    {
        private readonly int _port = 9876;
        private UdpClient _client;
        private Thread? _thread;
        private CancellationTokenSource canceller;
        private ILogger<BroadcastConnector> logger;

        public BroadcastConnector(ILogger<BroadcastConnector> logger, int port = 0)
        {
            this.logger = logger;
            if (port != 0)
            {
                _port = port;
            }
            _client = new UdpClient();
            _client.EnableBroadcast = true;
            canceller = new CancellationTokenSource();
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
                canceller.Cancel();
            }
            canceller = new CancellationTokenSource();
            _client.Client.Bind(new IPEndPoint(IPAddress.Any, _port));
            _thread = new Thread(() =>
            {
                var from = new IPEndPoint(0, 0);
                byte[] buffer;
                var token = canceller.Token;
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var response = _client.ReceiveAsync();
                        if (response.Wait(3000, token) && !response.IsFaulted && !response.IsCanceled &&
                            response.IsCompleted)
                        {
                            buffer = response.Result.Buffer;
                            from = response.Result.RemoteEndPoint;
                            var data = JsonConvert.DeserializeObject<dynamic>(Encoding.UTF8.GetString(buffer));
                            OnDataRecived?.Invoke(this, new TransmissionEventArgs(data, from.Address));
                        }
                        else
                        {
                            if (response.IsFaulted)
                            {
                                logger.LogError(response.Exception, "Broadcast failed");
                            }
                        }
                    }
                    catch (SocketException e)
                    {
                        if (e.ErrorCode != 10004)
                        {
                            throw e;
                        }

                        logger.LogError(e, "Broadcast failed");
                        break;
                    }
                    catch (OperationCanceledException e)
                    {
                        logger.LogInformation("Broadcaster terminated");
                    }
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
                    _client.Client.Shutdown(SocketShutdown.Receive);
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
        }

        public event EventHandler<TransmissionEventArgs> OnDataRecived;
        public void Dispose()
        {
            StopListening();
            try
            {
                _client.Client.Shutdown(SocketShutdown.Both);
                _client.Client.Close();
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
