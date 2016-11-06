using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Web.Utilities
{
	public class TwoWayMap<T1, T2>
	{
		private readonly Dictionary<T1, T2> _forward = new Dictionary<T1, T2>();
		private readonly Dictionary<T2, T1> _reverse = new Dictionary<T2, T1>();

		public TwoWayMap()
		{
			this.Forward = new Indexer<T1, T2>(_forward);
			this.Reverse = new Indexer<T2, T1>(_reverse);
		}

		public class Indexer<T3, T4>
		{
			private readonly Dictionary<T3, T4> _dictionary;
			public Indexer(Dictionary<T3, T4> dictionary)
			{
				_dictionary = dictionary;
			}
			public T4 this[T3 index]
			{
				get { return this._dictionary[index]; }
				set { this._dictionary[index] = value; }
			}

			public bool TryGetValue(T3 index, out T4 value)
			{
				return this._dictionary.TryGetValue(index, out value);
			}
		}

		public void Add(T1 t1, T2 t2)
		{
			this._forward.Add(t1, t2);
			this._reverse.Add(t2, t1);
		}

		public Indexer<T1, T2> Forward { get; private set; }
		public Indexer<T2, T1> Reverse { get; private set; }
	}
}