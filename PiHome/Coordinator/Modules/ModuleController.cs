using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using DataPersistance.Models;
using DataPersistance.Modules;

namespace Coordinator.Modules
{
	public class ModuleController
	{
		private List<ExtendedModule> moduleCache;
		private ModuleFactory mf = new ModuleFactory();

		public List<ExtendedModule> Modules
		{
			get
			{
				if (moduleCache == null)
				{
					var features = mf.GetFeatures();
					moduleCache = mf.GetAllModules().Select(x =>
					{
						var currentFeatures = features.Where(y => x.FeatureIds.Contains(y.Id));
						return new ExtendedModule(x, currentFeatures, x.Ip.Equals(IPAddress.Loopback));
					}).ToList();
				}
				return moduleCache;
			}
		}

		public List<Feature> GetAllPossibleFeatures()
		{
			return mf.GetFeatures();
		}

		public ExtendedModule GetModule(string name)
		{
			return Modules.SingleOrDefault(x => x.Module.Name == name);
		}

		public ExtendedModule GetModule(int id)
		{
			return Modules.SingleOrDefault(x => x.Module.Id == id);
		}

		public ExtendedModule GetCurrentModule()
		{
			return Modules.FirstOrDefault(x => x.IsLocal);
		}

		public void AddModule(string name, string ip = "127.0.0.1")
		{
			mf.AddModule(new Module
			{
				FeatureIds = new int[0],
				Ip = IPAddress.Parse(ip),
				Name = name
			});
		}
	}
}