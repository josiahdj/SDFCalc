using System.Collections.Generic;

namespace CoreCalc.Types {
	public class HashBag<T> : IEnumerable<T> {
		// Invariant: for each (k,v) in multiplicity, v > 0
		private readonly IDictionary<T, int> multiplicity = new Dictionary<T, int>();

		public bool Add(T item) {
			int count;
			if (multiplicity.TryGetValue(item, out count)) {
				multiplicity[item] = count + 1;
			}
			else {
				multiplicity[item] = 1;
			}
			return true;
		}

		public bool Remove(T item) {
			int count;
			if (multiplicity.TryGetValue(item, out count)) {
				count--;
				if (count == 0) {
					multiplicity.Remove(item);
				}
				else {
					multiplicity[item] = count;
				}
				return true;
			}
			else {
				return false;
			}
		}

		public void AddAll(IEnumerable<T> xs) {
			foreach (T x in xs) {
				Add(x);
			}
		}

		public void RemoveAll(IEnumerable<T> xs) {
			foreach (T x in xs) {
				Remove(x);
			}
		}

		public IEnumerable<KeyValuePair<T, int>> ItemMultiplicities() { return multiplicity; }

		public void Clear() { multiplicity.Clear(); }

		public IEnumerator<T> GetEnumerator() {
			foreach (KeyValuePair<T, int> entry in multiplicity) {
				for (int i = 0; i < entry.Value; i++) {
					yield return entry.Key;
				}
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
	}
}