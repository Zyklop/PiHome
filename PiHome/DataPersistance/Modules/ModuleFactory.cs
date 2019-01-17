using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using DataPersistance.Models;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace DataPersistance.Modules
{
	public class ModuleFactory
	{
		public ModuleFactory()
		{
		}

		public List<Module> GetAllModules()
		{
			using (var context = new PiHomeContext())
			{
				return context.Module.AsNoTracking().ToList();
			}
		}

		public List<Feature> GetFeatures()
		{
			using (var context = new PiHomeContext())
			{
				return context.Feature.AsNoTracking().ToList();
			}
		}

		public void AddLedValues(IEnumerable<LedValue> values)
		{
			var leds = values.ToArray();
			using (var context = new PiHomeContext())
			{
				foreach (var ledValue in leds)
				{
					context.Led.Add(new Led
					{
						ModuleId = ledValue.ModuleId,
						Index = ledValue.Index,
						Position = new NpgsqlPoint(ledValue.X, ledValue.Y)
					});
				}
				context.SaveChanges();
			}
		}

		public Module UpsertModule(ModuleDto mod, IPAddress ip)
		{
			using (var context = new PiHomeContext())
			{
				var module = context.Module.SingleOrDefault(x => x.Name == mod.Name);
				var existingLeds = new Dictionary<int, Led>();
				if (module == null)
				{
					module = new Module
					{
						Name = mod.Name,
						FeatureIds = mod.FeatureIds,
						Ip = ip
					};
					context.Add(module);
				}
				else
				{
					module.FeatureIds = mod.FeatureIds;
					module.Ip = ip;
					existingLeds = context.Led.Where(x => x.ModuleId == module.Id).ToDictionary(x => x.Index, x => x);
				}
				context.Led.RemoveRange(existingLeds.Where(x => mod.Leds.All(y => y.Index != x.Key)).Select(x => x.Value));
				foreach (var ledValue in mod.Leds)
				{
					if (!existingLeds.TryGetValue(ledValue.Index, out var led))
					{
						led = new Led{Index = ledValue.Index, Module = module};
						context.Led.Add(led);
					}
					led.Position = new NpgsqlPoint(ledValue.X, ledValue.Y);
				}
				context.SaveChanges();
				return module;
			}
		}

		public void AddFeature(int moduleId, int featureId, TimeSpan interval)
		{
			using (var context = new PiHomeContext())
			{
				var module = context.Module.Single(x => x.Id == moduleId);
				module.FeatureIds = module.FeatureIds.Union(new[] {featureId}).ToArray();
				module.LogConfiguration.Add(new LogConfiguration
				{
					FeatureId = featureId,
					Interval = interval,
					NextPoll = DateTime.UtcNow
				});
				context.SaveChanges();
			}
		}

		public void UpdateIp(int moduleId, IPAddress moduleIp)
		{
			using (var context = new PiHomeContext())
			{
				var module = context.Module.Single(x => x.Id == moduleId);
				module.Ip = moduleIp;
				context.SaveChanges();
			}
		}

		public void SetName(int id, string moduleName)
		{
			using (var context = new PiHomeContext())
			{
				var module = context.Module.Single(x => x.Id == id);
				module.Name = moduleName;
				context.SaveChanges();
			}
		}

		public ModuleDto GetModule(int id)
		{
			using (var context = new PiHomeContext())
			{
				var module = context.Module.Include(x => x.LogConfiguration).AsNoTracking().Single(x => x.Id == id);
				return new ModuleDto
				{
					Name = module.Name, FeatureIds = module.FeatureIds,
					Leds = module.Led.Select(x => new LedDto {Index = x.Index, X = x.Position.X, Y = x.Position.Y})
						.ToArray()
				};
			}
		}
	}

	public class LedDto
	{
		public int Index { get; set; }
		public double X { get; set; }
		public double Y { get; set; }
	}

	public class ModuleDto
	{
		public string Name { get; set; }
		public int[] FeatureIds { get; set; }
		public LedDto[] Leds { get; set; }
	}
}