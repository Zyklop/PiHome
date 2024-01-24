using System;
using System.Collections.Generic;

namespace DataPersistance.Models
{
    public partial class LedPresetValues
    {
        public int Id { get; set; }
        public int LedId { get; set; }
        public int PresetId { get; set; }
        public byte[] Color { get; set; }

        public Led Led { get; set; }
        public LedPreset Preset { get; set; }
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
