using System;
using System.Diagnostics;

namespace Corecalc {
	/// <summary>
	/// A TextCell is a cell containing a double-quoted string constant.
	/// </summary>
	internal sealed class TextCell : ConstCell {
		public readonly TextValue value; // Non-null

		public TextCell(String s) {
			Debug.Assert(s != null);
			value = TextValue.Make(s); // No interning
		}

		private TextCell(TextCell cell) { this.value = cell.value; }

		public override Value Eval(Sheet sheet, int col, int row) { return value; }

		public override String Show(int col, int row, Formats fo) { return "\"" + value.value + "\""; }

		public override Cell CloneCell(int col, int row) { return new TextCell(this); }
	}
}