using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Web
{
	public static class JavaScriptCoder
	{
		public static string ScriptForAddingCss(string cssStyling)
		{
			return $@"
				var style = document.createElement('style');
				style.type = 'text/css';
				style.innerHTML = ""{cssStyling}"";
				$('head').append(style);
			";
		}
	}
}