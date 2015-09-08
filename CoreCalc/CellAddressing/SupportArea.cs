using System;
using System.Collections.Generic;
using System.Diagnostics;

using Corecalc;

namespace CoreCalc.CellAddressing {
	/// <summary>
	/// A SupportArea is a supported  absolute cell area sheet[colInt, rowInt].
	/// </summary>
	public class SupportArea : SupportRange {
		private static readonly List<SupportArea> alreadyVisited
			= new List<SupportArea>();

		private static bool idempotentForeach;

		public readonly Interval colInt, rowInt;
		public readonly Sheet sheet;

		public SupportArea(Sheet sheet, Interval colInt, Interval rowInt) {
			this.sheet = sheet;
			this.colInt = colInt;
			this.rowInt = rowInt;
		}

		public override bool RemoveCell(SupportSet set, Sheet sheet, int col, int row) {
			if (Contains(sheet, col, row)) {
				// To exclude cell at sheet[col, row], split into up to 4 support ranges
				if (rowInt.min < row) // North, column above [col,row]
				{
					set.Add(Make(sheet, new Interval(col, col), new Interval(rowInt.min, row - 1)));
				}
				if (row < rowInt.max) // South, column below [col,row]
				{
					set.Add(Make(sheet, new Interval(col, col), new Interval(row + 1, rowInt.max)));
				}
				if (colInt.min < col) // West, block to the left of [col,row]
				{
					set.Add(Make(sheet, new Interval(colInt.min, col - 1), rowInt));
				}
				if (col < colInt.max) // East, block to the right of [col,row]
				{
					set.Add(Make(sheet, new Interval(col + 1, colInt.max), rowInt));
				}
				return true;
			}
			else {
				return false;
			}
		}

		public static bool IdempotentForeach {
			get { return idempotentForeach; }
			set {
				idempotentForeach = value;
				alreadyVisited.Clear();
			}
		}

		public override void ForEachSupported(Action<Sheet, int, int> act) {
			if (IdempotentForeach && this.Count > alreadyVisited.Count + 1) {
				for (int i = 0; i < alreadyVisited.Count; i++) {
					SupportArea old = alreadyVisited[i];
					if (this.Overlaps(old)) {
						SupportArea overlap = this.Overlap(old);
						if (overlap.Count == this.Count) { // contained in old
							return;
						}
						else if (overlap.Count == old.Count) { // contains old
							alreadyVisited[i] = this;
							ForEachExcept(overlap, act);
							return;
						}
						else if (this.colInt.Equals(old.colInt) && this.rowInt.Overlaps(old.rowInt)) {
							alreadyVisited[i] = new SupportArea(sheet, this.colInt, this.rowInt.Join(old.rowInt));
							ForEachExcept(overlap, act);
							return;
						}
						else if (this.rowInt.Equals(old.rowInt) && this.colInt.Overlaps(old.colInt)) {
							alreadyVisited[i] = new SupportArea(sheet, this.colInt.Join(old.colInt), this.rowInt);
							ForEachExcept(overlap, act);
							return;
						}
						else { // overlaps, but neither containment nor rectangular union
							alreadyVisited.Add(this);
							ForEachExcept(overlap, act);
							return;
						}
					}
				}
				// Large enough but no existing support set overlaps this one
				alreadyVisited.Add(this);
			}
			ForEachInArea(sheet, colInt, rowInt, act);
		}

		private void ForEachExcept(SupportArea overlap, Action<Sheet, int, int> act) {
			if (rowInt.min < overlap.rowInt.min) // North non-empty, columns above overlap
			{
				ForEachInArea(sheet, overlap.colInt, new Interval(rowInt.min, overlap.rowInt.min - 1), act);
			}
			if (overlap.rowInt.max < rowInt.max) // South non-empty, columns below overlap
			{
				ForEachInArea(sheet, overlap.colInt, new Interval(overlap.rowInt.max + 1, rowInt.max), act);
			}
			if (colInt.min < overlap.colInt.min) // West non-empty, rows left of overlap
			{
				ForEachInArea(sheet, new Interval(colInt.min, overlap.colInt.min - 1), rowInt, act);
			}
			if (overlap.colInt.max < colInt.max) // East non-empty, rows right of overlap
			{
				ForEachInArea(sheet, new Interval(overlap.colInt.max + 1, colInt.max), rowInt, act);
			}
		}

		private static void ForEachInArea(Sheet sheet,
										  Interval colInt,
										  Interval rowInt,
										  Action<Sheet, int, int> act) {
			for (int c = colInt.min; c <= colInt.max; c++) {
				for (int r = rowInt.min; r <= rowInt.max; r++) {
					act(sheet, c, r);
				}
			}
		}

		public override bool Contains(Sheet sheet, int col, int row) { return this.sheet == sheet && colInt.Contains(col) && rowInt.Contains(row); }

		public override int Count {
			get { return colInt.Length*rowInt.Length; }
		}

		public bool Overlaps(SupportArea that) {
			return this.sheet == that.sheet
				   && this.colInt.Overlaps(that.colInt) && this.rowInt.Overlaps(that.rowInt);
		}

		public SupportArea Overlap(SupportArea that) {
			Debug.Assert(this.Overlaps(that)); // In particular, on same sheet
			return new SupportArea(sheet,
								   this.colInt.Meet(that.colInt),
								   this.rowInt.Meet(that.rowInt));
		}

		public override string ToString() {
			CellAddr ulCa = new CellAddr(colInt.min, rowInt.min),
					 lrCa = new CellAddr(colInt.max, rowInt.max);
			return String.Format("{0}!{1}:{2}", sheet.Name, ulCa, lrCa);
		}
	}
}