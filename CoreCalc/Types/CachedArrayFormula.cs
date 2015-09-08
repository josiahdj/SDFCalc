using System;

using Corecalc;

using CoreCalc.CellAddressing;
using CoreCalc.Cells;
using CoreCalc.Values;

namespace CoreCalc.Types {
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