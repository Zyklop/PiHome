using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Coordinator.Modules;
using DataPersistance.Models;
using DataPersistance.Modules;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PiUi.Models;

namespace PiUi.Controllers
{
    [Route("Module")]
    public class ModuleController : Controller
    {
        private ILogger<ModuleController> logger;
        private readonly ModuleFactory moduleFactory;
        private readonly LogRepository logRepository;

        public ModuleController(ILogger<ModuleController> logger, ModuleFactory moduleFactory, LogRepository logRepository)
        {
            this.logger = logger;
            this.moduleFactory = moduleFactory;
            this.logRepository = logRepository;
        }

        [Route("")]
        [Route("Index")]
        public ActionResult Index()
        {
            var vm = new ModulesViewModel
            {
                ExtendedModules = moduleFactory.GetAllModules().Select(x => new ExtendedModuleViewModel(x)).ToArray()
            };
            return View(vm);
        }
        [HttpGet("Detail/{id}")]
        public ActionResult Details(int id)
        {
            var module = moduleFactory.GetModule(id);
            var vm = new DetailModuleViewModel
            {
                Module = new ExtendedModuleViewModel(module),
                Values = moduleFactory.GetAllValues(module.Id).Select(x => new FeatureWithLastValue(x.FeatureName, x.FeatureId, x.Value.ToString())).ToArray()
            };
            return View(vm);
        }


        [Route("Log")]
        public ActionResult Log(int moduleId, int featureId, DateTime from = default(DateTime), DateTime to = default(DateTime), int granularity = 100)
        {
            var module = moduleFactory.GetModule(moduleId);
            if (from == default(DateTime))
            {
                from = DateTime.UtcNow.AddDays(-1);
            }
            if (to == default(DateTime))
            {
                to = DateTime.UtcNow;
            }

            var feature = moduleFactory.GetFeature(featureId);
            var logs = logRepository.GetLogs(moduleId, featureId, from, to);
            var vm = new LogValuesViewModel(module.Name, feature.Name, feature.Unit,
                logs.ToDictionary(x => x.Time, x => (decimal)x.Value), module.Id, feature.Id);
            return View(vm);
        }

        [HttpGet("Edit/{id}")]
        public ActionResult Edit(int id)
        {
            var vm = GetModuleViewModel(id);
            return View(vm);
        }

        private EditModuleViewModel GetModuleViewModel(int id)
        {
            var module = moduleFactory.GetModule(id);
            var moduleFeatures = moduleFactory.GetFeatures().ToDictionary(x => x.Id, x => new FeatureWithLastValue(x.Name, x.Id, string.Empty));
            var vm = new EditModuleViewModel
            {
                ModuleId = module.Id,
                CurrentFeatures = module.FeatureIds.Select(x => moduleFeatures[x]).ToArray(),
                ModuleName = module.Name,
                PossibleFeatures = moduleFeatures.Where(x => !module.FeatureIds.Contains(x.Key)).Select(x => x.Value).ToArray(),
                Ip = module.Ip.ToString()
            };
            return vm;
        }

        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, EditModuleViewModel model)
        {
            if (!IPAddress.TryParse(model.Ip, out var ip))
            {
                return BadRequest("Invalid Ip");
            }
            try
            {
                moduleFactory.Update(id, model.ModuleName, ip);

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View("Edit", model);
            }
        }

        [HttpGet("Add")]
        public ActionResult Add()
        {
            var vm = new EditModuleViewModel();
            return View(vm);
        }

        [HttpPost("Add")]
        [ValidateAntiForgeryToken]
        public ActionResult Add(EditModuleViewModel model)
        {
            if (!IPAddress.TryParse(model.Ip, out var ip))
            {
                return BadRequest("Invalid Ip");
            }
            try
            {
                moduleFactory.Create(model.ModuleName, ip);

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View("Edit", model);
            }
        }

        [HttpPost("AddFeature")]
        [ValidateAntiForgeryToken]
        public ActionResult AddFeature(EditModuleViewModel model)
        {
            moduleFactory.AddFeature(model.ModuleId, model.FeatureToAdd, TimeSpan.Parse(model.Interval));

            return View("Edit", GetModuleViewModel(model.ModuleId));
        }

        [HttpPost("DeleteFeature")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteFeature(EditModuleViewModel model)
        {
            moduleFactory.RemoveFeature(model.ModuleId, model.FeatureToDelete);
            return View("Edit", GetModuleViewModel(model.ModuleId));
        }

        [HttpPost("AddStrip")]
        [ValidateAntiForgeryToken]
        public ActionResult AddStrip(EditModuleViewModel model)
        {
            try
            {
                var ledValues = new List<LedValue>();
                var numValues = model.EndIndex - model.StartIndex + 1;
                var xdiff = (model.EndX - model.StartX) / numValues;
                var ydiff = (model.EndY - model.StartY) / numValues;
                for (int i = 0; i < numValues; i++)
                {
                    ledValues.Add(new LedValue
                    {
                        Index = model.StartIndex + i,
                        ModuleId = model.ModuleId,
                        ModuleName = model.ModuleName,
                        X = model.StartX + xdiff * i,
                        Y = model.StartY + ydiff * i
                    });
                }
                moduleFactory.AddLedValues(ledValues);

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View("Edit", model);
            }
        }
    }
}