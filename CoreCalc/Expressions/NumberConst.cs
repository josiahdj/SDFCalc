using System;
using System.Diagnostics;

using Corecalc;

using CoreCalc.Types;
using CoreCalc.Values;

namespace CoreCalc.Expressions {
	/// <summary>
	/// A NumberConst is a constant number-valued expression.
	/// </summary>
	internal class NumberConst : Const {
		public readonly NumberValue value;

		public NumberConst(double d) {
			Debug.Assert(!Double.IsNaN(d) && !Double.IsInfinity(d));
			value = (NumberValue)NumberValue.Make(d);
		}

		public override Value Eval(Sheet sheet, int col, int row) { return value; }

		internal override void VisitorCall(IExpressionVisitor visitor) { visitor.CallVisitor(this); }

		public override String Show(int col, int row, int ctxpre, Formats fo) { return value.ToString(); }
	}
}