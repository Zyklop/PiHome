using System;
using System.Collections.Generic;
using System.Linq;
using Communication.ApiCommunication;
using DataPersistance.Models;
using DataPersistance.Modules;
using Microsoft.Extensions.Logging;

namespace Coordinator.Modules
{
    public class LedController
    {
        private static readonly byte[] IgnoreData = new byte[] { 0, 0, 0, 101 };

        private PresetRepository repo;
        private ModuleRepository mf;
        private ILogger<LedController> logger;
        private CommunicatorFactory commFac;

        public LedController(ILogger<LedController> logger, ModuleRepository mf, PresetRepository repo, CommunicatorFactory commFac)
        {
            this.logger = logger;
            this.mf = mf;
            this.repo = repo;
            this.commFac = commFac;
        }

        public List<LedValue> GetAllLeds()
        {
            return repo.GetAllLeds();
        }

        public ILookup<Module, LedValue> GetPreset(string name)
        {
            return repo.GetPreset(name);
        }

        public Dictionary<int, string> GetAllPresets()
        {
            return repo.GetAllPresets();
        }

        public void SavePreset(string name, IEnumerable<LedValue> leds)
        {
            repo.SavePreset(name, leds, DateTime.UtcNow);
        }

        public void SavePreset(PresetDto preset)
        {
            repo.SavePreset(preset.Name, repo.ToLedValues(preset.LedValues), preset.LastChangeDate);
        }

        public void DeletePreset(string name)
        {
            repo.DeletePreset(name);
        }

        public void Activate(string name, bool fade)
        {
            var preset = GetPreset(name);
            Activate(preset, fade);
        }

        public void Activate(IEnumerable<LedValue> ledValues, bool fade)
        {
            var mods = mf.GetAllModules().ToDictionary(x => x.Id, x => x);
            Activate(ledValues.ToLookup(x => mods[x.ModuleId]), fade);
        }

        public void Activate(ILookup<Module, LedValue> values, bool fade)
        {
            foreach (var valuesForModule in values)
            {
                var communicator = commFac.GetLedCommunicator(valuesForModule.Key.Ip);
                var maxIndex = valuesForModule.Max(x => x.Index) + 1;
                var valueLookup = valuesForModule.ToDictionary(x => x.Index, x => x.Color);
                var arr = new byte[maxIndex * 4];
                var data = arr.AsSpan();
                for (int i = 0; i < maxIndex; i++)
                {
                    if(valueLookup.TryGetValue(i, out var value))
                    {
                        value.ToRGBB().AsSpan().CopyTo(data);
                    }
                    else
                    {
                        IgnoreData.AsSpan().CopyTo(data);
                    }
                    data = data.Slice(4);
                }
                communicator.SetRGBB(arr, fade);
            }
        }

        public void TurnAllOff()
        {
            foreach (var module in mf.GetAllModules())
            {
                var comm = commFac.GetLedCommunicator(module.Ip);
                comm.TurnOff();
            }
        }

        public PresetActivation[] GetAllActivations()
        {
            return repo.GetAllPresetActivations();
        }

        public void SavePresetActivation(PresetActivation activation, string presetName)
        {
            repo.UpdatePresetActivation(activation, presetName);
        }
    }
}