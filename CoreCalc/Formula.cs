using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Corecalc {
	/// <summary>
	/// A Formula is a non-null caching expression contained in one cell.
	/// </summary>
	internal sealed class Formula : Cell {
		public readonly Workbook workbook; // Non-null
		private Expr e; // Non-null
		public CellState state; // Initially Dirty
		private Value v; // Up to date if state==Uptodate

		public Formula(Workbook workbook, Expr e) {
			Debug.Assert(workbook != null);
			Debug.Assert(e != null);
			this.workbook = workbook;
			this.e = e;
			this.state = CellState.Uptodate;
		}

		public static Formula Make(Workbook workbook, Expr e) {
			if (e == null) {
				return null;
			}
			else {
				return new Formula(workbook, e);
			}
		}

		// FIXME: Adequate for moving one cell, but block moves and row/column
		// inserts should avoid the duplication and unsharing of expressions. 
		public override Cell MoveContents(int deltaCol, int deltaRow) { return new Formula(workbook, e.Move(deltaCol, deltaRow)); }

		// Evaluate cell's expression if necessary and cache its value; 
		// also enqueue supported cells for evaluation if we use support graph
		public override Value Eval(Sheet sheet, int col, int row) {
			switch (state) {
				case CellState.Uptodate:
					break;
				case CellState.Computing:
					FullCellAddr culprit = new FullCellAddr(sheet, col, row);
					String msg = String.Format("### CYCLE in cell {0} formula {1}",
											   culprit,
											   Show(col, row, workbook.format));
					throw new CyclicException(msg, culprit);
				case CellState.Dirty:
				case CellState.Enqueued:
					state = CellState.Computing;
					v = e.Eval(sheet, col, row);
					state = CellState.Uptodate;
					if (workbook.UseSupportSets) {
						ForEachSupported(EnqueueCellForEvaluation);
					}
					break;
			}
			return v;
		}

		public override void InsertRowCols(Dictionary<Expr, Adjusted<Expr>> adjusted,
										   Sheet modSheet,
										   bool thisSheet,
										   int R,
										   int N,
										   int r,
										   bool doRows) {
			Adjusted<Expr> ae;
			if (adjusted.ContainsKey(e) && r < adjusted[e].upper) {
				// There is a valid cached adjusted expression
				ae = adjusted[e];
			}
			else {
				// Compute a new adjusted expression and insert into the cache
				ae = e.InsertRowCols(modSheet, thisSheet, R, N, r, doRows);
				Console.WriteLine("Making new adjusted at rowcol " + r
								  + "; upper = " + ae.upper);
				if (ae.same) { // For better sharing, reuse unadjusted e if same
					ae = new Adjusted<Expr>(e, ae.upper, ae.same);
					Console.WriteLine("Reusing expression");
				}
				adjusted[e] = ae;
			}
			Debug.Assert(r < ae.upper, "Formula.InsertRowCols");
			e = ae.e;
		}

		public Value Cached => v;

		public override void MarkDirty() {
			if (state != CellState.Dirty) {
				state = CellState.Dirty;
				ForEachSupported(MarkCellDirty);
			}
		}

		public override void EnqueueForEvaluation(Sheet sheet, int col, int row) {
			if (state == CellState.Dirty) { // Not Computing or Enqueued or Uptodate
				state = CellState.Enqueued;
				sheet.workbook.AddToQueue(sheet, col, row);
			}
		}

		public override void AddToSupportSets(Sheet supported, int col, int row, int cols, int rows) { e.AddToSupportSets(supported, col, row, cols, rows); }

		public override void RemoveFromSupportSets(Sheet sheet, int col, int row) { e.RemoveFromSupportSets(sheet, col, row); }

		// Reset recomputation flags, eg. after a circularity has been found
		public override void ResetCellState() { state = CellState.Dirty; }

		public override void ForEachReferred(Sheet sheet, int col, int row, Action<FullCellAddr> act) { e.ForEachReferred(sheet, col, row, act); }

		public override Cell CloneCell(int col, int row) { return new Formula(workbook, e.CopyTo(col, row)); }

		public override bool IsVolatile => e.IsVolatile;

		public override void DependsOn(FullCellAddr here, Action<FullCellAddr> dependsOn) { e.DependsOn(here, dependsOn); }

		public override String ShowValue(Sheet sheet, int col, int row) {
			// Use cached value, do not call Eval, as there might be a cycle!
			return v != null ? v.ToString() : "";
		}

		public override String Show(int col, int row, Formats fo) { return "=" + e.Show(col, row, 0, fo); }

		// Slight abuse of the cell state, used when detecting formula copies
		public bool Visited {
			get { return state == CellState.Uptodate; }
			set { state = value ? CellState.Uptodate : CellState.Dirty; }
		}

		public Expr Expr => e;
	}
}