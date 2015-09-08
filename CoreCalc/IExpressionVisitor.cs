using CoreCalc.Expressions;

namespace Corecalc {
	/// <summary>
	/// An IExpressionVisitor is used to traverse the Expr abstract syntax used in formulas.
	/// </summary>
	internal interface IExpressionVisitor {
		void CallVisitor(NumberConst numbConst);

		void CallVisitor(TextConst textConst);

		void CallVisitor(ValueConst valueConst);

		void CallVisitor(Error expr);

		void CallVisitor(FunCall funCall);

		void CallVisitor(CellRef cellRef);

		void CallVisitor(CellArea cellArea);
	}
}