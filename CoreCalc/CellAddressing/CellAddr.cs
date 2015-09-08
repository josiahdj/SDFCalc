// Corecalc, a spreadsheet core implementation
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

// Classes for representing absolute cell addresses,
// relative/absolute cell references, support ranges, and 
// adjusted expressions and refererences

namespace CoreCalc.CellAddressing {
	/// <summary>
	/// A CellAddr is an absolute, zero-based (col, row) location in some sheet.
	/// Serializable because it is used in ClipboardCell, which must be serializable.
	/// </summary>
	[Serializable]
	public struct CellAddr : IEquatable<CellAddr> {
		public readonly int col, row;
		public static readonly CellAddr A1 = new CellAddr(0, 0);

		public CellAddr(int col, int row) {
			this.col = col;
			this.row = row;
		}

		public CellAddr(RARef cr, int col, int row) {
			this.col = cr.colAbs ? cr.colRef : cr.colRef + col;
			this.row = cr.rowAbs ? cr.rowRef : cr.rowRef + row;
		}

		// Turn an A1-format string into an absolute cell address
		public CellAddr(String a1Ref) : this(new RARef(a1Ref, 0, 0), 0, 0) { }

		// Return ulCa as upper left and lrCa as lower right of (ca1, ca2)
		public static void NormalizeArea(CellAddr ca1,
										 CellAddr ca2,
										 out CellAddr ulCa,
										 out CellAddr lrCa) {
			int minCol = ca1.col,
				minRow = ca1.row,
				maxCol = ca2.col,
				maxRow = ca2.row;
			if (ca1.col > ca2.col) {
				minCol = ca2.col;
				maxCol = ca1.col;
			}
			if (ca1.row > ca2.row) {
				minRow = ca2.row;
				maxRow = ca1.row;
			}
			ulCa = new CellAddr(minCol, minRow);
			lrCa = new CellAddr(maxCol, maxRow);
		}

		public CellAddr(System.Drawing.Point p) {
			this.col = p.X;
			this.row = p.Y;
		}

		public CellAddr Offset(CellAddr offset) { return new CellAddr(this.col + offset.col, this.row + offset.row); }

		public bool Equals(CellAddr that) { return col == that.col && row == that.row; }

		public override int GetHashCode() { return 29*col + row; }

		public override bool Equals(Object o) { return o is CellAddr && Equals((CellAddr)o); }

		public static bool operator ==(CellAddr ca1, CellAddr ca2) { return ca1.col == ca2.col && ca1.row == ca2.col; }

		public static bool operator !=(CellAddr ca1, CellAddr ca2) { return ca1.col != ca2.col || ca1.row != ca2.col; }

		public override String ToString() { // A1 format only
			return ColumnName(col) + (row + 1);
		}

		public static String ColumnName(int col) {
			String name = "";
			while (col >= 26) {
				name = (char)('A' + col%26) + name;
				col = col/26 - 1;
			}
			return (char)('A' + col) + name;
		}
	}
}