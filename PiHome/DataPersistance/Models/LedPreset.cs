using System;
using System.Collections.Generic;

namespace DataPersistance.Models
{
    public partial class LedPreset
    {
        public LedPreset()
        {
            LedPresetValues = new HashSet<LedPresetValues>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime ChangeDate { get; set; }

        public ICollection<LedPresetValues> LedPresetValues { get; set; }
    }
}
