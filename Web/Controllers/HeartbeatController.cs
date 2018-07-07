using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using Web.Utilities;

namespace Web.Controllers
{
	public class HeartbeatController : Controller
	{
		private static readonly TimeSpan HeartbeatFrequency = TimeSpan.FromSeconds(30);
		private static readonly object Lock = new object();
		private static Task _heartbeatTask;
		private static CancellationTokenSource _cancellationSource;
		private static DateTimeOffset _latestHeartbeat;

		private readonly HttpClient _httpClient = new HttpClient();

		//[HttpGet, Route("heartbeat")]
		//[Throttle(Name = "CommandThrottle", Milliseconds = 100)]
		//public ActionResult Heartbeat()
		//{
		//	lock (Lock)
		//	{
		//		// clean up previous heartbeat task
		//		_cancellationSource?.Cancel();
		//		_heartbeatTask?.Wait(millisecondsTimeout: 3000);
		//		_cancellationSource?.Dispose();
		//		_heartbeatTask?.Dispose();

		//		// schedule next heartbeat
		//		var url = this.Url.Action("Terminal", "Terminal", new { }, this.Request.Url.Scheme);
		//		_cancellationSource = new CancellationTokenSource();
		//		_heartbeatTask = Task.Run(() => this.ScheduleHeartbeat(new Uri(url), _cancellationSource.Token).WithCancellationCatch());
		//	}

		//	return Json(new { Success = true, LatestHeartbeat = _latestHeartbeat.ToString() }, JsonRequestBehavior.AllowGet);
		//}

		//private async Task ScheduleHeartbeat(Uri baseUrl, CancellationToken token)
		//{
		//	await Task.Delay(HeartbeatFrequency, token);
		//	_latestHeartbeat = DateTimeOffset.Now;

		//	// ping main URLs to keep warm
		//	await this._httpClient.GetAsync(baseUrl, token);
		//	await this._httpClient.GetAsync(new Uri(baseUrl, "home"), token);

		//	// ping heartbeat endpoint to schedule the next
		//	await this._httpClient.GetAsync(new Uri(baseUrl, "heartbeat"), token);
		//}

		//private async Task PingHomeAsync()
		//{
		//	await this._httpClient.GetAsync($@"http://ryanallen.io").ConfigureAwait(false);
		//}
	}
}