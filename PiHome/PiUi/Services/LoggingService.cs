using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Communication.ApiCommunication;
using DataPersistance.Models;
using DataPersistance.Modules;
using Microsoft.Extensions.Hosting;

namespace PiUi.Services
{
	public class LoggingService : IHostedService, IDisposable
	{
		private readonly ModuleFactory moduleFactory;
		private readonly LogRepository logRepository;
		private CancellationTokenSource canceller;
		private ManualResetEvent stopDetector = new ManualResetEvent(false);
        private Module module;
        private SensorCommunicator sensor;

        public LoggingService(ModuleFactory moduleFactory)
        {
            this.moduleFactory = moduleFactory;
            canceller = new CancellationTokenSource();
        }
		
		public async Task StartAsync(CancellationToken cancellationToken)
		{
                module = moduleFactory.GetCurrentModule();
			if (module == null)
			{
				return;
			}

            sensor = new SensorCommunicator(module.Ip);
			Task.Run(() => UpdataLogForever(canceller.Token));
		}

		public async Task UpdataLogForever(CancellationToken token)
		{
			while (!token.IsCancellationRequested)
			{
				try
				{
					var logsToUpdate = logRepository.GetConfigurationsToUpdate(module.Id); var res = new Dictionary<int, double>();
                    foreach (var logConfiguration in logsToUpdate)
                    {
                        res.Add(logConfiguration.Id, GetValue(logConfiguration.FeatureId));
                    }
                    logRepository.LogData(res);
					logRepository.DeleteOldLogs(module.Id);
				}
				catch (Exception e)
				{
					
				}

				await Task.Delay(1000);
			}

			stopDetector.Set();
		}

        private double GetValue(int featureId)
        {
            switch (featureId)
            {
                case 1:
                    return sensor.GetEnvironment().Temperature;
                case 2:
                    return sensor.GetEnvironment().Humidity;
                case 3:
                    return sensor.Analog(2);
                case 4:
                    return sensor.Analog(3);
                default:
                    throw new NotImplementedException();
            }
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