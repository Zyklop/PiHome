using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Communication.ApiCommunication;
using DataPersistance.Models;
using DataPersistance.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace PiUi.Services
{
    public class LoggingService : IHostedService, IDisposable
    {
        private readonly IServiceScopeFactory scopeFactory;
        private CancellationTokenSource canceller;
        private ManualResetEvent stopDetector = new ManualResetEvent(false);

        public LoggingService(IServiceScopeFactory scopeFactory)
        {
            this.scopeFactory = scopeFactory;
            canceller = new CancellationTokenSource();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Run(() => UpdataLogForever(canceller.Token));
        }

        public async Task UpdataLogForever(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await using (var scope = scopeFactory.CreateAsyncScope())
                    {
                        var logRepository = scope.ServiceProvider.GetService<LogRepository>();
                        var moduleFactory = scope.ServiceProvider.GetService<ModuleFactory>();
                        foreach (var module in moduleFactory.GetAllModules())
                        {
                            var logsToUpdate = logRepository.GetConfigurationsToUpdate(module.Id);
                            var res = new Dictionary<int, double>();
                            var sensor = new SensorCommunicator(module.Ip);
                            foreach (var logConfiguration in logsToUpdate)
                            {
                                res.Add(logConfiguration.Id, GetValue(sensor, logConfiguration.FeatureId));
                            }

                            logRepository.LogData(res);
                            logRepository.DeleteOldLogs(module.Id);
                        }
                    }
                }
                catch (Exception e)
                {

                }

                await Task.Delay(1000);
            }

            stopDetector.Set();
        }

        private double GetValue(SensorCommunicator sensor, int featureId)
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