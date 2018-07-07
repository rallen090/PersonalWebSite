using System.Web.Mvc;
using Newtonsoft.Json;
using Web.Utilities;

namespace Web.Controllers
{
	[Pingable]
	public class TerminalController : Controller
	{
		private readonly CommandProcessor _commandProcessor;

		public TerminalController(CommandProcessor commandProcessor)
		{
			this._commandProcessor = commandProcessor;
		}

		[HttpGet, Route("")]
		public ActionResult Terminal()
		{
			return View();
		}

		[HttpPost, Route("terminal/command")]
		[Throttle(Name = "CommandThrottle", Milliseconds = 50)]
		public JsonResult Command(string inputJson)
		{
			var input = JsonConvert.DeserializeObject<UserInput>(inputJson);
			var command = this._commandProcessor.Parse(input.Command);
			var response =  this._commandProcessor.Execute(command, input.Path);
			return new JsonResult { Data = response };
		}

		[HttpPost, Route("terminal/tab/complete")]
		[Throttle(Name = "CommandThrottle", Milliseconds = 50)]
		public JsonResult TabComplete(string inputJson)
		{
			var input = JsonConvert.DeserializeObject<TabCompleteInput>(inputJson);
			var result = this._commandProcessor.TabComplete(input.Path, input.PartialWord);
			return new JsonResult { Data = result };
		}
	}

	public class UserInput
	{
		public string Command { get; set; }
		public string Path { get; set; }
	}

	public class TabCompleteInput
	{
		public string Path { get; set; }
		public string PartialWord { get; set; }
	}
}