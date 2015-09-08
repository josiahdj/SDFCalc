using System;
using System.Reflection.Emit;

using CoreCalc.Values;

namespace Corecalc.Funcalc {
	/// <summary>
	/// A CGClosure is an application of the CLOSURE built-in function.
	/// </summary>
	public class CGClosure : CGStrictOperation {
		private static readonly Applier closureApplier = Function.Get("CLOSURE").Applier;

		public CGClosure(CGExpr[] es)
			: base(es, closureApplier) {}

		public override void Compile() {
			int argCount = es.Length - 1;
			if (es.Length < 1) {
				LoadErrorValue(ErrorValue.argCountError);
			}
			else if (es[0] is CGTextConst) {
				String name = (es[0] as CGTextConst).value.value;
				SdfInfo sdfInfo = SdfManager.GetInfo(name);
				if (sdfInfo == null) {
					LoadErrorValue(ErrorValue.nameError);
				}
				else if (argCount != 0 && argCount != sdfInfo.arity) {
					LoadErrorValue(ErrorValue.argCountError);
				}
				else {
					ilg.Emit(OpCodes.Ldc_I4, sdfInfo.index);
					CompileToValueArray(argCount, 1, es);
					ilg.Emit(OpCodes.Call, FunctionValue.makeMethod);
				}
			}
			else {
				es[0].Compile();
				CheckType(FunctionValue.type,
						  new Gen(delegate {
									  CompileToValueArray(argCount, 1, es);
									  ilg.Emit(OpCodes.Call, FunctionValue.furtherApplyMethod);
								  }),
						  GenLoadErrorValue(ErrorValue.argTypeError));
			}
		}

		public override CGExpr Residualize(CGExpr[] res) { return new CGClosure(res); }

		public override int Arity {
			get { return es.Length; }
		}

		public override Typ Type() { return Typ.Function; }

		protected override Typ GetInputTypWithoutLengthCheck(int pos) { return Typ.Value; }

		public override void CompileToDoubleOrNan() {
			// TODO: Is this right?
			LoadErrorNan(ErrorValue.argTypeError);
		}
	}
}