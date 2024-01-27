using System;
using System.Collections.Generic;

namespace DataPersistance.Models
{
    public partial class LogConfiguration
    {
        public LogConfiguration()
        {
            Logs = new HashSet<Log>();
        }

        public int ModuleId { get; set; }
        public int FeatureId { get; set; }
        public TimeSpan Interval { get; set; }
        public DateTime NextPoll { get; set; }
        public int Id { get; set; }
        public TimeSpan? RetensionTime { get; set; }

        public virtual Feature Feature { get; set; } = null!;
        public virtual Module Module { get; set; } = null!;
        public virtual ICollection<Log> Logs { get; set; }
    }
}
