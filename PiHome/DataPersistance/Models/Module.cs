using System;
using System.Collections.Generic;
using System.Net;

namespace DataPersistance.Models
{
    public partial class Module
    {
        public Module()
        {
            Leds = new HashSet<Led>();
            LogConfigurations = new HashSet<LogConfiguration>();
        }

        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public IPAddress Ip { get; set; } = null!;
        public int[] FeatureIds { get; set; } = null!;

        public virtual ICollection<Led> Leds { get; set; }
        public virtual ICollection<LogConfiguration> LogConfigurations { get; set; }
    }
}
