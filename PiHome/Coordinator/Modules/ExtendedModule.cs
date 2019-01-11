using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataPersistance.Models;
using Communication.ApiCommunication;
using DataPersistance.Modules;

namespace Coordinator.Modules
{
	public class ExtendedModule
	{
		private LedCommunicator leds;
		private SensorCommunicator sensor;
		private LogRepository logRepo;
		private ModuleFactory mf;

		public ExtendedModule(Module module, IEnumerable<Feature> currentFeatures, bool isLocal)
		{
			Module = module;
			leds = new LedCommunicator(module.Ip);
			sensor = new SensorCommunicator(Module.Ip);
			logRepo = new LogRepository();
			mf = new ModuleFactory();
			Features = currentFeatures.ToList();
			IsLocal = isLocal;
		}

		public Module Module { get; }

		public List<Feature> Features { get; }

		public bool IsLocal { get; }

		public double GetValue(int featureId)
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

		public string GetValueForDisplay(Feature feature)
		{
			return $"{GetValue(feature.Id)} {feature.Unit}";
		}

		public List<FeatureWithLastValue> GetAllValues()
		{
			var res = Features.Select(x => new FeatureWithLastValue {Feature = x, Lastvalue = GetValueForDisplay(x)})
				.ToList();
			return res;
		}

		public LogValues GetLogValuesForRange(int featureId, DateTime from, DateTime to, int granularity = 100)
		{
			var allLogs = logRepo.GetLogs(Module.Id, featureId, from, to);
			var tsInterval = allLogs.Max(x => x.Time) - allLogs.Min(x => x.Time);
			tsInterval /= granularity;
			var nextsplit = from;
			var feature = Features.Single(x => x.Id == featureId);
			var res = new LogValues
			{
				Name = feature.Name,
				//Unit = feature.Unit
				Values = new List<(DateTime time, decimal value)>()
			};
			foreach (var log in allLogs)
			{
				if (log.Time >= nextsplit)
				{
					res.Values.Add((time:log.Time, value:Math.Round(log.Value / (decimal)feature.LogFactor, 4)));
					while (nextsplit <= log.Time)
					{
						nextsplit += tsInterval;
					}
				}
			}

			return res;
		}

		public List<LogConfiguration> GetLogsToUpdate()
		{
			return logRepo.GetConfigurationsToUpdate(Module.Id);
		}

		public void CleanupLogs()
		{
			logRepo.DeleteOldLogs(Module.Id);
		}

		public void AddLogs(IEnumerable<LogConfiguration> configs)
		{
			var res = new Dictionary<int, double>();
			foreach (var logConfiguration in configs)
			{
				res.Add(logConfiguration.Id, GetValue(logConfiguration.FeatureId));
			}
			logRepo.LogData(res);
		}

		public void TurnLedsOff()
		{
			leds.TurnOff();
		}

		public void AddLedValues(int startIndex, double startX, double startY, int endIndex, double endX, double endY)
		{
			var ledValues = new List<LedValue>();
			var numValues = endIndex - startIndex + 1;
			var xdiff = (endX - startX)/numValues;
			var ydiff = (endY - startY)/numValues;
			for (int i = 0; i < numValues; i++)
			{
				ledValues.Add(new LedValue
				{
					Index = startIndex + i,
					ModuleId = Module.Id,
					X = startX + xdiff * i,
					Y = startY + ydiff * i
				});
			}
			mf.AddLedValues(ledValues);
		}

		public void AddFeature(int featureId, string interval)
		{
			var ts = TimeSpan.Parse(interval);
			mf.AddFeature(Module.Id, featureId, ts);
		}
	}

	public class LogValues
	{
		public string Name { get; set; }
		public string Unit { get; set; }
		public List<(DateTime time, decimal value)> Values { get; set; }
	}

	public class FeatureWithLastValue
	{
		public Feature Feature { get; set; }
		public string Lastvalue { get; set; }
	}
}
