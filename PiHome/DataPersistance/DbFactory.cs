using System.Dynamic;
using DataPersistance.Models;

namespace DataPersistance
{
	internal class DbFactory
	{
		private static PiHomeContext context;

		public static PiHomeContext GetContext()
		{
			if (context == null)
			{
				context = new PiHomeContext();
			}
			return context;
		}
	}
}