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

using Corecalc.IO; // MemoryStream, Stream

namespace Corecalc {
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
}