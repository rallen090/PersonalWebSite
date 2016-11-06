using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using Web.Utilities;

namespace Web.Controllers
{
	public class TerminalPromptController : Controller
	{
		[HttpPost, Route("terminal/prompt/scroll")]
		public ActionResult Scroll(string inputJson)
		{
			var input = JsonConvert.DeserializeObject<PromptResponseInput>(inputJson);

			var passed = CodedStringComparer.SafeEquals(input.Response.ToLowerInvariant(), PromptAnswers.ScrollAnswer);

			var result = passed 
				? new CommandResult
				{
					ResponseLines = new List<string> { LinkGenerator.GenerateEmbeddedYouTubeFrame("https://www.youtube.com/embed/E3d_Y4IriCw", startSeconds: 24), "Welcome home..." },
					Color = Color.DarkRed
				} 
				: new CommandResult { ResponseLines = new List<string> { "Begone!" }, Color = Color.DarkRed };

			return new JsonResult { Data = result };
		}

		[HttpPost, Route("terminal/prompt/cake")]
		public ActionResult Cake(string inputJson)
		{
			var input = JsonConvert.DeserializeObject<PromptResponseInput>(inputJson);

			var passed = CodedStringComparer.SafeEquals(input.Response.ToLowerInvariant(), PromptAnswers.CakeAnswer);

			var output = Enumerable.Range(0, 25).Select(s => "the cake is a lie").ToList();
			output.Add(LinkGenerator.GenerateEmbeddedYouTubeFrame("https://www.youtube.com/embed/RVInBsib04M", startSeconds: 99));
			output.Add("the cake is a lie");
			var result = passed ? new CommandResult
			{
				ResponseLines = output,
				Color = Color.Crimson
			} : new CommandResult { ResponseLines = new List<string> { "Aren't you gullible..." }, Color = Color.DarkRed };

			return new JsonResult { Data = result };
		}

		[HttpPost, Route("terminal/prompt/prize")]
		public ActionResult Prize(string inputJson)
		{
			var input = JsonConvert.DeserializeObject<PromptResponseInput>(inputJson);

			var passed = CodedStringComparer.SafeEquals(input.Response.ToLowerInvariant(), PromptAnswers.PrizeAnswer);
			var output = passed
				? new List<string>
				{
					LinkGenerator.GenerateEmbeddedYouTubeFrame("https://www.youtube.com/embed/_A8DYYZGq5k"),
					"SUCCESS!"
				}
				: new List<string>
				{
					LinkGenerator.GenerateEmbeddedYouTubeFrame("http://www.youtube.com/embed/oHg5SJYRHA0"),
					"Incorrect! Here's a consolation prize, though..."
				};
			var result = new CommandResult
			{
				ResponseLines = output,
				Color = passed ? Color.Green : Color.Red
			};

			return new JsonResult { Data = result };
		}
	}

	public class PromptResponseInput
	{
		public string Path { get; set; }
		public string Response { get; set; }
	}
}