using System.Collections.Generic;
using DataPersistance.Modules;

namespace PiUi.Models
{
    public class PresetViewModel
    {
        public LedValueViewModel[] LedValues { get; set; }
        public string Name { get; set; }
    }

    public class LedValueViewModel
    {
        public LedValueViewModel(LedValue ledValue)
        {
            X = ledValue.X;
            Y = ledValue.Y;
            Id = ledValue.Id;
            R = ledValue.Color.R;
            G = ledValue.Color.G;
            B = ledValue.Color.B;
            Brightness = ledValue.Color.Brightness;
        }

        public double X { get; }
        public double Y { get; }
        public int R { get; set; }
        public int G { get; set; }
        public int B { get; set; }
        public int Brightness { get; }
        public int Id { get; set; }
    }
}