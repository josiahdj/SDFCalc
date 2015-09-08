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
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Reflection;

// Classes for representing absolute cell addresses,
// relative/absolute cell references, support ranges, and 
// adjusted expressions and refererences

namespace Corecalc {
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

	/// <summary>
	/// A SupportSet represents the set of cells supported by a given cell;
	/// ie. those cells that refer to it.
	/// </summary>
	public class SupportSet {
		private readonly List<SupportRange> ranges = new List<SupportRange>();

		// Add suppSheet[suppCols, suppRows] except sheet[col,row] to support set
		public void AddSupport(Sheet sheet,
							   int col,
							   int row,
							   Sheet suppSheet,
							   Interval suppCols,
							   Interval suppRows) {
			SupportRange range = SupportRange.Make(suppSheet, suppCols, suppRows);
			// Console.WriteLine("{0} supports {1}", new FullCellAddr(sheet, col, row), range);
			// If RemoveCell removed something (giving true), it also added remaining ranges
			if (!range.RemoveCell(this, sheet, col, row)) {
				ranges.Add(range);
			}
		}

		public void RemoveCell(Sheet sheet, int col, int row) {
			int i = 0, count = ranges.Count; // Process only original supportSet items
			while (i < count) {
				if (ranges[i].RemoveCell(this, sheet, col, row)) {
					ranges.RemoveAt(i);
					count--;
				}
				else {
					i++;
				}
			}
		}

		public void Add(SupportRange range) { ranges.Add(range); }

		public void ForEachSupported(Action<Sheet, int, int> act) {
			foreach (SupportRange range in ranges) {
				range.ForEachSupported(act);
			}
		}
	}

	/// <summary>
	/// A SupportRange is a single supported cell or a supported cell area on a sheet.
	/// </summary>
	public abstract class SupportRange {
		public static SupportRange Make(Sheet sheet, Interval colInt, Interval rowInt) {
			if (colInt.min == colInt.max && rowInt.min == rowInt.max) {
				return new SupportCell(sheet, colInt.min, rowInt.min);
			}
			else {
				return new SupportArea(sheet, colInt, rowInt);
			}
		}

		// Remove cell sheet[col,row] from given support set, possibly adding smaller
		// support ranges at end of supportSet; if so return true
		public abstract bool RemoveCell(SupportSet set, Sheet sheet, int col, int row);

		public abstract void ForEachSupported(Action<Sheet, int, int> act);

		public abstract bool Contains(Sheet sheet, int col, int row);

		public abstract int Count { get; }
	}

	/// <summary>
	/// A SupportCell is a single supported cell.
	/// </summary>
	public class SupportCell : SupportRange {
		public readonly Sheet sheet;
		public readonly int col, row;

		public SupportCell(Sheet sheet, int col, int row) {
			this.sheet = sheet;
			this.col = col;
			this.row = row;
		}

		public override bool RemoveCell(SupportSet set, Sheet sheet, int col, int row) { return Contains(sheet, col, row); }

		public override void ForEachSupported(Action<Sheet, int, int> act) { act(sheet, col, row); }

		public override bool Contains(Sheet sheet, int col, int row) { return this.sheet == sheet && this.col == col && this.row == row; }

		public override int Count {
			get { return 1; }
		}

		public override string ToString() { return new FullCellAddr(sheet, col, row).ToString(); }
	}

	/// <summary>
	/// A SupportArea is a supported  absolute cell area sheet[colInt, rowInt].
	/// </summary>
	public class SupportArea : SupportRange {
		private static readonly List<SupportArea> alreadyVisited
			= new List<SupportArea>();

		private static bool idempotentForeach;

		public readonly Interval colInt, rowInt;
		public readonly Sheet sheet;

		public SupportArea(Sheet sheet, Interval colInt, Interval rowInt) {
			this.sheet = sheet;
			this.colInt = colInt;
			this.rowInt = rowInt;
		}

		public override bool RemoveCell(SupportSet set, Sheet sheet, int col, int row) {
			if (Contains(sheet, col, row)) {
				// To exclude cell at sheet[col, row], split into up to 4 support ranges
				if (rowInt.min < row) // North, column above [col,row]
				{
					set.Add(Make(sheet, new Interval(col, col), new Interval(rowInt.min, row - 1)));
				}
				if (row < rowInt.max) // South, column below [col,row]
				{
					set.Add(Make(sheet, new Interval(col, col), new Interval(row + 1, rowInt.max)));
				}
				if (colInt.min < col) // West, block to the left of [col,row]
				{
					set.Add(Make(sheet, new Interval(colInt.min, col - 1), rowInt));
				}
				if (col < colInt.max) // East, block to the right of [col,row]
				{
					set.Add(Make(sheet, new Interval(col + 1, colInt.max), rowInt));
				}
				return true;
			}
			else {
				return false;
			}
		}

		public static bool IdempotentForeach {
			get { return idempotentForeach; }
			set {
				idempotentForeach = value;
				alreadyVisited.Clear();
			}
		}

