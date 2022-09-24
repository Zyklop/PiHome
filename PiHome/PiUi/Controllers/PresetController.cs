using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Coordinator.Modules;
using DataPersistance.Modules;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PiUi.Models;

namespace PiUi.Controllers
{
    [Route("Preset")]
    public class PresetController : Controller
    {
        private LedController ledController;
        private ILogger<PresetController> logger;

        public PresetController(LedController ledController, ILogger<PresetController> logger)
        {
            this.ledController = ledController;
            this.logger = logger;
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
            var model = new PresetViewModel { LedValues = ledController.GetAllLeds().Select(x => new LedValueViewModel(x)).ToArray(), Name = "Name" };
            return View(model);
        }

        [HttpGet("Edit/{name}")]
        public ActionResult Edit(string name)
        {
            var model = new PresetViewModel
            {
                LedValues = ledController.GetPreset(name).SelectMany(x => x.Value.Select(y => new LedValueViewModel(y))).ToArray(),
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
        [IgnoreAntiforgeryToken]
        public ActionResult Save([FromBody] PresetViewModel collection)
        {
            try
            {
                ledController.SavePreset(collection.Name, ConvertBack(collection.LedValues));
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return RedirectToAction("Error", "Home");
            }
        }

        [HttpPost("/Preview")]
        [IgnoreAntiforgeryToken]
        public ActionResult Preview([FromBody] PresetViewModel collection)
        {
            ledController.Activate(ConvertBack(collection.LedValues));
            return RedirectToAction(nameof(Edit), collection.Name);
        }

        [HttpPost("Delete/{name}")]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(string name)
        {
            ledController.DeletePreset(name);
            return RedirectToAction(nameof(Index));
        }

        private IEnumerable<LedValue> ConvertBack(LedValueViewModel[] values)
        {
            return values.Select(x => new LedValue()
            {
                Id = x.Id,
                Color = new Color { R = (byte)x.R, Brightness = (byte)x.Brightness, B = (byte)x.B, G = (byte)x.G },
                X = x.X,
                Y = x.Y
            });
        }
    }
}