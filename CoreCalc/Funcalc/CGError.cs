using System;
using System.Reflection.Emit;

namespace Corecalc.Funcalc {
	/// <summary>
	/// A CGError is a constant error-valued expression, such as an
	/// illegal cell reference arising from deletion of rows or columns
	/// or the copying of relative references.
	/// </summary>
	public class CGError : CGConst {
		private readonly ErrorValue errorValue;

		public CGError(ErrorValue errorValue) { this.errorValue = errorValue; }

		public CGError(String message) : this(ErrorValue.Make(message)) { }

		// This is used to implement the ERR function
		public CGError(CGExpr[] es) {
			if (es.Length != 1) {
				errorValue = ErrorValue.argCountError;
			}
			else {
				CGTextConst messageConst = es[0] as CGTextConst;
				if (messageConst == null) {
					errorValue = ErrorValue.argTypeError;
				}
				else {
					errorValue = ErrorValue.Make("#ERR: " + messageConst.value.value);
				}
			}
		}

		public override void Compile() { LoadErrorValue(errorValue); }

		public override void CompileToDoubleOrNan() { ilg.Emit(OpCodes.Ldc_R8, errorValue.ErrorNan); }

		public override Value Value {
			get { return errorValue; }
		}

		public override string ToString() { return errorValue.message; }

		public override Typ Type() { return Typ.Error; }
	}
}