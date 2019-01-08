using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataPersistance.Modules;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace PiUi.Controllers
{
	[Route("api/log")]
	public class LogController : Controller
	{
		private LogRepository repo;

		public LogController(LogRepository repo)
		{
			this.repo = repo;
		}

		// GET api/<controller>/5
		[HttpGet("{moduleId}/{featureId}")]
		public object Get(int moduleId, int featureId, DateTime from, DateTime to)
		{
			var logs = repo.GetLogs(moduleId, featureId, from, to);
			return logs.Select(x => new {time = x.Time, value = x.Value}).ToArray();
		}
	}
}
