using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using DataPersistance.Models;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace DataPersistance.Modules
{
    public class ModuleRepository
    {
        private readonly PiHomeContext context;

        public ModuleRepository(PiHomeContext context)
        {
            this.context = context;
        }

        public List<Module> GetAllModules()
        {
            return context.Modules.AsNoTracking().ToList();
        }

        public List<Feature> GetFeatures()
        {
            return context.Features.AsNoTracking().ToList();
        }

        public void AddLedValues(IEnumerable<LedValue> values)
        {
            var leds = values.ToArray();
            foreach (var ledValue in leds)
            {
                var existing =
                    context.Leds.FirstOrDefault(x => x.ModuleId == ledValue.ModuleId && x.Index == ledValue.Index);
                if (existing == null)
                {
                    context.Leds.Add(new Led
                    {
                        ModuleId = ledValue.ModuleId,
                        Index = ledValue.Index,
                        Position = new NpgsqlPoint(ledValue.X, ledValue.Y)
                    });
                }
                else
                {
                    existing.Position = new NpgsqlPoint(ledValue.X, ledValue.Y);
                }
            }
            context.SaveChanges();
        }

        public void AddFeature(int moduleId, int featureId, TimeSpan interval)
        {
            var module = context.Modules.Single(x => x.Id == moduleId);
            module.FeatureIds = module.FeatureIds.Union(new[] { featureId }).ToArray();
            module.LogConfigurations.Add(new LogConfiguration
            {
                FeatureId = featureId,
                Interval = interval,
                NextPoll = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
            });
            context.SaveChanges();
        }

        public void UpdateIp(int moduleId, IPAddress moduleIp)
        {
            var module = context.Modules.Single(x => x.Id == moduleId);
            module.Ip = moduleIp;
            context.SaveChanges();
        }

        public Module GetModule(int id)
        {
            var module = context.Modules.Include(x => x.Leds).AsNoTracking().Single(x => x.Id == id);
            return module;
        }

        public (string FeatureName, int FeatureId, int Value)[] GetAllValues(int moduleId)
        {
            return context.Logs.AsNoTracking()
                .Where(x => x.LogConfiguration.ModuleId == moduleId)
                .OrderByDescending(x => x.Time)
                .GroupBy(x => x.LogConfiguration.FeatureId,
                    x => new { x.LogConfiguration.Feature.Name, x.LogConfiguration.FeatureId, x.Value })
                .Select(x => x.FirstOrDefault())
                .AsEnumerable()
                .Select(x => (FeatureName: x.Name, FeatureId: x.FeatureId, Value: x.Value))
                .ToArray();
        }

        public Feature GetFeature(int featureId)
        {
            return context.Features.AsNoTracking().Single(x => x.Id == featureId);
        }

        public void RemoveFeature(int moduleId, int featureId)
        {
            var module = context.Modules.Include(x => x.LogConfigurations).Single(x => x.Id == moduleId);
            module.FeatureIds = module.FeatureIds.Except(new[] { featureId }).ToArray();
            var logConfig = module.LogConfigurations.Where(x => x.FeatureId == featureId);
            foreach (var config in logConfig)
            {
                context.LogConfigurations.Remove(config);
            }
            context.SaveChanges();
        }

        public void Update(int id, string moduleName, IPAddress address)
        {
            var module = context.Modules.Single(x => x.Id == id);
            module.Name = moduleName;
            module.Ip = address;
            context.SaveChanges();
        }

        public void Create(string moduleName, IPAddress address)
        {
            var module = new Module
            {
                Ip = address,
                Name = moduleName,
                FeatureIds = Array.Empty<int>()
            };
            context.Modules.Add(module);
            context.SaveChanges();
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