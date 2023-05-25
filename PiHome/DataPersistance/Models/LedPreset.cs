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
