using System;

namespace Corecalc {
	/// <summary>
	/// A Const expression is a constant, immutable and sharable.
	/// </summary>
	abstract class Const : Expr {
		public static Const Make(Value value) {
			if (value is NumberValue)
				return new NumberConst((value as NumberValue).value);
			else if (value is TextValue)
				return new TextConst((value as TextValue).value);
			else
				return new ValueConst(value);
		}

		public override Expr Move(int deltaCol, int deltaRow) {
			return this;
		}

		// Any expression can be copied with sharing
		public override Expr CopyTo(int col, int row) {
			return this;
		}

		public override Adjusted<Expr> InsertRowCols(Sheet modSheet, bool thisSheet,
													 int R, int N, int r, bool doRows) {
			return new Adjusted<Expr>(this);
		}

		internal override void VisitRefs(RefSet refSet, Action<CellRef> refAct, Action<CellArea> areaAct) 
		{ }

		public override void DependsOn(FullCellAddr here, Action<FullCellAddr> dependsOn) { }

		public override bool IsVolatile {
			get { return false; }
		}
	}
}