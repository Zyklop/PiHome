using System;
using System.Collections.Generic;

namespace DataPersistance.Models
{
    public partial class Feature
    {
        public Feature()
        {
            LogConfigurations = new HashSet<LogConfiguration>();
        }

        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Unit { get; set; } = null!;
        public double LogFactor { get; set; }

        public virtual ICollection<LogConfiguration> LogConfigurations { get; set; }
    }
}
