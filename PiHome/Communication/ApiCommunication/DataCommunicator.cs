using System.Net;
using DataPersistance.Modules;

namespace Communication.ApiCommunication
{
	public class DataCommunicator : BaseCommunicator
	{
		public DataCommunicator(IPAddress moduleIp, int port = 5000) : base(moduleIp, port)
		{
		}

		protected override string BasePath => string.Empty;

		public PresetDto GetPreset(string name)
		{
			var res = GetRequest<PresetDto>($"Preset/Get?name={name}");
			return res;
		}

		public string[] GetAllPresets()
		{
			return GetRequest<string[]>("Preset/GetAllPresets");
		}

		public ModuleDto GetConfig()
		{
			return GetRequest<ModuleDto>("Module/Settings");
		}
	}
}