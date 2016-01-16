// Funcalc, a spreadsheet core implementation 
// ----------------------------------------------------------------------
// Copyright (c) 2006-2014 Peter Sestoft and others

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
using System.Diagnostics;

using SC = System.Collections;

using System.Collections.Generic;
using System.Linq;

using Corecalc.Funcalc;

using CoreCalc.CellAddressing;
using CoreCalc.Cells;
using CoreCalc.Types;

namespace Corecalc {
	/// <summary>
	/// A Workbook is a collection of distinct named Sheets.
	/// </summary>
	public sealed class Workbook : IEnumerable<Sheet> {
		public event Action<String[]> OnFunctionsAltered;

		private readonly List<Sheet> sheets // All non-null and distinct
			= new List<Sheet>();

		public readonly Formats format // Formula formatting options
			= new Formats();

		// For managing recalculation of the workbook
		public CyclicException Cyclic { get; private set; } // Non-null if workbook has cycle
		public uint RecalcCount { get; private set; } // Number of recalculations done
		public bool UseSupportSets { get; private set; }

		private readonly List<FullCellAddr> editedCells
			= new List<FullCellAddr>();

		private readonly HashSet<FullCellAddr> volatileCells
			= new HashSet<FullCellAddr>();

		private readonly Queue<FullCellAddr> awaitsEvaluation
			= new Queue<FullCellAddr>();

		public Workbook() {
			RecalcCount = 0;
			UseSupportSets = false;
			SdfManager.ResetTables();
		}

		public void AddSheet(Sheet sheet) { sheets.Add(sheet); }

		public void RecordCellChange(int col, int row, Sheet sheet) { editedCells.Add(new FullCellAddr(sheet, col, row)); }

		public Sheet this[String name] {
			get {
				var upperName = name.ToUpper();
				return sheets.FirstOrDefault(sheet => sheet.Name.ToUpper() == upperName);
			}
		}

		public Sheet this[int i] => sheets[i];

		// Recalculate from recalculation roots only, using their supported sets
		public long Recalculate() {
			// Now Cyclic != null or for all formulas f, f.state==Uptodate
			if (Cyclic != null || CheckForModifiedSdf()) {
				return RecalculateFullAfterSdfCheck();
			}
			else {
				return TimeRecalculation(delegate {
											 UseSupportSets = true;
											 // Requires for all formulas f, f.state==Uptodate
											 // Stage (1): Mark formulas reachable from roots, f.state=Dirty
											 SupportArea.IdempotentForeach = true;
											 foreach (FullCellAddr fca in volatileCells) {
												 Cell.MarkCellDirty(fca.sheet, fca.ca.col, fca.ca.row);
											 }
											 foreach (FullCellAddr fca in editedCells) {
												 Cell.MarkCellDirty(fca.sheet, fca.ca.col, fca.ca.row);
											 }
											 // Stage (2): Evaluate Dirty formulas (and Dirty cells they depend on)
											 awaitsEvaluation.Clear();
											 SupportArea.IdempotentForeach = true;
											 foreach (FullCellAddr fca in editedCells) {
												 Cell.EnqueueCellForEvaluation(fca.sheet, fca.ca.col, fca.ca.row);
											 }
											 foreach (FullCellAddr fca in volatileCells) {
												 Cell.EnqueueCellForEvaluation(fca.sheet, fca.ca.col, fca.ca.row);
											 }
											 while (awaitsEvaluation.Count > 0) {
												 awaitsEvaluation.Dequeue().Eval();
											 }
										 });
			}
		}

		public void AddToQueue(Sheet sheet, int col, int row) { awaitsEvaluation.Enqueue(new FullCellAddr(sheet, col, row)); }

		public long RecalculateFull() {
			CheckForModifiedSdf();
			return RecalculateFullAfterSdfCheck();
		}

		// Unconditionally recalculate all cells a la Ctrl+Alt+F9
		public long RecalculateFullAfterSdfCheck() {
			return TimeRecalculation(delegate {
										 UseSupportSets = false;
										 ResetCellState();
										 // For all formulas f, f.state==Dirty
										 foreach (Sheet sheet in sheets) {
											 sheet.RecalculateFull();
										 }
									 });
		}

		public long RecalculateFullRebuild() {
			return TimeRecalculation(delegate {
										 UseSupportSets = false;
										 RebuildSupportGraph(); // Leaves all cells Dirty
										 foreach (Sheet sheet in sheets) {
											 sheet.RecalculateFull();
										 }
									 });
		}

		// Timing, and handling of cyclic dependencies
		private long TimeRecalculation(Action act) {
			Cyclic = null;
			RecalcCount++;
			Stopwatch sw = new Stopwatch();
			sw.Start();
			try {
				act();
			}
			catch (Exception exn) {
				ResetCellState(); // Mark all cells Dirty
				var cyclicException = exn as CyclicException;
				if (cyclicException != null) {
					Cyclic = cyclicException;
				}
				else {
					Console.WriteLine("BAD: {0}", exn);
				}
			}
			sw.Stop();
			editedCells.Clear();
			return sw.ElapsedMilliseconds;
		}

		private bool CheckForModifiedSdf() {
			if (RecalcCount != 0) {
				String[] modifiedFunctions = SdfManager.CheckForModifications(editedCells);
				if (modifiedFunctions.Length != 0 && OnFunctionsAltered != null) {
					OnFunctionsAltered(modifiedFunctions);
					return true;
				}
			}
			return false;
		}

		private void ResetCellState() {
			foreach (Sheet sheet in sheets) {
				sheet.ResetCellState();
			}
		}

		public void RebuildSupportGraph() {
			Console.WriteLine("Rebuilding support graph");
			foreach (Sheet sheet in this) {
				foreach (Cell cell in sheet) {
					cell.ResetSupportSet();
				}
			}
			ResetCellState(); // Mark all cells Dirty ie. not Visited
			foreach (Sheet sheet in this) {
				sheet.AddToSupportSets();
			}
			// Leaves all cells Dirty
		}

		public void ResetVolatileSet() {
			volatileCells.Clear();
			foreach (Sheet sheet in this) {
				sheet.IncreaseVolatileSet();
			}
		}

		public void IncreaseVolatileSet(Cell cell, Sheet sheet, int col, int row) {
			if (cell != null && cell.IsVolatile) {
				volatileCells.Add(new FullCellAddr(sheet, col, row));
			}
		}

		public void DecreaseVolatileSet(Cell cell, Sheet sheet, int col, int row) {
			Formula f = cell as Formula;
			if (f != null) {
				volatileCells.Remove(new FullCellAddr(sheet, col, row));
			}
		}

		public int SheetCount => sheets.Count;

		IEnumerator<Sheet> IEnumerable<Sheet>.GetEnumerator() {
			return ((IEnumerable<Sheet>)sheets).GetEnumerator();
		}

		SC.IEnumerator SC.IEnumerable.GetEnumerator() {
			return sheets.GetEnumerator();
		}

		public void Clear() {
			sheets.Clear();
			editedCells.Clear();
			volatileCells.Clear();
			awaitsEvaluation.Clear();
		}
	}
}