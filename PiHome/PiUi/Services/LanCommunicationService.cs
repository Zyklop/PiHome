using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Communication.Networking;
using Coordinator.Modules;
using Microsoft.Extensions.Hosting;

namespace PiUi.Services
{
	public class LanCommunicationService : IHostedService, IDisposable
	{
		private ExtendedModule mod;
		private CancellationTokenSource canceller;
		private MasterNetworker networker;
		private ModuleController mc;
		private LedController lc;

		public LanCommunicationService()
		{
			var mc = new ModuleController();
			mod = mc.GetCurrentModule();
			canceller = new CancellationTokenSource();
			networker = new MasterNetworker(mod?.Module?.Name);
			mc = new ModuleController();
			lc = new LedController();
		}

		private void ChangeDetected(object sender, ChangeDetectedEventArgs e)
		{
			var module = mc.GetModule(e.ModuleName);
			if (module == null)
			{
				module = UpsertModule(e.ModuleName, e.ModuleIp);
			}

			if (!module.Module.Ip.Equals(e.ModuleIp))
			{
				module.UpdateIp(e.ModuleIp);
			}
			switch (e.Type)
			{
				case ChangeType.ModuleAddress:
					break;
				case ChangeType.ModuleSettings:
					module.UpdateFromRemoteAsync();
					break;
				case ChangeType.PresetUpserted:
					var preset = module.DownloadPreset(e.PresetName);
					lc.SavePreset(preset);
					break;
				case ChangeType.PresetDeleted:
					lc.DeletePreset(e.PresetName);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private ExtendedModule UpsertModule(string eModuleName, IPAddress eModuleIp)
		{
			throw new NotImplementedException();
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			if (mod == null)
			{
				return;
			}
			networker.OnChange += ChangeDetected;
			Task.Run(() => Announce(canceller.Token));
		}

		public async Task Announce(CancellationToken token)
		{
			while (!token.IsCancellationRequested)
			{
				try
				{
					networker.Announce();
				}
				catch (Exception e)
				{
					
				}

				await Task.Delay(60000);
			}
		}

		public async Task StopAsync(CancellationToken cancellationToken)
		{
			canceller.Cancel();
			networker.OnChange -= ChangeDetected;
		}

		public void Dispose()
		{
			canceller.Cancel();
			networker.OnChange -= ChangeDetected;
			networker.Dispose();
		}
	}
}