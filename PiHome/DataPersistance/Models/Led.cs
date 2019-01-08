using System;
using System.Collections.Generic;
using NpgsqlTypes;

namespace DataPersistance.Models
{
    public partial class Led
    {
        public Led()
        {
            LedPresetValues = new HashSet<LedPresetValues>();
        }

        public int Index { get; set; }
        public int ModuleId { get; set; }
        public NpgsqlPoint Position { get; set; }
        public int Id { get; set; }

        public Module Module { get; set; }
        public ICollection<LedPresetValues> LedPresetValues { get; set; }
    }
}
