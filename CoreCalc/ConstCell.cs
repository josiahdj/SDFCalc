using System;
using System.Collections.Generic;

namespace Corecalc {
	/// <summary>
	/// A ConstCell is a cell that contains a constant only.  Its value is 
	/// immutable, yet it cannot in general be shared between sheet positions
	/// because these may have different supported sets and other metadata.
	/// </summary>
	internal abstract class ConstCell : Cell {
		public override Cell MoveContents(int deltaCol, int deltaRow) { return this; }

		public override void InsertRowCols(Dictionary<Expr, Adjusted<Expr>> adjusted,
										   Sheet modSheet,
										   bool thisSheet,
										   int R,
										   int N,
										   int r,
										   bool doRows) {}

		public override void AddToSupportSets(Sheet supported, int col, int row, int cols, int rows) { }

		public override void RemoveFromSupportSets(Sheet sheet, int col, int row) { }

		public override void ForEachReferred(Sheet sheet, int col, int row, Action<FullCellAddr> act) { }

		public override void MarkDirty() { ForEachSupported(MarkCellDirty); }

		// A (newly edited) constant cell should not be enqueued, but its support set should
		public override void EnqueueForEvaluation(Sheet sheet, int col, int row) { ForEachSupported(EnqueueCellForEvaluation); }

		public override void ResetCellState() { }

		public override bool IsVolatile => false;

		public override void DependsOn(FullCellAddr here, Action<FullCellAddr> dependsOn) { }
	}
}