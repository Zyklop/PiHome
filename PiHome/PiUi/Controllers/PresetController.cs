using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Coordinator.Modules;
using DataPersistance.Modules;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PiUi.Models;

namespace PiUi.Controllers
{
    public class PresetController : Controller
    {
	    private LedController ledController;

	    public PresetController()
	    {
			ledController = new LedController();
	    }

		// GET: Preset
		public ActionResult Index()
		{
			var presetModel = new PresetOverview
			{
				Presets = ledController.GetAllPresets()
			};
			return View(presetModel);
        }

        // GET: Preset/Create
        public ActionResult Create()
        {
	        var model = new PresetViewModel {LedValues = ledController.GetAllLeds(), Name = "Name"};
            return View(model);
        }

        // GET: Preset/Edit/5
        public ActionResult Edit(string name)
        {
			var model = new PresetViewModel
			{
				LedValues = ledController.GetPreset(name).SelectMany(x => x.Value).ToList(),
				Name = name
			};
            return View(nameof(Create), model);
		}

	    // GET: Preset/Activate/5
	    public ActionResult Activate(string name)
	    {
			ledController.Activate(name);
		    return RedirectToAction("Index");
	    }

		[IgnoreAntiforgeryToken]
		public ActionResult GetAllPresets()
		{
			return Json(ledController.GetAllPresets());
		}

	    public ActionResult Get(string name)
	    {
		    return Json(ledController.GetPresetDto(name));
	    }

		// POST: Preset/Edit/5
		[HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Save([FromBody] PresetViewModel collection)
        {
            try
            {
                ledController.SavePreset(collection.Name, collection.LedValues);
				return RedirectToAction(nameof(Index));
            }
            catch
            {
                return RedirectToAction("Error", "Home");
            }
		}

	    // POST: Preset/Preview/5
	    [HttpPost]
	    [ValidateAntiForgeryToken]
	    public ActionResult Preview([FromBody] PresetViewModel collection)
	    {
		    ledController.Activate(collection.LedValues);
			return RedirectToAction(nameof(Edit), collection.Name);
	    }

        // POST: Preset/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(string name)
        {
	        ledController.DeletePreset(name);
			return RedirectToAction(nameof(Index));
        }
    }
}