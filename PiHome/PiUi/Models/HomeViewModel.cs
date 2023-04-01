using System.Collections.Generic;

namespace PiUi.Models
{
    public class HomeViewModel
    {
        public HomeViewModel(string[] presets)
        {
            Presets = presets;
        }

        public string[] Presets { get; }
    }
}