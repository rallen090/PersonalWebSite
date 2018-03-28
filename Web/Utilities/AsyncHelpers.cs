using System;
using System.Collections;
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

	public class LongList<T> : IEnumerable<T>, IList<T>
	{
		private const int ListSize = Int32.MaxValue / 10;
		private List<List<T>> _lists = new List<List<T>>(1);

		private int _index = 0;

		public IEnumerator<T> GetEnumerator()
		{
			return new LongIterator<T>(ref this._lists);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void Add(T item)
		{
			if (this._lists[this._index].Count >= ListSize)
			{
				this._index++;
			}

			this._lists.Add(new List<T> { item });
		}

		public void Clear()
		{
			this._lists.ForEach(l => l.Clear());
		}

		public bool Contains(T item)
		{
			return this._lists.Any(l => l.Contains(item));
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			throw new NotImplementedException();
		}

		public bool Remove(T item)
		{
			return this._lists.Select(l => l.Remove(item)).Any();
		}

		public int Count
		{
			get { return this._lists.Sum(l => l.Count); }
		}

		public bool IsReadOnly { get; }
		public int IndexOf(T item)
		{
			throw new NotImplementedException();
		}

		public void Insert(int index, T item)
		{
			throw new NotImplementedException();
		}

		public void RemoveAt(int index)
		{
			throw new NotImplementedException();
		}

		public T this[int index]
		{
			get
			{
				var i = index / ListSize;
				return this._lists[i][index % ListSize];
			}
			set
			{
				var i = index / ListSize;
				this._lists[i][index % ListSize] = value;
			}
		}

		internal class LongIterator<T> : IEnumerator<T>
		{
			private List<List<T>> _lists;
			private int _index = 0;

			public LongIterator(ref List<List<T>> lists)
			{
				this._lists = lists;
			}

			public void Dispose()
			{
			}

			public bool MoveNext()
			{
				var i = (int)this._index / ListSize;
				if (this._lists[i].Count >= this._index + 1)
				{
					this._index++;
					return true;
				}
				return false;
			}

			public void Reset()
			{
				this._index = 0;
			}

			public T Current
			{
				get
				{
					var i = (int)this._index / ListSize;
					return this._lists[i][this._index % ListSize];
				}
			}

			object IEnumerator.Current => Current;
		}
	}
}