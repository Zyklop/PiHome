using System;
using System.Threading;
using System.Threading.Tasks;
using Coordinator.Modules;
using DataPersistance.Modules;
using Microsoft.Extensions.Hosting;

namespace PiUi.Services
{
    public class PresetActivator : IHostedService, IDisposable
    {
        private PresetRepository repo;
        private LedController lc;
        private CancellationTokenSource canceller;
        private ManualResetEvent stopDetector = new ManualResetEvent(false);

        public PresetActivator(PresetRepository repo, LedController lc)
        {
            this.repo = repo;
            this.lc = lc;
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
                    var preset = repo.GetPresetToActivate();
                    if (preset != null)
                    {
                        lc.Activate(preset);
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