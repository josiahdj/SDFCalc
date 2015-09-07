using System;
using System.Collections.Generic;

namespace Corecalc {
	/// <summary>
	/// Machinery to store objects of type T for later access via an integer index.
	/// </summary>
	/// <typeparam name="T">The type of item stored in the array</typeparam>

	sealed class ValueTable<T> where T : IEquatable<T> {
		private readonly IDictionary<T, int> dict = new Dictionary<T, int>();
		private readonly IList<T> array = new List<T>();

		public int GetIndex(T x) {
			int index;
			if (!dict.TryGetValue(x, out index)) {
				index = array.Count;
				array.Add(x);
				dict.Add(x, index);
			}
			return index;
		}

		public T this[int index] {
			get { return array[index]; }
		}
	}
}