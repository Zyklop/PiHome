using System;
using System.Collections.Generic;
using Coordinator.Modules;
using DataPersistance.Models;

namespace PiUi.Models
{
    public class LogValuesViewModel
    {
        public LogValuesViewModel(string moduleName, string featureName, string unit, Dictionary<DateTime, decimal> values, int moduleId, int featureId)
        {
            ModuleName = moduleName;
            FeatureName = featureName;
            Unit = unit;
            Values = values;
            ModuleId = moduleId;
            FeatureId = featureId;
        }

        public string ModuleName { get; }
        public string FeatureName { get; }
        public int ModuleId { get; }
        public int FeatureId { get; }
        public string Unit { get; }
        public Dictionary<DateTime, decimal> Values { get; }
    }
}