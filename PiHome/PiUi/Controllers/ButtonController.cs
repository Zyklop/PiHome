using System;
using System.Collections.Generic;
using System.Linq;
using Coordinator.Modules;
using DataPersistance.Modules;
using Microsoft.AspNetCore.Mvc;
using PiUi.Models;

namespace PiUi.Controllers
{
    [Route("Button")]
    public class ButtonController : Controller
    {
        private readonly ButtonRepository repo;
        private readonly LedController ledController;

        public ButtonController(ButtonRepository repo, LedController ledController)
        {
            this.repo = repo;
            this.ledController = ledController;
        }

        [Route("")]
        [Route("Index")]
        public ActionResult Index()
        {
            var buttons = repo.GetAllButtons();
            var actions = repo.GetAllActions();
            var model = new ButtonOverviewViewModel
            {
                Presets = ledController.GetAllPresets(),
                Buttons = buttons.Select(x => new ButtonViewModel
                {
                    Name = x.Name, 
                    Id = x.Id, 
                    IsOn = x.Toggled, 
                    ToggleGroup = x.ToggleGroup
                }).ToArray(),
                Actions = actions.Select(x => new ButtonActionViewModel
                {
                    Action = x.ActionId,
                    ButtonId = x.ButtonId,
                    OffPresetId = x.ToggleOffPresetId,
                    OnPresetId = x.ToggleOnPresetId,
                    Description = x.Description
                }).ToArray()
            };
            return View(model);
        }

        [HttpPost("{id}/Edit")]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, ButtonViewModel model)
        {
            repo.UpdateButton(id, model.Name, model.ToggleGroup);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("Add")]
        [ValidateAntiForgeryToken]
        public ActionResult Add(ButtonViewModel model)
        {
            repo.AddButton(model.Name, model.ToggleGroup);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("{id}/Delete")]
        public ActionResult Delete(int id)
        {
            repo.DeleteButton(id);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("Action/Add")]
        [HttpPost("{buttonId}/Action/Add")]
        [ValidateAntiForgeryToken]
        public ActionResult AddAction(int buttonId, ButtonActionViewModel model)
        {
            repo.AddAction(buttonId, model.Action, model.OnPresetId.Value, model.OffPresetId.Value, model.Description);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("{buttonId}/Action/{actionId}/Edit")]
        [ValidateAntiForgeryToken]
        public ActionResult EditAction(int buttonId, int actionId, ButtonActionViewModel model)
        {
            repo.EditAction(buttonId, actionId, model.OnPresetId.Value, model.OffPresetId.Value, model.Description);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("{buttonId}/Action/{actionId}/Delete")]
        public ActionResult DeleteAction(int buttonId, int actionId)
        {
            repo.DeleteAction(buttonId, actionId);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("{buttonId}/Action/{actionId}/Trigger")]
        [IgnoreAntiforgeryToken]
        public ActionResult TriggerAction(int buttonId, int actionId)
        {
            var presetName = repo.ButtonToggled(buttonId, actionId);
            if (!string.IsNullOrEmpty(presetName))
            {
                ledController.Activate(presetName, true);
            }
            return Ok();
        }
    }
}