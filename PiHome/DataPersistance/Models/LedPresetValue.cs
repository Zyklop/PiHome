using System;
using System.Collections.Generic;

namespace DataPersistance.Models
{
    public partial class LedPresetValue
    {
        public int Id { get; set; }
        public int LedId { get; set; }
        public int PresetId { get; set; }
        public byte[]? Color { get; set; }

        public virtual Led Led { get; set; } = null!;
        public virtual LedPreset Preset { get; set; } = null!;
    }
}
