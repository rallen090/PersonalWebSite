using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace Web.Utilities
{
	public static class LinkGenerator
	{
		private static readonly string IFrameTemplate = "<iframe width=\"100%\" height=\"100%\" src=\"{0}?start={1}&autoplay={2}\" frameborder =\"0\" allowfullscreen></iframe>";
		private static readonly string AudioTemplate = @"
			<audio controls autoplay {2}>
				<source src=""{0}"" type=""audio/{1}"">
			</audio>
		";

		public static string GenerateEmbeddedYouTubeFrame(string link, int startSeconds = 0, bool autoplay = true)
		{
			return string.Format(IFrameTemplate, link, startSeconds, autoplay ? 1 : 0);
		}

		public static string GenerateEmbeddedSoundLink(string filePath, bool hidePlayer = true)
		{
			var extension = Path.GetExtension(filePath).TrimStart('.');
			var classForHidingPlayer = hidePlayer ? "class='background-audio'" : string.Empty;
			return string.Format(AudioTemplate, filePath, extension, classForHidingPlayer);
		}
	}
}