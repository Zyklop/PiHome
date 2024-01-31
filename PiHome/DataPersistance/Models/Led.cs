using System;
using System.Collections.Generic;
using NpgsqlTypes;

namespace DataPersistance.Models
{
    public partial class Led
    {
        public Led()
        {
            LedPresetValues = new HashSet<LedPresetValue>();
        }

        public int Index { get; set; }
        public int ModuleId { get; set; }
        public NpgsqlPoint Position { get; set; }
        public int Id { get; set; }

        public virtual Module Module { get; set; } = null!;
        public virtual ICollection<LedPresetValue> LedPresetValues { get; set; }
    }
}
