using System;

namespace Corecalc {
	/// <summary>
	/// A CellRef expression refers to a single cell, eg. 
	/// is A1 or $A1 or A$1 or $A$1 or Sheet1!A1.
	/// </summary>
	class CellRef : Expr, IEquatable<CellRef> {
		public readonly RARef raref;
		public readonly Sheet sheet;    // non-null if sheet-absolute

		public CellRef(Sheet sheet, RARef raref) {
			this.sheet = sheet;
			this.raref = raref;
		}

		// Evaluate cell ref by evaluating the cell referred to 

		public override Value Eval(Sheet sheet, int col, int row) {
			CellAddr ca = raref.Addr(col, row);
			Cell cell = (this.sheet ?? sheet)[ca];
			return cell == null ? null : cell.Eval(sheet, ca.col, ca.row);
		}

		public FullCellAddr GetAbsoluteAddr(Sheet sheet, int col, int row) {
			return new FullCellAddr(this.sheet ?? sheet, raref.Addr(col, row));
		}

		public FullCellAddr GetAbsoluteAddr(FullCellAddr fca) {
			return GetAbsoluteAddr(fca.sheet, fca.ca.col, fca.ca.row);
		}

		// Clone and move (when the containing formula is moved, not copied!)
		public override Expr Move(int deltaCol, int deltaRow) {
			return new CellRef(sheet, raref.Move(deltaCol, deltaRow));
		}

		// Can be copied with sharing iff reference is within sheet
		public override Expr CopyTo(int col, int row) {
			if (raref.ValidAt(col, row))
				return this;
			return
				Error.refError;
		}

		public override Adjusted<Expr> InsertRowCols(Sheet modSheet, bool thisSheet,
													 int R, int N, int r,
													 bool doRows) {
			if (sheet == modSheet || sheet == null && thisSheet) {
				Adjusted<RARef> adj = raref.InsertRowCols(R, N, r, doRows);
				return new Adjusted<Expr>(new CellRef(sheet, adj.e), adj.upper, adj.same);
			} else
				return new Adjusted<Expr>(this);
		}

		internal void AddToSupport(Sheet supported, int col, int row, int cols, int rows) 
		{
			Sheet referredSheet = this.sheet ?? supported;
			int ca = raref.colRef, ra = raref.rowRef;
			int r1 = row, r2 = row + rows - 1, c1 = col, c2 = col + cols - 1;
			Interval referredCols, referredRows;
			Func<int, Interval> supportedCols, supportedRows;
			RefAndSupp(raref.colAbs, ca, c1, c2, out referredCols, out supportedCols);
			RefAndSupp(raref.rowAbs, ra, r1, r2, out referredRows, out supportedRows);
			// Outer iteration is made over the shorter interval for efficiency
			if (referredCols.Length < referredRows.Length)
				referredCols.ForEach(c => {
										 Interval suppCols = supportedCols(c);
										 referredRows.ForEach(r =>
															  referredSheet.AddSupport(c, r, supported, suppCols, supportedRows(r)));
									 });
			else
				referredRows.ForEach(r => {
										 Interval suppRows = supportedRows(r);
										 referredCols.ForEach(c =>
															  referredSheet.AddSupport(c, r, supported, supportedCols(c), suppRows));
									 }); 
		}

		// This uses the notation from the book's analysis of the row aspect of support sets
		private static void RefAndSupp(bool abs, int ra, int r1, int r2,
									   out Interval referred, out Func<int, Interval> supported) {
			if (abs) { // case abs          
				referred = new Interval(ra, ra);
				supported = r => new Interval(r1, r2);
			} else {   // case rel
				referred = new Interval(r1 + ra, r2 + ra);
				supported = r => new Interval(r - ra, r - ra);
			}
		}

		internal override void VisitRefs(RefSet refSet, Action<CellRef> refAct, Action<CellArea> areaAct) 
		{
			if (!refSet.SeenBefore(this))
				refAct(this);
		}

		public override void DependsOn(FullCellAddr here, Action<FullCellAddr> dependsOn) {
			dependsOn(GetAbsoluteAddr(here));
		}

		public override bool IsVolatile {
			get { return false; }
		}

		public bool Equals(CellRef that) {
			return this.raref.Equals(that.raref);
		}

		public override int GetHashCode() {
			return raref.GetHashCode();
		}

		public override String Show(int col, int row, int ctxpre, Formats fo) {
			String s = raref.Show(col, row, fo);
			return sheet == null ? s : sheet.Name + "!" + s;
		}

		internal override void VisitorCall(IExpressionVisitor visitor) {
			visitor.CallVisitor(this);
		}
	}
}