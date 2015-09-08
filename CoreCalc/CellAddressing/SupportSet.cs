using System;
using System.Collections.Generic;

using Corecalc;

namespace CoreCalc.CellAddressing {
	/// <summary>
	/// A SupportSet represents the set of cells supported by a given cell;
	/// ie. those cells that refer to it.
	/// </summary>
	public class SupportSet {
		private readonly List<SupportRange> ranges = new List<SupportRange>();

		// Add suppSheet[suppCols, suppRows] except sheet[col,row] to support set
		public void AddSupport(Sheet sheet,
							   int col,
							   int row,
							   Sheet suppSheet,
							   Interval suppCols,
							   Interval suppRows) {
			SupportRange range = SupportRange.Make(suppSheet, suppCols, suppRows);
			// Console.WriteLine("{0} supports {1}", new FullCellAddr(sheet, col, row), range);
			// If RemoveCell removed something (giving true), it also added remaining ranges
			if (!range.RemoveCell(this, sheet, col, row)) {
				ranges.Add(range);
			}
		}

		public void RemoveCell(Sheet sheet, int col, int row) {
			int i = 0, count = ranges.Count; // Process only original supportSet items
			while (i < count) {
				if (ranges[i].RemoveCell(this, sheet, col, row)) {
					ranges.RemoveAt(i);
					count--;
				}
				else {
					i++;
				}
			}
		}

		public void Add(SupportRange range) { ranges.Add(range); }

		public void ForEachSupported(Action<Sheet, int, int> act) {
			foreach (SupportRange range in ranges) {
				range.ForEachSupported(act);
			}
		}
	}
}