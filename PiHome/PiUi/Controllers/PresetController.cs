using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Coordinator.Modules;
using DataPersistance.Modules;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PiUi.Models;
using Serilog;

namespace PiUi.Controllers
{
    [Route("Preset")]
    public class PresetController : Controller
    {
	    private LedController ledController;
	    private ILogger logger;

	    public PresetController(ILogger logger)
	    {
		    this.logger = logger;
		    ledController = new LedController(logger);
	    }
        
        [Route("")]
        [Route("Index")]
        public ActionResult Index()
		{
			var presetModel = new PresetOverview
			{
				Presets = ledController.GetAllPresets()
			};
			return View(presetModel);
        }

        [Route("Create")]
        public ActionResult Create()
        {
	        var model = new PresetViewModel {LedValues = ledController.GetAllLeds(), Name = "Name"};
            return View(model);
        }

        [HttpGet("Edit/{name}")]
        public ActionResult Edit(string name)
        {
			var model = new PresetViewModel
			{
				LedValues = ledController.GetPreset(name).SelectMany(x => x.Value).ToList(),
				Name = name
			};
            return View(nameof(Create), model);
		}

        [HttpGet("Activate/{name}")]
        public ActionResult Activate(string name)
	    {
			ledController.Activate(name);
		    return RedirectToAction("Index");
	    }
        
        [Route("GetAllPresets")]
		[IgnoreAntiforgeryToken]
		public ActionResult GetAllPresets()
		{
			return Json(ledController.GetAllPresets());
		}

        [HttpGet("Get/{name}")]
	    public ActionResult Get(string name)
	    {
		    return Json(ledController.GetPresetDto(name));
	    }

		[HttpPost("Save")]
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

	    [HttpPost("/Preview")]
	    [ValidateAntiForgeryToken]
	    public ActionResult Preview([FromBody] PresetViewModel collection)
	    {
		    ledController.Activate(collection.LedValues);
			return RedirectToAction(nameof(Edit), collection.Name);
	    }

        [HttpPost("Delete/{name}")]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(string name)
        {
	        ledController.DeletePreset(name);
			return RedirectToAction(nameof(Index));
        }
    }
}