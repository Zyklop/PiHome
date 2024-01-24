using System;
using System.Collections.Generic;

namespace DataPersistance.Models
{
    public partial class Log
    {
        public DateTime Time { get; set; }
        public int Value { get; set; }
        public long Id { get; set; }
        public int LogConfigurationId { get; set; }

        public LogConfiguration LogConfiguration { get; set; }
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
