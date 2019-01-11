using System;
using System.Collections.Generic;
using System.Net;

namespace Coordinator.Networking
{
	public class MasterNetworker
	{
		private static BroadcastConnector _broad;
		private static MulticastConnector _multi;
		private static int _broadcastsAhead = 0;
		private string _moduleName;
		private readonly Dictionary<string, IPAddress> _knownModules = new Dictionary<string, IPAddress>();

		public MasterNetworker(string moduleName)
		{
			_moduleName = moduleName;
			if (_broad == null && _multi == null)
			{
				_broad = new BroadcastConnector();
				_multi = new MulticastConnector();
				_broad.Listen();
				_multi.Listen();
			}

			if (_broad != null)
			{
				_broad.OnDataRecived += MessageRecived;
			}
			if (_multi != null)
			{
				_multi.OnDataRecived += MessageRecived;
			}
		}

		private void MessageRecived(object sender, TransmissionEventArgs e)
		{
			if (_broad != null && sender is MulticastConnector)
			{
				_broad.OnDataRecived -= MessageRecived;
				_broad.StopListening();
				_broad.Dispose();
				_broad = null;
			}
			else if(_multi != null && sender is BroadcastConnector)
			{
				if (_broadcastsAhead > 10)
				{
					_multi.OnDataRecived -= MessageRecived;
					_multi.StopListening();
					_multi.Dispose();
					_multi = null;
				}
				else
				{
					_broadcastsAhead++;
				}
			}

			if (!_knownModules.ContainsKey(e.Data.ModuleName))
			{
				_knownModules.Add(e.Data.ModuleName, e.Ip);
				OnChange?.Invoke(this, new ChangeDetectedEventArgs{ModuleIp = e.Ip, ModuleName = e.Data.ModuleName, Type = ChangeType.ModuleAddress});
			}
			else if (_knownModules[e.Data.moduleName] != e.Ip)
			{
				_knownModules[e.Data.ModuleName] = e.Ip;
				OnChange?.Invoke(this, new ChangeDetectedEventArgs { ModuleIp = e.Ip, ModuleName = e.Data.ModuleName, Type = ChangeType.ModuleAddress });
			}

			if (e.Data.Type == "PresetChange")
			{
				OnChange?.Invoke(this, new ChangeDetectedEventArgs { ModuleIp = e.Ip, ModuleName = e.Data.ModuleName, Type = ChangeType.Preset, PresetName = e.Data.PresetName });
			}
			else if (e.Data.Type == "ModuleChange")
			{
				OnChange?.Invoke(this, new ChangeDetectedEventArgs { ModuleIp = e.Ip, ModuleName = e.Data.ModuleName, Type = ChangeType.ModuleSettings });
			}
		}

		public void Announce()
		{
			_multi?.Send(new ModuleAnnouncment{ModuleName = _moduleName});
			_broad?.Send(new ModuleAnnouncment { ModuleName = _moduleName });
		}

		public void ModuleChanges()
		{
			_multi?.Send(new ModuleChanged { ModuleName = _moduleName });
			_broad?.Send(new ModuleChanged { ModuleName = _moduleName });
		}

		public void PresetChanges(string presetName)
		{
			_multi?.Send(new PresetChanged { ModuleName = _moduleName, PresetName = presetName });
			_broad?.Send(new PresetChanged { ModuleName = _moduleName, PresetName = presetName });
		}

		public event EventHandler<ChangeDetectedEventArgs> OnChange; 

		private class ModuleAnnouncment
		{
			public string Type => "Announcement";
			public string ModuleName { get; set; }
		}

		private class PresetChanged
		{
			public string Type => "PresetChange";
			public string PresetName { get; set; }
			public string ModuleName { get; set; }
		}

		private class ModuleChanged
		{
			public string Type => "ModuleChange";
			public string ModuleName { get; set; }
		}
	}

	public class ChangeDetectedEventArgs : EventArgs
	{
		public string ModuleName { get; set; }
		public IPAddress ModuleIp { get; set; }
		public string PresetName { get; set; }
		public ChangeType Type { get; set; }
	}

	public enum ChangeType
	{
		ModuleAddress,
		ModuleSettings,
		Preset
	}
}