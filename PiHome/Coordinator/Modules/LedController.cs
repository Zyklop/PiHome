﻿using System;
using System.Collections.Generic;
using System.Linq;
using Communication.ApiCommunication;
using Communication.Networking;
using DataPersistance.Models;
using DataPersistance.Modules;
using Serilog;

namespace Coordinator.Modules
{
	public class LedController
	{
		private PresetRepository repo;
		private ModuleFactory mf = new ModuleFactory();
		private ModuleController mc;
		private ILogger logger;

		public LedController(ILogger logger)
		{
			this.logger = logger;
			repo = new PresetRepository();
			mc = new ModuleController(logger);
		}

		public List<LedValue> GetAllLeds()
		{
			return repo.GetAllLeds();
		}

		public Dictionary<Module, LedValue[]> GetPreset(string name)
		{
			return repo.GetPreset(name);
		}

		public PresetDto GetPresetDto(string name)
		{
			return repo.GetPresetDto(name);
		}

		public List<string> GetAllPresets()
		{
			return repo.GetAllPresets();
		}

		public void SavePreset(string name, IEnumerable<LedValue> leds)
		{
			repo.SavePreset(name, leds, DateTime.UtcNow);
			using (var mn = new MasterNetworker(mc.GetCurrentModule().Module.Name, logger))
			{
				mn.PresetChanges(name);
			}
		}

		public void SavePreset(PresetDto preset)
		{
			repo.SavePreset(preset.Name, repo.ToLedValues(preset.LedValues), preset.LastChangeDate);
		}

		public void DeletePreset(string name)
		{
			repo.DeletePreset(name);
			using (var mn = new MasterNetworker(mc.GetCurrentModule().Module.Name, logger))
			{
				mn.PresetDeleted(name);
			}
		}

		public void Activate(string name)
		{
			var preset = GetPreset(name);
			Activate(preset);
		}

		public void Activate(IEnumerable<LedValue> ledValues)
		{
			var mods = mf.GetAllModules().ToDictionary(x => x.Id, x => x);
			Activate(ledValues.GroupBy(x => x.ModuleId).ToDictionary(x => mods[x.Key], x => x.ToArray()));
		}

		public void Activate(Dictionary<Module, LedValue[]> values)
		{
			foreach (var valuesForModule in values)
			{
				var communicator = new LedCommunicator(valuesForModule.Key.Ip);
				var maxIndex = valuesForModule.Value.Max(x => x.Index) + 1;
				var data = new byte[maxIndex * 4];
				foreach (var ledValue in valuesForModule.Value)
				{
					Buffer.BlockCopy(ledValue.Color.ToRGBB(), 0, data, ledValue.Index * 4, 4);
				}
				communicator.SetRGBB(data);
			}
		}

		public void TurnAllOff()
		{
			foreach (var module in mf.GetAllModules())
			{
				var comm = new LedCommunicator(module.Ip);
				comm.TurnOff();
			}
		}
	}
}