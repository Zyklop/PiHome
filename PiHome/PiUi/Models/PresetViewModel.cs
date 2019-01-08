using System.Collections.Generic;
using DataPersistance.Modules;

namespace PiUi.Models
{
	public class PresetViewModel
	{
		public List<LedValue> LedValues { get; set; }
		public string Name { get; set; }
	}
}