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

        [HttpGet("Delete/{name}")]
        public ActionResult Delete(string name)
        {
            ledController.DeletePreset(name);
            return RedirectToAction(nameof(Index));
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

        [HttpPost("Preview")]
        [IgnoreAntiforgeryToken]
        public ActionResult Preview([FromBody] PresetViewModel collection)
        {
            ledController.Activate(ConvertBack(collection.LedValues));
            return RedirectToAction(nameof(Edit), collection.Name);
        }

        private IEnumerable<LedValue> ConvertBack(LedValueViewModel[] values)
        {
            var ledsById = ledController.GetAllLeds().ToDictionary(x => x.Id, x => x);
            foreach (var model in values)
            {
                var fromDb = ledsById[model.Id];
                yield return new LedValue()
                {
                    Id = model.Id,
                    Color = new Color { R = (byte)model.R, Brightness = (byte)model.Brightness, B = (byte)model.B, G = (byte)model.G },
                    X = fromDb.X,
                    Y = fromDb.Y,
                    Index = fromDb.Index,
                    ModuleId = fromDb.ModuleId
                };
            }
        }
    }
}