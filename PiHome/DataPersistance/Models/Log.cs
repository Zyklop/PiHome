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

        public virtual LogConfiguration LogConfiguration { get; set; } = null!;
    }
}
