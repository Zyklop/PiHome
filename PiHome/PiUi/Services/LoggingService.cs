using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Coordinator.Modules;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace PiUi.Services
{
	public class LoggingService : IHostedService, IDisposable
	{
		private ExtendedModule mod;
		private CancellationTokenSource canceller;
		private ManualResetEvent stopDetector = new ManualResetEvent(false);
		private ILogger logger;

		public LoggingService(ILogger logger)
		{
			this.logger = logger;
			var mc = new ModuleController(logger);
			mod = mc.GetCurrentModule();
			canceller = new CancellationTokenSource();
		}
		
		public async Task StartAsync(CancellationToken cancellationToken)
		{
			if (mod == null)
			{
				return;
			}
			Task.Run(() => UpdataLogForever(canceller.Token));
		}

		public async Task UpdataLogForever(CancellationToken token)
		{
			while (!token.IsCancellationRequested)
			{
				try
				{
					var logsToUpdate = mod.GetLogsToUpdate();
					mod.AddLogs(logsToUpdate);
					mod.CleanupLogs();
				}
				catch (Exception e)
				{
					
				}

				await Task.Delay(1000);
			}

			stopDetector.Set();
		}

		public async Task StopAsync(CancellationToken cancellationToken)
		{
			canceller.Cancel();
		}

		public void Dispose()
		{
			canceller.Cancel();
			stopDetector.WaitOne();
		}
	}
}