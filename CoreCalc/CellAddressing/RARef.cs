using System;

using Corecalc;

using CoreCalc.Types;

namespace CoreCalc.CellAddressing {
	/// <summary>
	/// A RARef is a relative or absolute cell reference (sheet unspecified).
	/// </summary>
	public sealed class RARef : IEquatable<RARef> {
		public readonly bool colAbs, rowAbs; // True=absolute, False=relative
		public readonly int colRef, rowRef;

		public RARef(bool colAbs, int colRef, bool rowAbs, int rowRef) {
			this.colAbs = colAbs;
			this.colRef = colRef;
			this.rowAbs = rowAbs;
			this.rowRef = rowRef;
		}

		// Parse "$A$3" to (true,0,true,2) and so on; relative references
		// must be adjusted to the (col,row) in which the reference occurs.

		public RARef(String a1Ref, int col, int row) {
			/* RC denotes "this" cell in MS XMLSS format; needed for area support */
			if (a1Ref.Equals("rc", StringComparison.CurrentCultureIgnoreCase)) {
				colAbs = false;
				rowAbs = false;
				colRef = 0;
				rowRef = 0;
			}
			else {
				int i = 0;
				if (i < a1Ref.Length && a1Ref[i] == '$') {
					colAbs = true;
					i++;
				}
				int val = -1;
				while (i < a1Ref.Length && IsAToZ(a1Ref[i])) {
					val = (val + 1)*26 + AToZValue(a1Ref[i]);
					i++;
				}
				colRef = colAbs ? val : val - col;
				if (i < a1Ref.Length && a1Ref[i] == '$') {
					rowAbs = true;
					i++;
				}
				val = ParseInt(a1Ref, ref i);
				rowRef = (rowAbs ? val : val - row) - 1;
			}
		}

		// Parse XMLSS R1C1 notation from well-formed string (checked by parser)

		public RARef(String r1c1) {
			int i = 0;
			rowAbs = true;
			colAbs = true;
			if (i < r1c1.Length && r1c1[i] == 'R') {
				i++;
			}
			if (i < r1c1.Length && r1c1[i] == '[') {
				rowAbs = false;
				i++;
			}
			int val = ParseInt(r1c1, ref i);
			if (rowAbs && val == 0) {
				rowAbs = false;
			}
			this.rowRef = rowAbs ? val - 1 : val;
			if (i < r1c1.Length && r1c1[i] == ']') {
				i++;
			}
			if (i < r1c1.Length && r1c1[i] == 'C') {
				i++;
			}
			if (i < r1c1.Length && r1c1[i] == '[') {
				colAbs = false;
				i++;
			}
			val = ParseInt(r1c1, ref i);
			if (i < r1c1.Length && r1c1[i] == ']') {
				i++;
			}
			if (colAbs && val == 0) {
				colAbs = false;
			}
			this.colRef = colAbs ? val - 1 : val;
		}

		// Parse possibly signed decimal integer
		private static int ParseInt(String s, ref int i) {
			int val = 0;
			bool negative = false;
			if (i < s.Length && (s[i] == '-' || s[i] == '+')) {
				negative = s[i] == '-';
				i++;
			}
			val = 0;
			while (i < s.Length && Char.IsDigit(s[i])) {
				val = val*10 + (s[i] - '0');
				i++;
			}
			return negative ? -val : val;
		}

		private static bool IsAToZ(char c) { return 'a' <= c && c <= 'z' || 'A' <= c && c <= 'Z'; }

		private static int AToZValue(char c) { return (c - 'A')%32; }

		// Absolute address of ref 
		public CellAddr Addr(int col, int row) { return new CellAddr(this, col, row); }

