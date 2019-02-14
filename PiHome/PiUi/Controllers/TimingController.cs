﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Coordinator.Modules;
using DataPersistance.Models;
using Microsoft.AspNetCore.Mvc;
using PiUi.Models;
using Serilog;

namespace PiUi.Controllers
{
    public class TimingController : Controller
    {
        private LedController ledController;
        private ILogger logger;

        public TimingController(ILogger logger)
        {
            this.logger = logger;
            ledController = new LedController(logger);
        }

        public IActionResult Index()
        {
            var activations = ledController.GetAllActivations();
            var converted = activations.Select(x => new PresetActivationModel(x, new string[0])).ToArray();
            return View(converted);
        }

        // GET: Preset/Create
        public ActionResult Create()
        {
            var presets = ledController.GetAllPresets();
            var model = new PresetActivationModel(){AllPresets = presets.ToArray()};
            return View(model);
        }

        // GET: Preset/Edit/5
        public ActionResult Edit(int id)
        {
            var presets = ledController.GetAllPresets();
            var activation = ledController.GetAllActivations().Single(x => x.Id == id);
            var model = new PresetActivationModel(activation, presets.ToArray());
            return View(nameof(Create), model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Update(PresetActivationModel model)
        {
            var pa = new PresetActivation
            {
                Id = model.Id,
                ActivationTime = model.ActivationTime,
                Active = new BitArray(new [] {model.Active}),
                DaysOfWeek = new BitArray(new [] {model.Sunday, model.Monday, model.Tuesday, model.Wednesday, model.Thursday, model.Friday, model.Saturday})
            };
            ledController.SavePresetActivation(pa, model.SelectedPreset);

            return RedirectToAction("Index");
        }


    }
}