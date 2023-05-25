using System;
using System.Collections.Generic;
using System.Net;

namespace DataPersistance.Models
{
    public partial class Module
    {
        public Module()
        {
            Led = new HashSet<Led>();
            LogConfiguration = new HashSet<LogConfiguration>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public IPAddress Ip { get; set; }
        public int[] FeatureIds { get; set; }

        public ICollection<Led> Led { get; set; }
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
