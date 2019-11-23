using System;
using System.Collections.Generic;

namespace DataPersistance.Models
{
    public partial class LedPreset
    {
        public LedPreset()
        {
            LedPresetValues = new HashSet<LedPresetValues>();
            PresetActivation = new HashSet<PresetActivation>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime ChangeDate { get; set; }

        public ICollection<LedPresetValues> LedPresetValues { get; set; }
        public ICollection<PresetActivation> PresetActivation { get; set; }
    }
}
