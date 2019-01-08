using System;
using System.Collections.Generic;
using System.Linq;
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
			using (var context = new PiHomeContext())
			{
				foreach (var ledValue in values)
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

		public void AddModule(Module module)
		{
			using (var context = new PiHomeContext())
			{
				context.Add(module);
				context.SaveChanges();
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
	}
}