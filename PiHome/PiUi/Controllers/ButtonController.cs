using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Coordinator.Modules;
using DataPersistance.Modules;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PiUi.Models;

namespace PiUi.Controllers
{
    [Route("Button")]
    public class ButtonController : Controller
    {
        private readonly ButtonRepository repo;
        private readonly LedController ledController;
        private ILogger<ButtonController> logger;

        public ButtonController(ButtonRepository repo, LedController ledController, ILogger<ButtonController> logger)
        {
            this.repo = repo;
            this.ledController = ledController;
            this.logger = logger;
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

        [HttpPost("Edit")]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(ButtonViewModel button)
        {
            repo.UpdateButton(button.Id, button.Name, button.ToggleGroup);
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

        [HttpGet("{buttonId}/Action/{actionId}/HomeAssistant")]
        [IgnoreAntiforgeryToken]
        public string GetButton(int buttonId, int actionId)
        {
            return repo.ButtonIsToggled(buttonId) ? "ON" : "OFF";
        }

        [HttpPost("{buttonId}/Action/{actionId}/HomeAssistant")]
        [IgnoreAntiforgeryToken]
        public async Task<ActionResult> SetButton(int buttonId, int actionId)
        {
            var isOn = false;
            using (StreamReader reader
                   = new StreamReader(Request.Body, Encoding.ASCII, true, 1024, true))
            {
                var bodyContent = await reader.ReadToEndAsync();
                if (bodyContent.Contains("ON"))
                {
                    isOn = true;
                }
                else if (!bodyContent.Contains("OFF"))
                {
                    logger.LogWarning("Request body expected ON or OFF, but was {0}", bodyContent);
                    return BadRequest();
                }
            }

            var presetName = repo.ButtonSetValue(buttonId, actionId, isOn);
            if (!string.IsNullOrEmpty(presetName))
            {
                ledController.Activate(presetName, true);
            }
            return Ok();
        }

    }
}