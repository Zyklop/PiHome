using System.Collections.Generic;
using Coordinator.Modules;
using DataPersistance.Models;

namespace PiUi.Models
{
    public class DetailModuleViewModel
    {
        public ExtendedModuleViewModel Module { get; set; }
        public FeatureWithLastValue[] Values { get; set; }
        public int Id { get; }
    }
}