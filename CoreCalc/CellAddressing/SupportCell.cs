using System;

using Corecalc;

namespace CoreCalc.CellAddressing {
	/// <summary>
	/// A SupportCell is a single supported cell.
	/// </summary>
	public class SupportCell : SupportRange {
		public readonly Sheet sheet;
		public readonly int col, row;

		public SupportCell(Sheet sheet, int col, int row) {
			this.sheet = sheet;
			this.col = col;
			this.row = row;
		}

		public override bool RemoveCell(SupportSet set, Sheet sheet, int col, int row) { return Contains(sheet, col, row); }

		public override void ForEachSupported(Action<Sheet, int, int> act) { act(sheet, col, row); }

		public override bool Contains(Sheet sheet, int col, int row) { return this.sheet == sheet && this.col == col && this.row == row; }

		public override int Count => 1;

		public override string ToString() { return new FullCellAddr(sheet, col, row).ToString(); }
	}
}