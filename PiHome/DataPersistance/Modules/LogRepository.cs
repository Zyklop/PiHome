using System;
using System.Collections.Generic;
using System.Linq;
using DataPersistance.Models;
using Microsoft.EntityFrameworkCore;

namespace DataPersistance.Modules
{
    public class LogRepository
    {
        private readonly PiHomeContext context;

        public LogRepository(PiHomeContext context)
        {
            this.context = context;
        }

        public void LogData(int moduleId, int featureId, double value)
        {
            var config = context.LogConfigurations.Include(x => x.Feature)
                .SingleOrDefault(x => x.FeatureId == featureId && x.ModuleId == moduleId);

            if (config != null)
            {
                LogData(config, (int)Math.Round(value * config.Feature.LogFactor));
            }
        }

        private void LogData(LogConfiguration config, int value)
        {
            var log = new Log
            {
                LogConfigurationId = config.Id,
                Time = DateTime.UtcNow,
                Value = value
            };
            config.NextPoll = DateTime.UtcNow.Add(config.Interval);
            context.Add(log);
            context.SaveChanges();
        }

        public void LogData(int configId, int value)
        {
            var config = context.LogConfigurations.SingleOrDefault(x => x.Id == configId);

            if (config != null)
            {
                LogData(config, value);
            }
        }

        public void LogData(Dictionary<int, double> values)
        {
            var configIds = values.Keys.ToArray();
            var configs = context.LogConfigurations.Include(x => x.Feature).Where(x => configIds.Contains(x.Id))
                .ToDictionary(x => x.Id, x => x);
            foreach (var value in values)
            {
                context.Add(new Log
                {
                    LogConfigurationId = value.Key,
                    Time = DateTime.UtcNow,
                    Value = (int)Math.Round(value.Value * configs[value.Key].Feature.LogFactor),
                });
                configs.TryGetValue(value.Key, out var lc);
                if (lc != null)
                {
                    lc.NextPoll = DateTime.UtcNow.Add(lc.Interval);
                }
            }

            context.SaveChanges();
        }

        public List<Log> GetLogs(int logConfigId, DateTime from, DateTime to)
        {
            return context.Logs
                .Where(x => x.LogConfigurationId == logConfigId && x.Time > from && x.Time < to)
                .AsNoTracking()
                .ToList();
        }

        public List<Log> GetLogs(int moduleId, int featureId, DateTime from, DateTime to)
        {
            return context.LogConfigurations
                .Where(x => x.ModuleId == moduleId && x.FeatureId == featureId)
                .SelectMany(x => x.Logs)
                .Where(x => x.Time > from && x.Time < to)
                .AsNoTracking()
                .OrderBy(x => x.Time)
                .ToList();
        }

        public List<LogConfiguration> GetConfigurationsToUpdate(int moduleId)
        {
            return context.LogConfigurations.Where(x => x.ModuleId == moduleId && x.NextPoll < DateTime.UtcNow)
                .AsNoTracking().ToList();
        }

        public void DeleteOldLogs(int moduleId)
        {
            var configs = context.LogConfigurations.Where(x => x.ModuleId == moduleId && x.RetensionTime != null)
                .AsNoTracking();
            var logsToRem = new List<Log>();
            foreach (var config in configs)
            {
                var data = DateTime.UtcNow.Subtract(config.RetensionTime.Value);
                logsToRem.AddRange(context.Logs.Where(x => x.LogConfigurationId == config.Id && x.Time < data));
            }

            if (logsToRem.Any())
            {
                context.Logs.RemoveRange(logsToRem);
                context.SaveChanges();
            }
        }
    }
}