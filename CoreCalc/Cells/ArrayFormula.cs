using System;
using System.Collections.Generic;

using Corecalc;

using CoreCalc.CellAddressing;
using CoreCalc.Expressions;
using CoreCalc.Types;
using CoreCalc.Values;

using NotImplementedException = CoreCalc.Types.NotImplementedException;

namespace CoreCalc.Cells {
	/// <summary>
	/// An ArrayFormula is a cached array formula shared among several
	/// cells, each cell accessing one part of the result array.  Several
	/// ArrayFormula cells share one CachedArrayFormula cell; evaluation
	/// of one cell will evaluate the formula and cache its (array) value
	/// for the other cells to use.
	/// </summary>
	internal sealed class ArrayFormula : Cell {
		public readonly CachedArrayFormula caf; // Non-null
		private readonly CellAddr ca; // Cell's location within array value

		public ArrayFormula(CachedArrayFormula caf, CellAddr ca) {
			this.caf = caf;
			this.ca = ca;
		}

		public ArrayFormula(CachedArrayFormula caf, int col, int row)
			: this(caf, new CellAddr(col, row)) {}

		public override Value Eval(Sheet sheet, int col, int row) {
			Value v = caf.Eval();
			if (v is ArrayValue) {
				return (v as ArrayValue)[ca];
			}
			else if (v is ErrorValue) {
				return v;
			}
			else {
				return ErrorValue.Make("#ERR: Not array");
			}
		}

		public bool Contains(int col, int row) {
			return caf.ulCa.col <= col && col <= caf.lrCa.col
				   && caf.ulCa.row <= row && row <= caf.lrCa.row;
		}

		public override Cell MoveContents(int deltaCol, int deltaRow) {
			// FIXME: loses sharing of the CachedArrayFormula; but then again
			// an array formula should never be moved cell by cell, but in its
			// entirety, and in one go.
			return new ArrayFormula(caf.MoveContents(deltaCol, deltaRow), ca);
		}

		public override void InsertRowCols(Dictionary<Expr, Adjusted<Expr>> adjusted,
										   Sheet modSheet,
										   bool thisSheet,
										   int R,
										   int N,
										   int r,
										   bool doRows) {
			// FIXME: Implement, make sure to update underlying formula only once
			throw new NotImplementedException("Insertions that move array formulas");
		}

		public override String ShowValue(Sheet sheet, int col, int row) {
			// Use the underlying cached value, do not call Eval, there might be a cycle!
			Value v = caf.CachedArray;
			if (v is ArrayValue) {
				Value element = (v as ArrayValue)[ca];
				return element != null ? element.ToString() : "";
			}
			else if (v is ErrorValue) {
				return v.ToString();
			}
			else {
				return ErrorValue.Make("#ERR: Not array").ToString();
			}
		}

		public override void MarkDirty() {
			switch (caf.formula.state) {
				case CellState.Uptodate:
					caf.formula.MarkDirty();
					ForEachSupported(MarkCellDirty);
					// caf.formula will call MarkDirty on the array formula's display 
					// cells, except this one, so must mark its dependents explicitly. 
					break;
				case CellState.Dirty:
					ForEachSupported(MarkCellDirty);
					break;
			}
		}

		public override void EnqueueForEvaluation(Sheet sheet, int col, int row) {
			switch (caf.formula.state) {
				case CellState.Dirty:
					caf.Eval();
					ForEachSupported(EnqueueCellForEvaluation);
					// caf.formula will call EnqueueForEvaluation on the array formula's
					// display cells, except this one, so must enqueue its dependents.
					break;
				case CellState.Uptodate:
					ForEachSupported(EnqueueCellForEvaluation);
					break;
			}
		}

		public override void ResetCellState() { caf.formula.ResetCellState(); }

		public override void ResetSupportSet() {
			caf.ResetSupportSet();
			base.ResetSupportSet();
		}

		public override void AddToSupportSets(Sheet supported, int col, int row, int cols, int rows) { caf.UpdateSupport(supported); }

		public override void RemoveFromSupportSets(Sheet sheet, int col, int row) { caf.RemoveFromSupportSets(sheet, col, row); }

		public override void ForEachReferred(Sheet sheet, int col, int row, Action<FullCellAddr> act) { caf.ForEachReferred(act); }

		public override Cell CloneCell(int col, int row) {
			// Not clear how to copy an array formula.  It does make sense; see Excel.
			throw new System.NotImplementedException();
		}

		public override bool IsVolatile => caf.formula.IsVolatile;

		public override void DependsOn(FullCellAddr here, Action<FullCellAddr> dependsOn) {
			// It seems that this could uselessly be called on every cell 
			// that shares the formula, but this will not happen on function sheets
			caf.formula.DependsOn(here, dependsOn);
		}

		public override String Show(int col, int row, Formats fo) { return "{" + caf.Show(caf.formulaCol, caf.formulaRow, fo) + "}"; }
	}
}