		public override void ForEachSupported(Action<Sheet, int, int> act) {
			if (IdempotentForeach && this.Count > alreadyVisited.Count + 1) {
				for (int i = 0; i < alreadyVisited.Count; i++) {
					SupportArea old = alreadyVisited[i];
					if (this.Overlaps(old)) {
						SupportArea overlap = this.Overlap(old);
						if (overlap.Count == this.Count) { // contained in old
							return;
						}
						else if (overlap.Count == old.Count) { // contains old
							alreadyVisited[i] = this;
							ForEachExcept(overlap, act);
							return;
						}
						else if (this.colInt.Equals(old.colInt) && this.rowInt.Overlaps(old.rowInt)) {
							alreadyVisited[i] = new SupportArea(sheet, this.colInt, this.rowInt.Join(old.rowInt));
							ForEachExcept(overlap, act);
							return;
						}
						else if (this.rowInt.Equals(old.rowInt) && this.colInt.Overlaps(old.colInt)) {
							alreadyVisited[i] = new SupportArea(sheet, this.colInt.Join(old.colInt), this.rowInt);
							ForEachExcept(overlap, act);
							return;
						}
						else { // overlaps, but neither containment nor rectangular union
							alreadyVisited.Add(this);
							ForEachExcept(overlap, act);
							return;
						}
					}
				}
				// Large enough but no existing support set overlaps this one
				alreadyVisited.Add(this);
			}
			ForEachInArea(sheet, colInt, rowInt, act);
		}

		private void ForEachExcept(SupportArea overlap, Action<Sheet, int, int> act) {
			if (rowInt.min < overlap.rowInt.min) // North non-empty, columns above overlap
			{
				ForEachInArea(sheet, overlap.colInt, new Interval(rowInt.min, overlap.rowInt.min - 1), act);
			}
			if (overlap.rowInt.max < rowInt.max) // South non-empty, columns below overlap
			{
				ForEachInArea(sheet, overlap.colInt, new Interval(overlap.rowInt.max + 1, rowInt.max), act);
			}
			if (colInt.min < overlap.colInt.min) // West non-empty, rows left of overlap
			{
				ForEachInArea(sheet, new Interval(colInt.min, overlap.colInt.min - 1), rowInt, act);
			}
			if (overlap.colInt.max < colInt.max) // East non-empty, rows right of overlap
			{
				ForEachInArea(sheet, new Interval(overlap.colInt.max + 1, colInt.max), rowInt, act);
			}
		}

		private static void ForEachInArea(Sheet sheet,
										  Interval colInt,
										  Interval rowInt,
										  Action<Sheet, int, int> act) {
			for (int c = colInt.min; c <= colInt.max; c++) {
				for (int r = rowInt.min; r <= rowInt.max; r++) {
					act(sheet, c, r);
				}
			}
		}

		public override bool Contains(Sheet sheet, int col, int row) { return this.sheet == sheet && colInt.Contains(col) && rowInt.Contains(row); }

		public override int Count {
			get { return colInt.Length*rowInt.Length; }
		}

		public bool Overlaps(SupportArea that) {
			return this.sheet == that.sheet
				   && this.colInt.Overlaps(that.colInt) && this.rowInt.Overlaps(that.rowInt);
		}

		public SupportArea Overlap(SupportArea that) {
			Debug.Assert(this.Overlaps(that)); // In particular, on same sheet
			return new SupportArea(sheet,
								   this.colInt.Meet(that.colInt),
								   this.rowInt.Meet(that.rowInt));
		}

		public override string ToString() {
			CellAddr ulCa = new CellAddr(colInt.min, rowInt.min),
					 lrCa = new CellAddr(colInt.max, rowInt.max);
			return String.Format("{0}!{1}:{2}", sheet.Name, ulCa, lrCa);
		}
	}

	/// <summary>
	/// An Interval is a non-empty integer interval [min...max].
	/// </summary>
	public struct Interval : IEquatable<Interval> {
		public readonly int min, max; // Assume min<=max

		public Interval(int min, int max) {
			Debug.Assert(min <= max);
			this.min = min;
			this.max = max;
		}

		public void ForEach(Action<int> act) {
			for (int i = min; i <= max; i++) {
				act(i);
			}
		}

		public bool Contains(int i) { return min <= i && i <= max; }

		public int Length {
			get { return max - min + 1; }
		}

		public bool Overlaps(Interval that) {
			return this.min <= that.min && that.min <= this.max
				   || that.min <= this.min && this.min <= that.max;
		}

		// When the intervals overlap, this is their union:
		public Interval Join(Interval that) { return new Interval(Math.Min(this.min, that.min), Math.Max(this.max, that.max)); }

		// When the intervals overlap, this is their intersection:
		public Interval Meet(Interval that) { return new Interval(Math.Max(this.min, that.min), Math.Min(this.max, that.max)); }

		public bool Equals(Interval that) { return this.min == that.min && this.max == that.max; }
	}

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

	/// <summary>
	/// An Adjusted<T> represents an adjusted expression or 
	/// a relative/absolute ref, for use in method InsertRowCols.
	/// </summary>
	/// <typeparam name="T">The type of adjusted entity: Expr or RaRef.</typeparam>
	public struct Adjusted<T> {
		public readonly T e; // The adjusted Expr or RaRef
		public readonly int upper; // ... invalid for rows >= upper
		public readonly bool same; // Adjusted is identical to original

		public Adjusted(T e, int upper, bool same) {
			this.e = e;
			this.upper = upper;
			this.same = same;
		}

		public Adjusted(T e) : this(e, int.MaxValue, true) { }
	}
}