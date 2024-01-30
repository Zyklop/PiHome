using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Coordinator.Modules;
using DataPersistance.Models;
using DataPersistance.Modules;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PiUi.Models;

namespace PiUi.Controllers
{
    [Route("Button")]
    public class ButtonController : Controller
    {

        public ButtonController()
        {
        }

        [Route("")]
        [Route("Index")]
        public ActionResult Index()
        {
            return View(new ButtonOverviewViewModel());
        }

        [HttpPost("{id}/Edit")]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, ButtonViewModel model)
        {
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("Add")]
        [ValidateAntiForgeryToken]
        public ActionResult Add(ButtonViewModel model)
        {
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("{id}/Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("{buttonId}/Action/Add")]
        [ValidateAntiForgeryToken]
        public ActionResult AddAction(int buttonId, ButtonActionViewModel model)
        {
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("{buttonId}/Action/{actionId}/Edit")]
        [ValidateAntiForgeryToken]
        public ActionResult EditAction(int buttonId, int actionId, ButtonActionViewModel model)
        {
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("{buttonId}/Action/{actionId}/Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteAction(int buttonId, int actionId)
        {
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("{buttonId}/Action/{actionId}/Trigger")]
        [IgnoreAntiforgeryToken]
        public ActionResult TriggerAction(int buttonId, int actionId)
        {
            return Ok();
        }
    }
}