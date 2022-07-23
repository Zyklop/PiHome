using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Coordinator.Modules;
using DataPersistance.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PiUi.Models;
using Serilog;

namespace PiUi.Controllers
{

    [Route("Module")]
    public class ModuleController : Controller
    {
	    private Coordinator.Modules.ModuleController mc;
	    private ILogger logger;

	    public ModuleController(ILogger logger)
	    {
		    this.logger = logger;
		    mc = new Coordinator.Modules.ModuleController(logger);
	    }

        [Route("")]
        [Route("Index")]
        public ActionResult Index()
        {
	        var vm = new ModuleViewModel
	        {
		        ExtendedModules = mc.Modules
	        };
            return View(vm);
        }
        [HttpGet("Detail/{id}")]
        public ActionResult Details(int id)
        {
	        var module = mc.GetModule(id);
	        var vm = new DetailModuleViewModel
			{
				Module = module,
				Values = module.GetAllValues()
			};
            return View(vm);
        }


        [Route("Log")]
        public ActionResult Log(int moduleId, int featureId, DateTime from = default(DateTime), DateTime to = default(DateTime), int granularity = 100)
	    {
		    var module = mc.GetModule(moduleId);
		    if (from == default(DateTime))
		    {
			    from = DateTime.UtcNow.AddDays(-1);
			}
		    if (to == default(DateTime))
		    {
			    to = DateTime.UtcNow;
		    }
			var vm = new LogViewModel()
		    {
			    Module = module,
				Feature = module.Features.Single(x => x.Id == featureId),
				Values = module.GetLogValuesForRange(featureId, from, to, granularity)
		    };
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
		    var module = mc.GetModule(id);
		    var vm = new EditModuleViewModel
		    {
			    ModuleId = module.Module.Id,
			    CurrentFeatures = module.Features,
			    ModuleName = module.Module.Name,
			    PossibleFeatures = mc.GetAllPossibleFeatures().Where(x => module.Features.All(y => y.Id != x.Id)).ToList()
		    };
		    return vm;
	    }

	    [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(EditModuleViewModel model)
        {
	        try
	        {
		        var mod = mc.GetCurrentModule();
		        mod.SetName(model.ModuleName);

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
		    mc.GetModule(model.ModuleId).AddFeature(model.FeatureToAdd, model.Interval);

		    return View("Edit", GetModuleViewModel(model.ModuleId));
		}

	    [HttpPost("DeleteFeature")]
	    [ValidateAntiForgeryToken]
	    public ActionResult DeleteFeature(EditModuleViewModel model)
	    {
		    throw new NotImplementedException();
			
		    var featureToDelete = model.CurrentFeatures.Single(x => x.Id == model.FeatureToDelete);
		    model.CurrentFeatures.Remove(featureToDelete);
		    model.PossibleFeatures.Add(featureToDelete);
			return View("Edit", model);
		}

	    [HttpPost("AddStrip")]
	    [ValidateAntiForgeryToken]
	    public ActionResult AddStrip(EditModuleViewModel model)
	    {
		    try
		    {
			    var module = mc.GetModule(model.ModuleId);
				module.AddLedValues(model.StartIndex, model.StartX, model.StartY, model.EndIndex, model.EndX, model.EndY);

			    return RedirectToAction(nameof(Index));
		    }
		    catch
		    {
			    return View("Edit", model);
		    }
	    }

        [Route("Settings")]
        [IgnoreAntiforgeryToken]
	    public ActionResult Settings()
	    {
		    var mod = mc.GetCurrentModule();
		    var settings = mod.GetSettings();
		    return Json(settings);
	    }
	}
}