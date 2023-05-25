using System.Collections.Generic;
using Coordinator.Modules;
using DataPersistance.Models;

namespace PiUi.Models
{
    public class EditModuleViewModel
    {
        public int ModuleId { get; set; }
        public string ModuleName { get; set; }
        public string Ip { get; set; }
        public int FeatureToAdd { get; set; }
        public string Interval { get; set; }
        public int FeatureToDelete { get; set; }
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
        public double StartX { get; set; }
        public double EndX { get; set; }
        public double StartY { get; set; }
        public double EndY { get; set; }
        public FeatureWithLastValue[] PossibleFeatures { get; set; }
        public FeatureWithLastValue[] CurrentFeatures { get; set; }
    }
}