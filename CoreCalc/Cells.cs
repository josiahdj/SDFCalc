// Funcalc, a spreadsheet core implementation   
// ----------------------------------------------------------------------
// Copyright (c) 2006-2014 Peter Sestoft

// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use, copy,
// modify, merge, publish, distribute, sublicense, and/or sell copies
// of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

//  * The above copyright notice and this permission notice shall be
//    included in all copies or substantial portions of the Software.

//  * The software is provided "as is", without warranty of any kind,
//    express or implied, including but not limited to the warranties of
//    merchantability, fitness for a particular purpose and
//    noninfringement.  In no event shall the authors or copyright
//    holders be liable for any claim, damages or other liability,
//    whether in an action of contract, tort or otherwise, arising from,
//    out of or in connection with the software or the use or other
//    dealings in the software.
// ----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Corecalc.IO; // MemoryStream, Stream

namespace Corecalc {
	/// <summary>
	/// The recalculation state of a Formula cell.
	/// </summary>
	public enum CellState {
		Dirty,
		Enqueued,
		Computing,
		Uptodate
	}

	/// <summary>
	/// A Cell is what populates one position of a Sheet, if anything.
	/// Some cells (ConstCells) are immutable and so it seems the same 
	/// cell could appear multiple places in a workbook, but since cells 
	/// have metadata, such as the support set, which varies from position to position, that doesn't work.
	/// </summary>
	public abstract class Cell : IDepend {
		// The CellRanges in the support set may overlap; null means empty set
		private SupportSet supportSet;

		public abstract Value Eval(Sheet sheet, int col, int row);

		public abstract Cell MoveContents(int deltaCol, int deltaRow);

		public abstract void InsertRowCols(Dictionary<Expr, Adjusted<Expr>> adjusted,
										   Sheet modSheet,
										   bool thisSheet,
										   int R,
										   int N,
										   int r,
										   bool doRows);

		public abstract void ResetCellState();

		// Mark the cell dirty, for subsequent evaluation
		public abstract void MarkDirty();

		// Mark cell, dirty if non-empty
		public static void MarkCellDirty(Sheet sheet, int col, int row) {
			// Console.WriteLine("MarkDirty({0})", new FullCellAddr(sheet, col, row));
			Cell cell = sheet[col, row];
			cell?.MarkDirty();
		}

		// Enqueue this cell for evaluation
		public abstract void EnqueueForEvaluation(Sheet sheet, int col, int row);

		// Enqueue the cell at sheet[col, row] for evaluation, if non-null
		public static void EnqueueCellForEvaluation(Sheet sheet, int col, int row) {
			Cell cell = sheet[col, row];
			cell?.EnqueueForEvaluation(sheet, col, row); // Add if not already added, etc
		}

		// Show computed value; overridden in Formula and ArrayFormula to show cached value
		public virtual String ShowValue(Sheet sheet, int col, int row) {
			Value v = Eval(sheet, col, row);
			return v != null ? v.ToString() : "";
		}

		// Show constant or formula or array formula
		public abstract String Show(int col, int row, Formats fo);

		// Parse string to cell contents at (col, row) in given workbook 
		public static Cell Parse(String text, Workbook workbook, int col, int row) {
			if (!String.IsNullOrWhiteSpace(text)) {
				Scanner scanner = new Scanner(IOFormat.MakeStream(text));
				Parser parser = new Parser(scanner);
				return parser.ParseCell(workbook, col, row); // May be null
			}
			else {
				return null;
			}
		}

		// Add the support range to the cell, avoiding direct self-support at sheet[col,row]
		public void AddSupport(Sheet sheet,
							   int col,
							   int row,
							   Sheet suppSheet,
							   Interval suppCols,
							   Interval suppRows) {
			if (supportSet == null) {
				supportSet = new SupportSet();
			}
			supportSet.AddSupport(sheet, col, row, suppSheet, suppCols, suppRows);
		}

		// Remove sheet[col,row] from the support sets of cells that this cell refers to
		public abstract void RemoveFromSupportSets(Sheet sheet, int col, int row);

		// Remove sheet[col,row] from this cell's support set
		public void RemoveSupportFor(Sheet sheet, int col, int row) {
			supportSet?.RemoveCell(sheet, col, row);
		}

		// Overridden in ArrayFormula?
		public virtual void ForEachSupported(Action<Sheet, int, int> act) {
			supportSet?.ForEachSupported(act);
		}

		// Use at manual cell update, and only if the oldCell is never used again
		public void TransferSupportTo(ref Cell newCell) {
			if (supportSet != null) {
				newCell = newCell ?? new BlankCell();
				newCell.supportSet = supportSet;
			}
		}

		// Add to support sets of all cells referred to from this cell, when
		// the cell appears in the block supported[col..col+cols-1, row..row+rows-1] 
		public abstract void AddToSupportSets(Sheet supported, int col, int row, int cols, int rows);

		// Clear the cell's support set; in ArrayFormula also clear the supportSetUpdated flag
		public virtual void ResetSupportSet() { supportSet = null; }

		public abstract void ForEachReferred(Sheet sheet, int col, int row, Action<FullCellAddr> act);

		// True if the expression in the cell is volatile
		public abstract bool IsVolatile { get; }

