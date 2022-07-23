using System.Collections.Generic;
using System.Net;
using Coordinator.Modules;
using DataPersistance.Models;
using DataPersistance.Modules;

namespace PiUi.Models
{
    public class ModulesViewModel
    {
        public ExtendedModuleViewModel[] ExtendedModules { get; set; }
    }

    public class ExtendedModuleViewModel
    {
        public ExtendedModuleViewModel(Module module)
        {
            Name = module.Name;
            IsLocal = Equals(module.Ip, IPAddress.Loopback);
            Ip = module.Ip.ToString();
            Id = module.Id;
        }

        public string Name { get; }
        public bool IsLocal { get; }
        public string Ip { get; }
        public int Id { get; }
    }
}