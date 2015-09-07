using System;

namespace Corecalc {
	/// <summary>
	/// A CellArea expression refers to a rectangular cell area, eg. is
	/// A1:C4 or $A$1:C4 or A1:$C4 or Sheet1!A1:C4
	/// </summary>
	class CellArea : Expr, IEquatable<CellArea> {
		// It would be desirable to store the cell area in normalized form, 
		// with ul always to the left and above lr; but this cannot be done 
		// because it may be mixed relative/absolute as in $A1:$A$5, which 
		// when copied from B1 to B10 is no longer normalized.
		private readonly RARef ul, lr;  // upper left, lower right
		public readonly Sheet sheet;    // non-null if sheet-absolute

		public CellArea(Sheet sheet,
						bool ulColAbs, int ulColRef, bool ulRowAbs, int ulRowRef,
						bool lrColAbs, int lrColRef, bool lrRowAbs, int lrRowRef)
			: this(sheet,
				   new RARef(ulColAbs, ulColRef, ulRowAbs, ulRowRef),
				   new RARef(lrColAbs, lrColRef, lrRowAbs, lrRowRef)) {
		}

		public CellArea(Sheet sheet, RARef ul, RARef lr) {
			this.sheet = sheet;
			this.ul = ul;
			this.lr = lr;
		}

		// Evaluate cell area by returning an array view of it

		public override Value Eval(Sheet sheet, int col, int row) {
			return MakeArrayView(sheet, col, row);
		}

		public ArrayView MakeArrayView(FullCellAddr fca) {
			return MakeArrayView(fca.sheet, fca.ca.col, fca.ca.row);
		}

		public ArrayView MakeArrayView(Sheet sheet, int col, int row) {
			CellAddr ulCa = ul.Addr(col, row), lrCa = lr.Addr(col, row);
			ArrayView view = ArrayView.Make(ulCa, lrCa, this.sheet ?? sheet);
			// Forcing the evaluation of all cells in an array view value.
			// TODO: Doing this repeatedly, as in ManyDependents.xml, is costly
			for (int c = 0; c < view.Cols; c++)
				for (int r = 0; r < view.Rows; r++) {
					// Value ignore = view[c, r];
				}
			return view;
		}

		public void ApplyToFcas(Sheet sheet, int col, int row, Action<FullCellAddr> act) {
			CellAddr ulCa = ul.Addr(col, row), lrCa = lr.Addr(col, row);
			ArrayView.Make(ulCa, lrCa, this.sheet ?? sheet).Apply(act);
		}

		// Clone and move (when the containing formula is moved, not copied)
		public override Expr Move(int deltaCol, int deltaRow) {
			return new CellArea(sheet,
								ul.Move(deltaCol, deltaRow),
								lr.Move(deltaCol, deltaRow));
		}

		// Can copy cell area with sharing iff corners are within sheet
		public override Expr CopyTo(int col, int row) {
			if (ul.ValidAt(col, row) && lr.ValidAt(col, row))
				return this;
			else
				return Error.refError;
		}

		public override Adjusted<Expr> InsertRowCols(Sheet modSheet, bool thisSheet,
													 int R, int N, int r,
													 bool doRows) {
			if (sheet == modSheet || sheet == null && thisSheet) {
				Adjusted<RARef> ulNew = ul.InsertRowCols(R, N, r, doRows),
								lrNew = lr.InsertRowCols(R, N, r, doRows);
				int upper = Math.Min(ulNew.upper, lrNew.upper);
				return new Adjusted<Expr>(new CellArea(sheet, ulNew.e, lrNew.e),
										  upper, ulNew.same && lrNew.same);
			} else
				return new Adjusted<Expr>(this);
		}

		internal void AddToSupport(Sheet supported, int col, int row, int cols, int rows) {
			Sheet referredSheet = this.sheet ?? supported;
			Interval referredRows, referredCols;
			Func<int, Interval> supportedRows, supportedCols;
			int ra = ul.rowRef, rb = lr.rowRef, r1 = row, r2 = row + rows - 1;
			RefAndSupp(ul.rowAbs, lr.rowAbs, ra, rb, r1, r2, out referredRows, out supportedRows);
			int ca = ul.colRef, cb = lr.colRef, c1 = col, c2 = col + cols - 1;
			RefAndSupp(ul.colAbs, lr.colAbs, ca, cb, c1, c2, out referredCols, out supportedCols);
			// Outer iteration should be over the shorter interval for efficiency
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

		// This uses notation from the book's discussion of the row dimension of support sets,
		// works equally well for the column dimension.
		// Assumes r1 <= r2 but nothing about the order of ra and rb
		private static void RefAndSupp(bool ulAbs, bool lrAbs, int ra, int rb, int r1, int r2,
									   out Interval referred, out Func<int, Interval> supported) {
			if (ulAbs) {
				if (lrAbs) { // case abs-abs
					SortInts(ref ra, ref rb);
					referred = new Interval(ra, rb);
					supported = r => new Interval(r1, r2);
				} else {     // case abs-rel
					referred = new Interval(Math.Min(ra, r1 + rb), Math.Max(ra, r2 + rb));
					supported = r => ra < r ? new Interval(Math.Max(r1, r - rb), r2)
										 : ra > r ? new Interval(r1, Math.Min(r2, r - rb))
											   : new Interval(r1, r2);
				}
			} else {
				if (lrAbs) { // case rel-abs
					referred = new Interval(Math.Min(r1 + ra, rb), Math.Max(r2 + ra, rb));
					supported = r => rb > r ? new Interval(r1, Math.Min(r2, r - ra))
										 : rb < r ? new Interval(Math.Max(r1, r - ra), r2)
											   : new Interval(r1, r2);
				} else {     // case rel-rel
					SortInts(ref ra, ref rb);
					referred = new Interval(r1 + ra, r2 + rb);
					supported = r => new Interval(Math.Max(r1, r - rb), Math.Min(r2, r - ra));
				}
			}
		}

		private static void SortInts(ref int a, ref int b) {
			if (a > b) {
				int tmp = a; a = b; b = tmp;
			}
		}

		internal override void VisitRefs(RefSet refSet, Action<CellRef> refAct, Action<CellArea> areaAct) 
		{
			if (!refSet.SeenBefore(this))
				areaAct(this);
		}

		public override void DependsOn(FullCellAddr here, Action<FullCellAddr> dependsOn) {
			ApplyToFcas(here.sheet, here.ca.col, here.ca.row, dependsOn);
		}

		public override bool IsVolatile {
			get { return false; }
		}

		public bool Equals(CellArea that) {
			return that != null && this.ul.Equals(that.ul) && this.lr.Equals(that.lr);
		}

		public override int GetHashCode() {
			return lr.GetHashCode() * 511 + ul.GetHashCode();
		} 

		public override String Show(int col, int row, int ctxpre, Formats fo) {
			String s = ul.Show(col, row, fo) + ":" + lr.Show(col, row, fo);
			return sheet == null ? s : sheet.Name + "!" + s;
		}

		internal override void VisitorCall(IExpressionVisitor visitor) {
			visitor.CallVisitor(this);
		}
	}
}