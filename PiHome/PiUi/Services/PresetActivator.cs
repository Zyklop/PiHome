using System;
using System.Threading;
using System.Threading.Tasks;
using Coordinator.Modules;
using DataPersistance.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace PiUi.Services
{
    public class PresetActivator : IHostedService, IDisposable
    {
        private readonly IServiceScopeFactory scopeFactory;
        private CancellationTokenSource canceller;
        private ManualResetEvent stopDetector = new ManualResetEvent(false);

        public PresetActivator(IServiceScopeFactory scopeFactory)
        {
            this.scopeFactory = scopeFactory;
            canceller = new CancellationTokenSource();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Run(() => ActivatePresetsForever(canceller.Token));
        }

        public async Task ActivatePresetsForever(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await using(var scope = scopeFactory.CreateAsyncScope())
                    {
                        var preset = scope.ServiceProvider.GetService<PresetRepository>().GetPresetToActivate();
                        if (preset != null)
                        {
                            scope.ServiceProvider.GetService<LedController>().Activate(preset);
                        }
                    }
                }
                catch (Exception e)
                {

                }

                await Task.Delay(10000);
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