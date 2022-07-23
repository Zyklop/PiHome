using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace Communication.Networking
{
    public class MasterNetworker : IDisposable
    {
        private BroadcastConnector broad;
        private MulticastConnector multi;
        private static int broadcastsAhead = 0;
        private string _moduleName;
        private readonly ConcurrentDictionary<string, IPAddress> _knownModules = new ConcurrentDictionary<string, IPAddress>();
        private ILogger<MasterNetworker> logger;
        private AnnouncementMode announcementMode = AnnouncementMode.Both;

        public MasterNetworker(string moduleName, ILogger<MasterNetworker> logger, BroadcastConnector broad, MulticastConnector multi)
        {
            _moduleName = moduleName;
            this.logger = logger;
            this.broad = broad;
            this.multi = multi;
            broad.OnDataRecived += MessageRecived;
            multi.OnDataRecived += MessageRecived;
        }

        private void MessageRecived(object? sender, TransmissionEventArgs e)
        {

            var localIps = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces().SelectMany(x => x.GetIPProperties().UnicastAddresses).Where(x => x.Address.AddressFamily == AddressFamily.InterNetwork);
            var isLocal = localIps.Any(x => x.Address.Equals(e.Ip));
            if (isLocal)
            {
                return;
            }
            if (sender is MulticastConnector)
            {
                broad.OnDataRecived -= MessageRecived;
                broad.StopListening();
                announcementMode = AnnouncementMode.Multicast;
            }
            else if (sender is BroadcastConnector)
            {
                if (broadcastsAhead > 10)
                {
                    multi.OnDataRecived -= MessageRecived;
                    multi.StopListening();
                    announcementMode = AnnouncementMode.Braodcast;
                }
                else
                {
                    broadcastsAhead++;
                }
            }

            var moduleName = e.Data.ModuleName.Value;
            var eventType = e.Data.Type.Value;

            if (!_knownModules.ContainsKey(moduleName))
            {
                _knownModules.TryAdd(moduleName, e.Ip);
                OnChange?.Invoke(this, new ChangeDetectedEventArgs { ModuleIp = e.Ip, ModuleName = moduleName, Type = ChangeType.ModuleAddress });
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
            logger.LogDebug($"Announcing on: {announcementMode}");
            var data = new ModuleAnnouncment { ModuleName = _moduleName };
            switch (announcementMode)
            {
                case AnnouncementMode.Both:
                    broad.Send(data);
                    multi.Send(data);
                    break;
                case AnnouncementMode.Multicast:
                    multi.Send(data);
                    break;
                case AnnouncementMode.Braodcast:
                    broad.Send(data);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void ModuleChanges()
        {
            if (string.IsNullOrEmpty(_moduleName))
            {
                throw new ArgumentException("Modulename is not allowed to be empty");
            }
            var data = new ModuleChanged { ModuleName = _moduleName };
            switch (announcementMode)
            {
                case AnnouncementMode.Both:
                    broad.Send(data);
                    multi.Send(data);
                    break;
                case AnnouncementMode.Multicast:
                    multi.Send(data);
                    break;
                case AnnouncementMode.Braodcast:
                    broad.Send(data);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void PresetChanges(string presetName)
        {
            if (string.IsNullOrEmpty(_moduleName))
            {
                throw new ArgumentException("Modulename is not allowed to be empty");
            }
            var data = new PresetChanged { ModuleName = _moduleName, PresetName = presetName };
            switch (announcementMode)
            {
                case AnnouncementMode.Both:
                    broad.Send(data);
                    multi.Send(data);
                    break;
                case AnnouncementMode.Multicast:
                    multi.Send(data);
                    break;
                case AnnouncementMode.Braodcast:
                    broad.Send(data);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void PresetDeleted(string presetName)
        {
            if (string.IsNullOrEmpty(_moduleName))
            {
                throw new ArgumentException("Modulename is not allowed to be empty");
            }
            var data = new PresetChanged { ModuleName = _moduleName, PresetName = presetName, Deleted = true };
            switch (announcementMode)
            {
                case AnnouncementMode.Both:
                    broad.Send(data);
                    multi.Send(data);
                    break;
                case AnnouncementMode.Multicast:
                    multi.Send(data);
                    break;
                case AnnouncementMode.Braodcast:
                    broad.Send(data);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
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
            multi.OnDataRecived -= MessageRecived;
            broad.OnDataRecived -= MessageRecived;
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

    public enum AnnouncementMode
    {
        Both,
        Multicast,
        Braodcast
    }
}