using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Web.Utilities
{
	public class RecurringAction : IDisposable
	{
		private readonly Timer _timer;
		private readonly Func<Task> _action;
		private readonly SemaphoreSlim _actionSemaphore;
		private readonly bool _allowConcurrentActions;

		public RecurringAction(Func<Task> action, TimeSpan frequency, bool allowConcurrentActions = false)
		{
			this._allowConcurrentActions = allowConcurrentActions;
			this._action = action;
			this._actionSemaphore = allowConcurrentActions
				? null
				// only require the semaphore if we are preventing concurrent actions
				: new SemaphoreSlim(initialCount: 1, maxCount: 1);
			this._timer = new Timer(async _ => await this.TriggerAsync().ConfigureAwait(false));

			// start timer
			this._timer.Change(TimeSpan.Zero, frequency);
		}

		public async Task TriggerAsync()
		{
			if (this._allowConcurrentActions)
			{
				await this._action().ConfigureAwait(false);
			}
			else if (await this._actionSemaphore.WaitAsync(TimeSpan.Zero).ConfigureAwait(false))
			{
				await this._action().ConfigureAwait(false);

				this._actionSemaphore.Release();
			}
		}

		public void Dispose()
		{
			this._timer.Dispose();
			this._actionSemaphore?.Dispose();
		}
	}
}