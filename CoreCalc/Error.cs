using System;

namespace Corecalc {
	/// <summary>
	/// An Error expression represents a static error, e.g. invalid cell reference.
	/// </summary>
	class Error : Const {
		private readonly String error;
		public readonly ErrorValue value;
		public static readonly Error refError = new Error(ErrorValue.refError);

		public Error(String msg) : this(ErrorValue.Make(msg)) { }

		public Error(ErrorValue value) {
			this.value = value;
			this.error = this.value.ToString();
		}

		public override Value Eval(Sheet sheet, int col, int row) {
			return value;
		}

		public override String Show(int col, int row, int ctxpre, Formats fo) {
			return error;
		}

		internal override void VisitorCall(IExpressionVisitor visitor) {
			visitor.CallVisitor(this);
		}
	}
}