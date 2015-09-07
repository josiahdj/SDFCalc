using System;

namespace Corecalc {
	/// <summary>
	/// A TextConst is a constant string-valued expression.
	/// </summary>
	class TextConst : Const {
		public readonly TextValue value;

		public TextConst(String s) {
			value = TextValue.MakeInterned(s);
		}

		public override Value Eval(Sheet sheet, int col, int row) {
			return value;
		}

		internal override void VisitorCall(IExpressionVisitor visitor) {
			visitor.CallVisitor(this);
		}

		public override String Show(int col, int row, int ctxpre, Formats fo) {
			return "\"" + value + "\"";
		}
	}
}