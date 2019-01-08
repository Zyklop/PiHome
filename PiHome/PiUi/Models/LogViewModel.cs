using Coordinator.Modules;
using DataPersistance.Models;

namespace PiUi.Models
{
	public class LogViewModel
	{
		public ExtendedModule Module { get; set; }
		public Feature Feature { get; set; }
		public LogValues Values { get; set; }
	}
}