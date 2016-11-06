using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Web.Utilities
{
	public static class LinkGenerator
	{
		private static readonly string IFrameTemplate = "<iframe width=\"100%\" height=\"100%\" src=\"{0}?start={1}&autoplay={2}\" frameborder =\"0\" allowfullscreen></iframe>";

		public static string GenerateEmbeddedYouTubeFrame(string link, int startSeconds = 0, bool autoplay = true)
		{
			return string.Format(IFrameTemplate, link, startSeconds, autoplay ? 1 : 0);
		}
	}
}