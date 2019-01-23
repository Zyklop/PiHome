using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Serilog;

namespace Communication.Networking
{
	public class MasterNetworker : IDisposable
	{
		private static BroadcastConnector _broad;
		private static MulticastConnector _multi;
		private static int _broadcastsAhead = 0;
		private string _moduleName;
		private readonly ConcurrentDictionary<string, IPAddress> _knownModules = new ConcurrentDictionary<string, IPAddress>();
		private ILogger logger;

		public MasterNetworker(string moduleName, ILogger logger)
		{
			this.logger = logger;
			_moduleName = moduleName;
			if (_broad == null && _multi == null)
			{
				_broad = new BroadcastConnector(logger);
				_multi = new MulticastConnector(logger);
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

			var localIps = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces().SelectMany(x => x.GetIPProperties().UnicastAddresses).Where(x => x.Address.AddressFamily == AddressFamily.InterNetwork);
			var isLocal = localIps.Any(x => x.Address.Equals(e.Ip));
			if (isLocal)
			{
				return;
			}
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
			
			var moduleName = e.Data.ModuleName.Value;
			var eventType = e.Data.Type.Value;

			if (!_knownModules.ContainsKey(moduleName))
			{
				_knownModules.TryAdd(moduleName, e.Ip);
				OnChange?.Invoke(this, new ChangeDetectedEventArgs{ModuleIp = e.Ip, ModuleName = moduleName, Type = ChangeType.ModuleAddress});
			}
			else if (!_knownModules[moduleName].Equals(e.Ip))
			{
				_knownModules[moduleName] = e.Ip;
				OnChange?.Invoke(this, new ChangeDetectedEventArgs { ModuleIp = e.Ip, ModuleName = moduleName, Type = ChangeType.ModuleAddress });
			}

			if (eventType == "PresetChange")
			{
				OnChange?.Invoke(this, new ChangeDetectedEventArgs
				{
					ModuleIp = e.Ip,
					ModuleName = moduleName,
					Type = e.Data.Deleted.Value ? ChangeType.PresetDeleted : ChangeType.PresetUpserted,
					PresetName = e.Data.PresetName.Value
				});
			}
			else if (eventType == "ModuleChange")
			{
				OnChange?.Invoke(this, new ChangeDetectedEventArgs { ModuleIp = e.Ip, ModuleName = moduleName, Type = ChangeType.ModuleSettings });
			}
		}

		public void Announce()
		{
			if (string.IsNullOrEmpty(_moduleName))
			{
				throw new ArgumentException("Modulename is not allowed to be empty");
			}
			logger.Debug("Announcing on:" + (_multi==null?"":" multicast") + (_broad==null?"":" broadcast"));
			_multi?.Send(new ModuleAnnouncment {ModuleName = _moduleName});
			_broad?.Send(new ModuleAnnouncment {ModuleName = _moduleName});
		}

		public void ModuleChanges()
		{
			if (string.IsNullOrEmpty(_moduleName))
			{
				throw new ArgumentException("Modulename is not allowed to be empty");
			}
			_multi?.Send(new ModuleChanged {ModuleName = _moduleName});
			_broad?.Send(new ModuleChanged {ModuleName = _moduleName});
		}

		public void PresetChanges(string presetName)
		{
			if (string.IsNullOrEmpty(_moduleName))
			{
				throw new ArgumentException("Modulename is not allowed to be empty");
			}
			_multi?.Send(new PresetChanged {ModuleName = _moduleName, PresetName = presetName});
			_broad?.Send(new PresetChanged {ModuleName = _moduleName, PresetName = presetName});
		}
		public void PresetDeleted(string presetName)
		{
			if (string.IsNullOrEmpty(_moduleName))
			{
				throw new ArgumentException("Modulename is not allowed to be empty");
			}
			_multi?.Send(new PresetChanged {ModuleName = _moduleName, PresetName = presetName, Deleted = true});
			_broad?.Send(new PresetChanged {ModuleName = _moduleName, PresetName = presetName, Deleted = true});
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
			public bool Deleted { get; set; }
		}

		private class ModuleChanged
		{
			public string Type => "ModuleChange";
			public string ModuleName { get; set; }
		}

		public void Dispose()
		{
			if (_multi != null)
			{
				_multi.OnDataRecived -= MessageRecived;
				_multi.StopListening();
				_multi.Dispose();
				_multi = null;
			}
			if (_broad != null)
			{
				_broad.OnDataRecived -= MessageRecived;
				_broad.StopListening();
				_broad.Dispose();
				_broad = null;
			}
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
		PresetUpserted,
		PresetDeleted
	}
}