		// Clone cell (supportSet, state fields) but not its sharable contents
		public abstract Cell CloneCell(int col, int row);

		public abstract void DependsOn(FullCellAddr here, Action<FullCellAddr> dependsOn);
	}

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

	/// <summary>
	/// A NumberCell is a cell containing a floating-point constant.
	/// </summary>
	internal sealed class NumberCell : ConstCell {
		public readonly NumberValue value; // Non-null

		public NumberCell(double d) {
			Debug.Assert(!Double.IsNaN(d) && !Double.IsInfinity(d));
			value = (NumberValue)NumberValue.Make(d);
		}

		private NumberCell(NumberCell cell) { value = cell.value; }

		public override Value Eval(Sheet sheet, int col, int row) { return value; }

		public override String Show(int col, int row, Formats fo) { return value.value.ToString(); }

		public override Cell CloneCell(int col, int row) { return new NumberCell(this); }
	}

	/// <summary>
	/// A QuoteCell is a cell containing a single-quoted string constant.
	/// </summary>
	internal sealed class QuoteCell : ConstCell {
		public readonly TextValue value; // Non-null

		public QuoteCell(String s) {
			Debug.Assert(s != null);
			value = TextValue.Make(s); // No interning
		}

		private QuoteCell(QuoteCell cell) { value = cell.value; }

		public override Value Eval(Sheet sheet, int col, int row) { return value; }

		public override String Show(int col, int row, Formats fo) { return "'" + value.value; }

		public override Cell CloneCell(int col, int row) { return new QuoteCell(this); }
	}

	/// <summary>
	/// A TextCell is a cell containing a double-quoted string constant.
	/// </summary>
	internal sealed class TextCell : ConstCell {
		public readonly TextValue value; // Non-null

		public TextCell(String s) {
			Debug.Assert(s != null);
			value = TextValue.Make(s); // No interning
		}

		private TextCell(TextCell cell) { this.value = cell.value; }

		public override Value Eval(Sheet sheet, int col, int row) { return value; }

		public override String Show(int col, int row, Formats fo) { return "\"" + value.value + "\""; }

		public override Cell CloneCell(int col, int row) { return new TextCell(this); }
	}

	/// <summary>
	/// A BlankCell is a blank cell, used only to record a blank cell's support set.
	/// </summary>
	internal sealed class BlankCell : ConstCell {
		public override Value Eval(Sheet sheet, int col, int row) { return null; }

		public override String Show(int col, int row, Formats fo) { return ""; }

		public override Cell CloneCell(int col, int row) { return new BlankCell(); }
	}

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

	/// <summary>
	/// A CachedArrayFormula is shared between multiple array cells.
	/// It contains a (caching) array-valued formula, the original cell
	/// address of that formula, and the upper left and lower right corners
	/// of the array.
	/// </summary>
	internal sealed class CachedArrayFormula {
		public readonly Formula formula; // Non-null
		public readonly Sheet _sheet; // Sheet containing the array formulas
		public readonly int formulaCol, formulaRow; // Location of formula entry
		public readonly CellAddr ulCa, lrCa; // Corners of array formula
		private bool supportAdded, supportRemoved; // Referred cells' support sets up to date

		// Invariant: Every cell within the display area sheet[ulCa, lrCa] is an 
		// ArrayFormula whose CachedArrayFormula instance is this one.

		public CachedArrayFormula(Formula formula,
								  Sheet sheet,
								  int formulaCol,
								  int formulaRow,
								  CellAddr ulCa,
								  CellAddr lrCa) {
			if (formula == null) {
				throw new Exception("CachedArrayFormula arguments");
			}
			else {
				this.formula = formula;
				this._sheet = sheet;
				this.formulaCol = formulaCol;
				this.formulaRow = formulaRow;
				this.ulCa = ulCa;
				this.lrCa = lrCa;
				this.supportAdded = this.supportRemoved = false;
			}
		}

		// Evaluate expression if necessary
		public Value Eval() { return formula.Eval(_sheet, formulaCol, formulaRow); }

		public CachedArrayFormula MoveContents(int deltaCol, int deltaRow) {
			// FIXME: Unshares the formula, shouldn't ...
			return new CachedArrayFormula((Formula)formula.MoveContents(deltaCol, deltaRow),
										  _sheet,
										  formulaCol,
										  formulaRow,
										  ulCa,
										  lrCa);
		}

		public Value CachedArray => formula.Cached;

		public void ResetSupportSet() {
			// Do NOT clear the underlying formula's support set; it will not be recreated
			supportAdded = false;
		}

		public void UpdateSupport(Sheet supported) {
			// Update the support sets of cells referred from an array formula only once
			if (!supportAdded) {
				formula.AddToSupportSets(supported, formulaCol, formulaRow, 1, 1);
				supportAdded = true;
			}
		}

		public void RemoveFromSupportSets(Sheet sheet, int col, int row) {
			// Update the support sets of cells referred from an array formula only once
			if (!supportRemoved) {
				formula.RemoveFromSupportSets(sheet, col, row);
				supportRemoved = true;
			}
		}

		public void ForEachReferred(Action<FullCellAddr> act) { formula.ForEachReferred(_sheet, formulaCol, formulaRow, act); }

		public String Show(int col, int row, Formats fo) { return formula.Show(col, row, fo); }
	}
}