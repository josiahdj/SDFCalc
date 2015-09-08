using System;

using Corecalc;

using CoreCalc.Types;
using CoreCalc.Values;

namespace CoreCalc.Cells {
	/// <summary>
	/// A BlankCell is a blank cell, used only to record a blank cell's support set.
	/// </summary>
	internal sealed class BlankCell : ConstCell {
		public override Value Eval(Sheet sheet, int col, int row) { return null; }

		public override String Show(int col, int row, Formats fo) { return ""; }

		public override Cell CloneCell(int col, int row) { return new BlankCell(); }
	}
}