using System;
using System.Collections.Generic;

namespace DataPersistance.Models
{
    public partial class PresetActivation
    {
        public int Id { get; set; }
        public int PresetId { get; set; }
        public TimeOnly ActivationTime { get; set; }
        public DateTime NextActivationTime { get; set; }
        public bool[] DaysOfWeek { get; set; } = null!;
        public bool Active { get; set; }

        public virtual LedPreset Preset { get; set; } = null!;
    }
}
