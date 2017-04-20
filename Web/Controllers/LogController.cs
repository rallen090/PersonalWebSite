using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Web.Controllers
{
	public class LogController : Controller
	{
		[HttpGet, Route("log/start")]
		public ActionResult Start()
		{
			ConsoleInterceptor.Start();
			return Json(new { Success = true, Message = $"{nameof(ConsoleInterceptor)} now on" }, JsonRequestBehavior.AllowGet);
		}

		[HttpGet, Route("log/stop")]
		public ActionResult Stop()
		{
			ConsoleInterceptor.Stop();
			return Json(new { Success = true, Message = $"{nameof(ConsoleInterceptor)} now off" }, JsonRequestBehavior.AllowGet);
		}

		[HttpGet, Route("log")]
		[UseConsoleInterceptor]
		public ActionResult Log()
		{
			Task.Run(() =>
			{
				Enumerable.Range(0, 10)
					.ToList()
					.ForEach(i =>
					{
						Task.Delay(TimeSpan.FromSeconds(1)).Wait();
						Console.WriteLine($"Log: {i}");
					});
			});

			return this.View();
		}

		[HttpGet, Route("log/{key}")]
		public ActionResult GetLogs(string key)
		{
			var interceptor = ConsoleInterceptor.GetInterceptor();
			return Content(interceptor?.GetAllText(key) 
				?? $"ConsoleInterceptor is off. Turn it on via <a href='{this.Url.Action("Start", "Log", new { }, this.Request.Url.Scheme)}'>here</a>.");
		}
	}

	public sealed class ConsoleInterceptor : TextWriter
	{
		private static readonly Guid DefaultGuid = Guid.NewGuid();
		private static readonly AsyncLocal<string> AsyncLocalId = new AsyncLocal<string>();
		private static ConsoleInterceptor _instance;

		public static void Start()
		{
			if (_instance == null)
			{
				_instance = new ConsoleInterceptor();
			}
		}

		public static void Stop()
		{
			_instance?.Dispose();
			_instance = null;
		}

		public static void SetThreadKey()
		{
			AsyncLocalId.Value = Guid.NewGuid().ToString();
		}

		public static string GetThreadKey()
		{
			return AsyncLocalId.Value;
		}

		public static ConsoleInterceptor GetInterceptor()
		{
			return _instance;
		}

		private static readonly TextWriter DefaultOut = Console.Out;
		private static readonly TextWriter DefaultError = Console.Error;
		private readonly Dictionary<string, List<string>> _logsByRequest = new Dictionary<string, List<string>>();

		public override Encoding Encoding => Encoding.UTF8;

		private ConsoleInterceptor()
		{
			if (Console.Out != DefaultOut || Console.Error != DefaultError)
			{
				throw new InvalidOperationException("Console standard OUTPUT or ERROR is already overridden! Cannot intercept console output.");
			}
			Console.SetOut(this);
			Console.SetError(this);
		}

		public override void Write(string value)
		{
			this.AddLog(value);
		}

		public override void WriteLine(string value)
		{
			this.AddLog(value + Environment.NewLine);
		}

		public IReadOnlyList<string> GetAllLines(string key)
		{
			return this._logsByRequest.ContainsKey(key)
				// return copy of list
				? this._logsByRequest[key].ToList()
				: new List<string>();
		}

		public string GetAllText(string key)
		{
			return string.Join(string.Empty, this.GetAllLines(key));
		}

		private void AddLog(string log)
		{
			var key = this.GetKey();
			List<string> logs;
			if (this._logsByRequest.TryGetValue(key, out logs))
			{
				logs.Add(log);
			}
			else
			{
				this._logsByRequest[key] = new List<string> { log };
			}
		}

		private string GetKey() => AsyncLocalId?.Value ?? DefaultGuid.ToString();

		protected override void Dispose(bool disposing)
		{
			Console.SetOut(DefaultOut);
			Console.SetError(DefaultError);
		}
	}

	[AttributeUsage(AttributeTargets.Method)]
	public sealed class UseConsoleInterceptorAttribute : ActionFilterAttribute
	{
		public override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			ConsoleInterceptor.SetThreadKey();
		}
	}
}