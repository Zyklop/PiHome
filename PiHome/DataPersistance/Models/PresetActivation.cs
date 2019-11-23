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
    }
}
