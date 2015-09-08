using System;
using System.Diagnostics;

namespace Corecalc {
	/// <summary>
	/// A QuoteCell is a cell containing a single-quoted string constant.
	/// </summary>
	internal sealed class QuoteCell : ConstCell {
		public readonly TextValue value; // Non-null

		public QuoteCell(String s) {
			Debug.Assert(s != null);
			value = TextValue.Make(s); // No interning
		}

		private QuoteCell(QuoteCell cell) { value = cell.value; }

		public override Value Eval(Sheet sheet, int col, int row) { return value; }

		public override String Show(int col, int row, Formats fo) { return "'" + value.value; }

		public override Cell CloneCell(int col, int row) { return new QuoteCell(this); }
	}
}