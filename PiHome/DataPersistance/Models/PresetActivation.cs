using System;
using System.Collections.Generic;
using System.Collections;

namespace DataPersistance.Models
{
    public partial class PresetActivation
    {
        public int Id { get; set; }
        public int PresetId { get; set; }
        public TimeOnly ActivationTime { get; set; }
        public BitArray[] DaysOfWeek { get; set; } = null!;
        public BitArray Active { get; set; } = null!;
        public DateTime NextActivationTime { get; set; }

        public virtual LedPreset Preset { get; set; } = null!;
    }
}
