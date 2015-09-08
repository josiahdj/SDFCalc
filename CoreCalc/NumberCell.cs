using System;
using System.Diagnostics;

namespace Corecalc {
	/// <summary>
	/// A NumberCell is a cell containing a floating-point constant.
	/// </summary>
	internal sealed class NumberCell : ConstCell {
		public readonly NumberValue value; // Non-null

		public NumberCell(double d) {
			Debug.Assert(!Double.IsNaN(d) && !Double.IsInfinity(d));
			value = (NumberValue)NumberValue.Make(d);
		}

		private NumberCell(NumberCell cell) { value = cell.value; }

		public override Value Eval(Sheet sheet, int col, int row) { return value; }

		public override String Show(int col, int row, Formats fo) { return value.value.ToString(); }

		public override Cell CloneCell(int col, int row) { return new NumberCell(this); }
	}
}