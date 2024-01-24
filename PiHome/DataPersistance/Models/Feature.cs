using System;
using System.Collections.Generic;

namespace DataPersistance.Models
{
    public partial class Feature
    {
        public Feature()
        {
            LogConfiguration = new HashSet<LogConfiguration>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
        public double LogFactor { get; set; }

        public ICollection<LogConfiguration> LogConfiguration { get; set; }
        public override bool Equals(object? obj)
        {
            if (obj is Module other)
            {
                return other.Id == Id;
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
