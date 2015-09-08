using System;
using System.Collections.Generic;
using System.Linq;

namespace CoreCalc.Types {
	public class HashList<T> : IEnumerable<T> where T : IEquatable<T> {
		// Invariants: No duplicates in seq; seq and set have the same 
		// sets of items and the same number of items.
		private readonly List<T> seq = new List<T>();
		private readonly HashSet<T> set = new HashSet<T>();

		public bool Contains(T item) { return set.Contains(item); }

		public int Count {
			get { return seq.Count; }
		}

		public bool Add(T item) {
			if (set.Contains(item)) {
				return false;
			}
			else {
				seq.Add(item);
				set.Add(item);
				return true;
			}
		}

		public void AddAll(IEnumerable<T> xs) {
			foreach (T x in xs) {
				Add(x);
			}
		}

		public static HashList<T> Union(HashList<T> ha1, HashList<T> ha2) {
			HashList<T> result = new HashList<T>();
			result.AddAll(ha1);
			result.AddAll(ha2);
			return result;
		}

		public static HashList<T> Intersection(HashList<T> ha1, HashList<T> ha2) {
			HashList<T> result = new HashList<T>();
			foreach (T x in ha1) {
				if (ha2.Contains(x)) {
					result.Add(x);
				}
			}
			return result;
		}

		public static HashList<T> Difference(HashList<T> ha1, HashList<T> ha2) {
			HashList<T> result = new HashList<T>();
			foreach (T x in ha1) {
				if (!ha2.Contains(x)) {
					result.Add(x);
				}
			}
			return result;
		}

		public bool UnsequencedEquals(HashList<T> that) {
			if (Count != that.Count) {
				return false;
			}
			return seq.All(x => that.set.Contains(x));
		}

		public T[] ToArray() { return seq.ToArray(); }

		public IEnumerator<T> GetEnumerator() { return seq.GetEnumerator(); }

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
	}
}