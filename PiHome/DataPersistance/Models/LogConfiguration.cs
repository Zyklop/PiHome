using System;
using System.Collections.Generic;

namespace DataPersistance.Models
{
    public partial class LogConfiguration
    {
        public LogConfiguration()
        {
            Log = new HashSet<Log>();
        }

        public int ModuleId { get; set; }
        public int FeatureId { get; set; }
        public TimeSpan Interval { get; set; }
        public DateTime NextPoll { get; set; }
        public int Id { get; set; }
        public TimeSpan? RetensionTime { get; set; }

        public Feature Feature { get; set; }
        public Module Module { get; set; }
        public ICollection<Log> Log { get; set; }
    }
}
