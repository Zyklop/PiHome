using System;
using System.Net;

namespace Coordinator.Networking
{
	public interface INetworkConnector
	{
		void Send(object data);
		void Listen();
		void StopListening();
		event EventHandler<TransmissionEventArgs> OnDataRecived;
	}


	public class TransmissionEventArgs : EventArgs
	{
		public TransmissionEventArgs(dynamic data, IPAddress ip)
		{
			Data = data;
			Ip = ip;
		}

		public IPAddress Ip { get; }
		public dynamic Data { get; }
	}
}