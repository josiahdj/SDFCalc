using System;
using System.Reflection;

using Corecalc;

using CoreCalc.Cells;
using CoreCalc.Values;

namespace CoreCalc.CellAddressing {
	/// <summary>
	/// A FullCellAddr is an absolute cell address, including a non-null 
	/// reference to a Sheet.
	/// </summary>
	public struct FullCellAddr : IEquatable<FullCellAddr> {
		public readonly Sheet sheet; // non-null
		public readonly CellAddr ca;
		public static readonly Type type = typeof (FullCellAddr);
		public static readonly MethodInfo evalMethod = type.GetMethod("Eval");

		public FullCellAddr(Sheet sheet, CellAddr ca) {
			this.ca = ca;
			this.sheet = sheet;
		}

		public FullCellAddr(Sheet sheet, int col, int row)
			: this(sheet, new CellAddr(col, row)) {}

		public FullCellAddr(Sheet sheet, string A1Format)
			: this(sheet, new CellAddr(A1Format)) {}

		public bool Equals(FullCellAddr other) { return ca.Equals(other.ca) && sheet == other.sheet; }

		public override int GetHashCode() { return ca.GetHashCode()*29 + sheet.GetHashCode(); }

		public override bool Equals(Object o) { return o is FullCellAddr && Equals((FullCellAddr)o); }

		public static bool operator ==(FullCellAddr fca1, FullCellAddr fca2) { return fca1.ca == fca2.ca && fca1.sheet == fca2.sheet; }

		public static bool operator !=(FullCellAddr fca1, FullCellAddr fca2) { return fca1.ca != fca2.ca || fca1.sheet != fca2.sheet; }

		public override string ToString() { return sheet.Name + "!" + ca; }

		public Value Eval() {
			Cell cell = sheet[ca];
			if (cell != null) {
				return cell.Eval(sheet, ca.col, ca.row);
			}
			else {
				return null;
			}
		}

		public bool TryGetCell(out Cell currentCell) {
			currentCell = sheet[ca];
			return currentCell != null;
		}
	}
}