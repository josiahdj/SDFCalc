using System;

namespace Corecalc {
	/// <summary>
	/// A ValueConst is an arbitrary constant valued expression, used only
	/// for partial evaluation; there is no corresponding formula source syntax.
	/// </summary>
	internal class ValueConst : Const {
		public readonly Value value;

		public ValueConst(Value value) { this.value = value; }

		public override Value Eval(Sheet sheet, int col, int row) { return value; }

		internal override void VisitorCall(IExpressionVisitor visitor) { visitor.CallVisitor(this); }

		public override String Show(int col, int row, int ctxpre, Formats fo) { return "ValueConst[" + value + "]"; }
	}
}