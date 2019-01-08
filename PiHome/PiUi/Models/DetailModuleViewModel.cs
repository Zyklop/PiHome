using System.Collections.Generic;
using Coordinator.Modules;
using DataPersistance.Models;

namespace PiUi.Models
{
	public class DetailModuleViewModel
	{
		public ExtendedModule Module { get; set; }
		public List<FeatureWithLastValue> Values { get; set; }
	}
}