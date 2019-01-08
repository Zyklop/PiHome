using System.Net;
using Communication.Types;

namespace Communication.ApiCommunication
{
	public class LedCommunicator : BaseCommunicator
	{
		public LedCommunicator(IPAddress moduleIp) : base(moduleIp)
		{
		}

		protected override string BasePath => "/led/api";

		public void TurnOff()
		{
			GetRequest<StatusResponse>("/turnoff");
		}

		public void SetSolid(byte red, byte green, byte blue)
		{
			GetRequest<StatusResponse>("/solid", red.ToString(), green.ToString(), blue.ToString());
		}

		public void SetRGBB(byte[] data)
		{
			PostRequest<StatusResponse>("/customRGBB", data);
		}
	}
}