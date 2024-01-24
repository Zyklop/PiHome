using System;
using System.Collections.Generic;
using System.Collections;

namespace DataPersistance.Models
{
    public partial class PresetActivation
    {
        public int Id { get; set; }
        public int PresetId { get; set; }
        public TimeSpan ActivationTime { get; set; }
        public BitArray DaysOfWeek { get; set; }
        public BitArray Active { get; set; }
        public DateTime NextActivationTime { get; set; }

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
