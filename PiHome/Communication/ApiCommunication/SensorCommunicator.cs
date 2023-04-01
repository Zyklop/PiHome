using System.Net;
using Communication.Types;
using Environment = Communication.Types.Environment;

namespace Communication.ApiCommunication
{
	public class SensorCommunicator : BaseCommunicator
	{
		public SensorCommunicator(IPAddress moduleIp) : base(moduleIp)
		{
		}

		protected override string BasePath => "/sensors/api";

		public int Analog(int pinId)
		{
			var value = GetRequest<AnalogValue>($"/analog/{pinId}");
			return value.Value;
		}

		public Environment GetEnvironment()
		{
			return GetRequest<Environment>("/environment");
		}
	}
}