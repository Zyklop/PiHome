using System;
using System.Collections.Generic;

namespace DataPersistance.Models
{
    public partial class LedPreset
    {
        public LedPreset()
        {
            LedPresetValues = new HashSet<LedPresetValue>();
            PresetActivations = new HashSet<PresetActivation>();
        }

        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public DateTime ChangeDate { get; set; }
        public virtual ICollection<LedPresetValue> LedPresetValues { get; set; }
        public virtual ICollection<PresetActivation> PresetActivations { get; set; }
    }
}
