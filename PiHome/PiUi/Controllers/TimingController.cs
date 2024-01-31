using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Coordinator.Modules;
using DataPersistance.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PiUi.Models;

namespace PiUi.Controllers
{
    [Route("Timing")]
    public class TimingController : Controller
    {
        private LedController ledController;
        private ILogger<TimingController> logger;

        public TimingController(LedController ledController, ILogger<TimingController> logger)
        {
            this.ledController = ledController;
            this.logger = logger;
        }

        [Route("")]
        [Route("Index")]
        public IActionResult Index()
        {
            var activations = ledController.GetAllActivations();
            var converted = activations.Select(x => new PresetActivationModel(x, new string[0])).ToArray();
            return View(converted);
        }

        [Route("/Create")]
        // GET: Preset/Create
        public ActionResult Create()
        {
            var presets = ledController.GetAllPresets();
            var model = new PresetActivationModel(){AllPresets = presets.Values.ToArray(), Active = true};
            return View(model);
        }

        [HttpGet("Edit/{id}")]
        public ActionResult Edit(int id)
        {
            var presets = ledController.GetAllPresets();
            var activation = ledController.GetAllActivations().Single(x => x.Id == id);
            var model = new PresetActivationModel(activation, presets.Values.ToArray());
            return View(nameof(Create), model);
        }

        [HttpPost("Update")]
        [ValidateAntiForgeryToken]
        public ActionResult Update(PresetActivationModel model)
        {
            var pa = new PresetActivation
            {
                Id = model.Id,
                ActivationTime = new TimeOnly(model.ActivationTime.Ticks),
                Active = model.Active,
                DaysOfWeek = new [] {model.Sunday, model.Monday, model.Tuesday, model.Wednesday, model.Thursday, model.Friday, model.Saturday}
            };
            ledController.SavePresetActivation(pa, model.SelectedPreset);

            return RedirectToAction("Index");
        }
    }
}