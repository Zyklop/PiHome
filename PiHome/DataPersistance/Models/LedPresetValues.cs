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
    }
}
