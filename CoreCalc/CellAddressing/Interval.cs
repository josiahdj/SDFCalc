using System;
using System.Diagnostics;

namespace CoreCalc.CellAddressing {
	/// <summary>
	/// An Interval is a non-empty integer interval [min...max].
	/// </summary>
	public struct Interval : IEquatable<Interval> {
		public readonly int min, max; // Assume min<=max

		public Interval(int min, int max) {
			Debug.Assert(min <= max);
			this.min = min;
			this.max = max;
		}

		public void ForEach(Action<int> act) {
			for (int i = min; i <= max; i++) {
				act(i);
			}
		}

		public bool Contains(int i) { return min <= i && i <= max; }

		public int Length {
			get { return max - min + 1; }
		}

		public bool Overlaps(Interval that) {
			return this.min <= that.min && that.min <= this.max
				   || that.min <= this.min && this.min <= that.max;
		}

		// When the intervals overlap, this is their union:
		public Interval Join(Interval that) { return new Interval(Math.Min(this.min, that.min), Math.Max(this.max, that.max)); }

		// When the intervals overlap, this is their intersection:
		public Interval Meet(Interval that) { return new Interval(Math.Max(this.min, that.min), Math.Min(this.max, that.max)); }

		public bool Equals(Interval that) { return this.min == that.min && this.max == that.max; }
	}
}