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
                Presets = ledController.GetAllPresets().Values.ToArray()
            };
            return View(presetModel);
        }

        [Route("Create")]
        public ActionResult Create()
        {
            var model = new PresetViewModel { LedValues = ledController.GetAllLeds().Select(x => new LedValueViewModel(x) { Brightness = -1 }).ToArray(), Name = "Name" };
            return View(model);
        }

        [HttpGet("Edit/{name}")]
        public ActionResult Edit(string name)
        {
            var preset = ledController.GetPreset(name);
            var allLeds = ledController.GetAllLeds();
            var values = new LedValueViewModel[allLeds.Count];
            var grouped = preset.SelectMany(x => x).ToDictionary(x => x.Id, x => x);
            for (var i = 0; i < allLeds.Count; i++)
            {
                var value = allLeds[i];
                if (grouped.TryGetValue(value.Id, out var saved))
                {
                    values[i] = new LedValueViewModel(saved);
                }
                else
                {
                    values[i] = new LedValueViewModel(value) { Brightness = -1 };
                }
            }
            var model = new PresetViewModel
            {
                LedValues = values,
                Name = name
            };
            return View(nameof(Create), model);
        }

        [HttpGet("Activate/{name}")]
        public ActionResult Activate(string name)
        {
            ledController.Activate(name, true);
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
            ledController.Activate(ConvertBack(collection.LedValues), false);
            return RedirectToAction(nameof(Edit), collection.Name);
        }

        private IEnumerable<LedValue> ConvertBack(LedValueViewModel[] values)
        {
            var ledsById = ledController.GetAllLeds().ToDictionary(x => x.Id, x => x);
            foreach (var model in values)
            {
                if (model.Brightness < 0)
                {
                    continue;
                }
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