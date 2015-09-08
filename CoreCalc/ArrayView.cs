using System;

namespace Corecalc {
	/// <summary>
	/// An ArrayView is a rectangular view of a sheet, the
	/// result of evaluating a CellArea expression.  Accessing an 
	/// element of an ArrayView may cause a cell to be evaluated.
	/// </summary>
	public class ArrayView : ArrayValue, IEquatable<ArrayView> {
		public readonly CellAddr ulCa, lrCa; // ulCa to the left and above lrCa
		public readonly Sheet sheet; // non-null
		private readonly int cols, rows;

		private ArrayView(CellAddr ulCa, CellAddr lrCa, Sheet sheet) {
			this.sheet = sheet;
			this.ulCa = ulCa;
			this.lrCa = lrCa;
			this.cols = lrCa.col - ulCa.col + 1;
			this.rows = lrCa.row - ulCa.row + 1;
		}

		public static ArrayView Make(CellAddr ulCa, CellAddr lrCa, Sheet sheet) {
			CellAddr.NormalizeArea(ulCa, lrCa, out ulCa, out lrCa);
			return new ArrayView(ulCa, lrCa, sheet);
		}

		public override int Cols {
			get { return cols; }
		}

		public override int Rows {
			get { return rows; }
		}

		// Evaluate and get value at offset [col, row], 0-based
		public override Value this[int col, int row] {
			get {
				if (0 <= col && col < Cols && 0 <= row && row < Rows) {
					int c = ulCa.col + col, r = ulCa.row + row;
					Cell cell = sheet[c, r];
					if (cell != null) {
						return cell.Eval(sheet, c, r);
					}
					else {
						return null;
					}
				}
				else {
					return ErrorValue.naError;
				}
			}
		}

		public override Value View(CellAddr ulCa, CellAddr lrCa) { return ArrayView.Make(ulCa.Offset(this.ulCa), lrCa.Offset(this.ulCa), sheet); }

		public override Value Slice(CellAddr ulCa, CellAddr lrCa) { return new ArrayView(ulCa.Offset(this.ulCa), lrCa.Offset(this.ulCa), sheet); }

		public void Apply(Action<FullCellAddr> act) {
			int col0 = ulCa.col, row0 = ulCa.row;
			for (int c = 0; c < cols; c++) {
				for (int r = 0; r < rows; r++) {
					act(new FullCellAddr(sheet, col0 + c, row0 + r));
				}
			}
		}

		public override bool Equals(Value v) {
			return v is ArrayView && Equals(v as ArrayView)
				   || EqualElements(this, v as ArrayValue);
		}

		// Used at codegen time for numbering and caching CGNormalCellArea expressions 
		// in sheet-defined functions.  NB: Must not compare element values.
		public bool Equals(ArrayView other) { return sheet == other.sheet && ulCa.Equals(other.ulCa) && lrCa.Equals(other.lrCa); }

		public override bool Equals(Object o) { return o is ArrayView && Equals((ArrayView)o); }

		public override int GetHashCode() { return (ulCa.GetHashCode()*29 + lrCa.GetHashCode())*37 + sheet.GetHashCode(); }
	}
}