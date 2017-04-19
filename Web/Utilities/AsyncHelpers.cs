using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace Web.Utilities
{
	public static class AsyncHelpers
	{
		public static async Task WithCancellationCatch(this Task @this)
		{
			try
			{
				await @this;
			}
			catch (TaskCanceledException ex)
			{
				Console.WriteLine(ex);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
		}
	}
}