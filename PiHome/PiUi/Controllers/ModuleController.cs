using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Coordinator.Modules;
using DataPersistance.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PiUi.Models;

namespace PiUi.Controllers
{
    public class ModuleController : Controller
    {
	    private Coordinator.Modules.ModuleController mc;

	    public ModuleController()
	    {
		    mc = new Coordinator.Modules.ModuleController();
	    }

	    // GET: Module
        public ActionResult Index()
        {
	        var vm = new ModuleViewModel
	        {
		        ExtendedModules = mc.Modules
	        };
            return View(vm);
        }

        // GET: Module/Details/5
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

        // GET: Module/Edit/5
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

	    [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(EditModuleViewModel model)
        {
	        try
	        {
		        throw new NotImplementedException();

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View("Edit", model);
            }
		}
		
	    [HttpGet]
	    public ActionResult Delete(int moduleId)
	    {
			throw new NotImplementedException();
		    return RedirectToAction(nameof(Index));
		}
		
	    [HttpPost]
	    [ValidateAntiForgeryToken]
	    public ActionResult AddFeature(EditModuleViewModel model)
	    {
		    mc.GetModule(model.ModuleId).AddFeature(model.FeatureToAdd, model.Interval);

		    return View("Edit", GetModuleViewModel(model.ModuleId));
		}

	    [HttpPost]
	    [ValidateAntiForgeryToken]
	    public ActionResult DeleteFeature(EditModuleViewModel model)
	    {
		    throw new NotImplementedException();
			
		    var featureToDelete = model.CurrentFeatures.Single(x => x.Id == model.FeatureToDelete);
		    model.CurrentFeatures.Remove(featureToDelete);
		    model.PossibleFeatures.Add(featureToDelete);
			return View("Edit", model);
		}

	    [HttpPost]
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
	}
}