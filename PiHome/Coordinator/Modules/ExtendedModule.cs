using System;

namespace Coordinator.Modules
{
    public class LogValues
    {
        public LogValues(string name, string unit, List<(DateTime time, decimal value)> values)
        {
            Name = name;
            Unit = unit;
            Values = values;
        }

        public string Name { get; set; }
        public string Unit { get; set; }
        public List<(DateTime time, decimal value)> Values { get; set; }
    }

    public class FeatureWithLastValue
    {
        public FeatureWithLastValue(string featureName, int featureId, string lastValue)
        {
            FeatureName = featureName;
            FeatureId = featureId;
            LastValue = lastValue;
        }

        public string FeatureName { get; }
        public int FeatureId { get; }
        public string LastValue { get; }
    }
}