		// Insert N new rowcols before rowcol R>=0, when we're at rowcol r
		public Adjusted<RARef> InsertRowCols(int R, int N, int r, bool insertRow) {
			int newRef;
			int upper;
			if (insertRow) { // Insert N rows before row R; we're at row r
				InsertRowCols(R, N, r, rowAbs, rowRef, out newRef, out upper);
				RARef rarefNew = new RARef(colAbs, colRef, rowAbs, newRef);
				return new Adjusted<RARef>(rarefNew, upper, rowRef == newRef);
			}
			else { // Insert N columns before column R; we're at column r
				InsertRowCols(R, N, r, colAbs, colRef, out newRef, out upper);
				RARef rarefNew = new RARef(colAbs, newRef, rowAbs, rowRef);
				return new Adjusted<RARef>(rarefNew, upper, colRef == newRef);
			}
		}

		// Insert N new rowcols before rowcol R>=0, when we're at rowcol r
		private static void InsertRowCols(int R,
										  int N,
										  int r,
										  bool rcAbs,
										  int rcRef,
										  out int newRc,
										  out int upper) {
			if (rcAbs) {
				if (rcRef >= R) {
					// Absolute ref to cell after inserted          // Case (Ab)
					newRc = rcRef + N;
					upper = int.MaxValue;
				}
				else {
					// Absolute ref to cell before inserted       // Case (Aa) 
					newRc = rcRef;
					upper = int.MaxValue;
				}
			}
			else // Relative reference
				if (r >= R) {
					if (r + rcRef < R) {
						// Relative ref from after inserted rowcols to cell before them
						newRc = rcRef - N; // Case (Rab)
						upper = R - rcRef;
					}
					else {
						// Relative ref from after inserted rowcols to cell after them
						newRc = rcRef; // Case (Rbb)
						upper = int.MaxValue;
					}
				}
				else // r < R
					if (r + rcRef >= R) {
						// Relative ref from before inserted rowcols to cell after them
						newRc = rcRef + N; // Case (Rba)
						upper = R;
					}
					else {
						// Relative ref from before inserted rowcols to cell before them
						newRc = rcRef; // Case (Raa)
						upper = Math.Min(R, R - rcRef);
					}
		}

		// Clone and move (when the containing formula is moved, not copied)
		public RARef Move(int deltaCol, int deltaRow) {
			return new RARef(colAbs,
							 colAbs ? colRef : colRef + deltaCol,
							 rowAbs,
							 rowAbs ? rowRef : rowRef + deltaRow);
		}

		// Does this raref at (col, row) refer inside the sheet?
		public bool ValidAt(int col, int row) {
			CellAddr ca = new CellAddr(this, col, row);
			return 0 <= ca.col && 0 <= ca.row;
		}

		public String Show(int col, int row, Formats fo) {
			switch (fo.RefFmt) {
				case Formats.RefType.A1:
					CellAddr ca = new CellAddr(this, col, row);
					return (colAbs ? "$" : "") + CellAddr.ColumnName(ca.col)
						   + (rowAbs ? "$" : "") + (ca.row + 1);
				case Formats.RefType.R1C1:
					return "R" + RelAbsFormat(rowAbs, rowRef, 1)
						   + "C" + RelAbsFormat(colAbs, colRef, 1);
				case Formats.RefType.C0R0:
					return "C" + RelAbsFormat(colAbs, colRef, 0)
						   + "R" + RelAbsFormat(rowAbs, rowRef, 0);
				default:
					throw new ImpossibleException("Unknown reference format");
			}
		}

		private static String RelAbsFormat(bool abs, int offset, int origo) {
			if (abs) {
				return (offset + origo).ToString();
			}
			else if (offset == 0) {
				return "";
			}
			else {
				return "[" + (offset > 0 ? "+" : "") + offset.ToString() + "]";
			}
		}

		public bool Equals(RARef that) {
			return that != null && this.colAbs == that.colAbs && this.rowAbs == that.rowAbs
				   && this.colRef == that.colRef && this.rowRef == that.rowRef;
		}

		public override int GetHashCode() { return (((colAbs ? 1 : 0) + (rowAbs ? 2 : 0)) + colRef*4)*37 + rowRef; }
	}
}