using System;
using System.Collections.Generic;

namespace DataPersistance.Models
{
    public partial class Button
    {
        public Button()
        {
            ButtonMappings = new HashSet<ButtonMapping>();
        }

        public int Id { get; set; }
        public string? Name { get; set; }
        public bool Toggled { get; set; }
        public int ToggleGroup { get; set; }
        public DateTime? LastActivation { get; set; }

        public virtual ICollection<ButtonMapping> ButtonMappings { get; set; }
    }
}
