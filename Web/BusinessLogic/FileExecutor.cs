using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Web.Utilities;

namespace Web
{
	public class FileExecutor
	{
		private static readonly string BarrelRollCss = ".terminal { -moz-animation-name: roll; -moz-animation-duration: 4s; -moz-animation-iteration-count: 1; -webkit-animation-name: roll; -webkit-animation-duration: 4s; -webkit-animation-iteration-count: 1; }";

		public static CommandResult Execute(UrlHelper urlHelper, FileInfo fileInfo)
		{
			switch (fileInfo.Name)
			{
				case "scroll.sh":
					return new CommandResult
					{
						ResponseLines = new List<string> { "What is the color of night?" },
						PromptResponsePostUrl = new Uri(urlHelper.Action("Scroll", "TerminalPrompt", routeValues: null, protocol: HttpContext.Current.Request.Url.Scheme)),
						Color = Color.DarkRed
					};
				case "cake.exe":
					return new CommandResult
					{
						ResponseLines = new List<string> { "There will be cake! Yeah?" },
						PromptResponsePostUrl = new Uri(urlHelper.Action("Cake", "TerminalPrompt", routeValues: null, protocol: HttpContext.Current.Request.Url.Scheme)),
						Color = Color.Crimson
					};
				case "prize.exe":
					return new CommandResult
					{
						ResponseLines = new List<string> { "The prize requires a password! Enter password:" },
						PromptResponsePostUrl = new Uri(urlHelper.Action("Prize", "TerminalPrompt", routeValues: null, protocol: HttpContext.Current.Request.Url.Scheme)),
						Color = Color.DeepPink
					};
				case "roll.sh":
					var path = "/Content/Audio/roll.wav";
					return new CommandResult
					{
						ResponseLines = new List<string> { LinkGenerator.GenerateEmbeddedSoundLink(path), "Do a barrel roll!" },
						Color = Color.OrangeRed,
						JavaScriptCode = JavaScriptCoder.ScriptForAddingCss(BarrelRollCss)
					};
				default:
					throw new InvalidOperationException($"Cannot execute file {fileInfo.Name}");
			}
		}
	}
}