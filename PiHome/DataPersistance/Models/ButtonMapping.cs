using System;
using System.Collections.Generic;

namespace DataPersistance.Models
{
    public partial class ButtonMapping
    {
        public int ButtonId { get; set; }
        public int ActionId { get; set; }
        public int? ToggleOnPresetId { get; set; }
        public int? ToggleOffPresetId { get; set; }
        public string? Description { get; set; }

        public virtual Button Button { get; set; } = null!;
        public virtual LedPreset? ToggleOffPreset { get; set; }
        public virtual LedPreset? ToggleOnPreset { get; set; }
    }
}
