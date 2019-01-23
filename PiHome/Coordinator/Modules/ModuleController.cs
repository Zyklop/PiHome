using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Communication.ApiCommunication;
using DataPersistance.Models;
using DataPersistance.Modules;
using Serilog;

namespace Coordinator.Modules
{
	public class ModuleController
	{
		private List<ExtendedModule> moduleCache;
		private ModuleFactory mf = new ModuleFactory();
		private ILogger logger;

		public ModuleController(ILogger logger)
		{
			this.logger = logger;
		}

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
						return new ExtendedModule(x, currentFeatures, x.Ip.Equals(IPAddress.Loopback), logger);
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

		public ExtendedModule UpsertModule(IPAddress ip)
		{
			var comm = new DataCommunicator(ip);
			var dto = comm.GetConfig();
			var mod = mf.UpsertModule(dto, ip);
			var old = Modules.SingleOrDefault(x => x.Module.Id == mod.Id);
			if (old != null)
			{
				moduleCache.Remove(old);
			}
			var features = mf.GetFeatures();
			var newMod = new ExtendedModule(mod, features.Where(y => mod.FeatureIds.Contains(y.Id)),
				mod.Ip.Equals(IPAddress.Loopback), logger);
			moduleCache.Add(newMod);
			return newMod;
		}

		public void UpdateIp(string name, IPAddress ip)
		{
			var mod = GetModule(name);
			mf.UpdateIp(mod.Module.Id, ip);
		}
	}
}