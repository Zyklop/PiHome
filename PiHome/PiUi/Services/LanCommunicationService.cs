using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Communication.ApiCommunication;
using Communication.Networking;
using Coordinator.Modules;
using DataPersistance.Models;
using DataPersistance.Modules;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PiUi.Services
{
    public class LanCommunicationService : IHostedService, IDisposable
    {
        private readonly CancellationTokenSource canceller;
        private readonly MasterNetworker networker;
        private readonly LedController lc;
        private readonly ILogger<LanCommunicationService> logger;
        private readonly ModuleFactory moduleFactory;
        private readonly LedController ledController;

        public LanCommunicationService(MasterNetworker networker, LedController lc,
            ILogger<LanCommunicationService> logger, ModuleFactory moduleFactory, LedController ledController)
        {
            this.networker = networker;
            this.lc = lc;
            this.logger = logger;
            this.moduleFactory = moduleFactory;
            this.ledController = ledController;
            canceller = new CancellationTokenSource();
        }

        private void ChangeDetected(object sender, ChangeDetectedEventArgs e)
        {
            try
            {
                var module = moduleFactory.GetModule(e.ModuleName);
                if (module == null)
                {
                    module = UpsertModule(e);
                }

                switch (e.Type)
                {
                    case ChangeType.ModuleAddress:
                        moduleFactory.UpdateIp(e.ModuleName, e.ModuleIp);
                        break;
                    case ChangeType.ModuleSettings:
                        var presets = GetAllPresets(module.Ip);
                        foreach (var preset1 in presets)
                        {
                            ledController.SavePreset(preset1);
                        }

                        break;
                    case ChangeType.PresetUpserted:
                        var comm = new DataCommunicator(module.Ip);
                        var preset = comm.GetPreset(e.PresetName);
                        lc.SavePreset(preset);
                        break;
                    case ChangeType.PresetDeleted:
                        lc.DeletePreset(e.PresetName);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Change parser crashed");
                throw;
            }
        }

        public PresetDto[] GetAllPresets(IPAddress ip)
        {
            var comm = new DataCommunicator(ip);
            return comm.GetAllPresets().Select(x => comm.GetPreset(x)).ToArray();
        }

        private Module UpsertModule(ChangeDetectedEventArgs args)
        {
            var newMod = moduleFactory.UpsertModule(new ModuleDto { Name = args.ModuleName }, args.ModuleIp);
            var presets = GetAllPresets(newMod.Ip);
            foreach (var preset in presets)
            {
                lc.SavePreset(preset);
            }

            return newMod;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            networker.OnChange += ChangeDetected;
            Task.Run(() => Announce(canceller.Token));
            logger.LogInformation("Communicator started");
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

            logger.LogWarning("Announcer stopped");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            canceller.Cancel();
            networker.OnChange -= ChangeDetected;
            logger.LogInformation("Communicator stopped");
        }

        public void Dispose()
        {
            canceller.Cancel();
            networker.OnChange -= ChangeDetected;
            networker.Dispose();
            logger.LogInformation("Communicator disposed");
        }
    }
}