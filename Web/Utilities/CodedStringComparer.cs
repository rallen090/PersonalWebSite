using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Web.Utilities
{
	public static class CodedStringComparer
	{
		public const string KeyCodedNonBreakingSpace = " ";
		public const string StandardSpace = " ";

		public static bool SafeEquals(string a, string b)
		{
			return string.Equals(a.Replace(KeyCodedNonBreakingSpace, StandardSpace), b.Replace(KeyCodedNonBreakingSpace, StandardSpace), StringComparison.OrdinalIgnoreCase);
		}
	}
}