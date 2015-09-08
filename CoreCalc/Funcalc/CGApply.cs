using System.Reflection.Emit;

namespace Corecalc.Funcalc {
	/// <summary>
	/// A CGApply is a call to the APPLY function; its first argument must 
	/// evaluate to a function value (closure).
	/// </summary>
	internal class CGApply : CGStrictOperation {
		public CGApply(CGExpr[] es) : base(es, null) { }

		public override void Compile() {
			if (es.Length < 1) {
				LoadErrorValue(ErrorValue.argCountError);
			}
			else {
				es[0].Compile();
				CheckType(FunctionValue.type,
						  new Gen(delegate {
									  int arity = es.Length - 1;
									  // Don't check arity here; it is done elsewhere
									  // Compute and push additional arguments
									  for (int i = 1; i < es.Length; i++) {
										  es[i].Compile();
									  }
									  // Call the appropriate CallN method on the FunctionValue
									  ilg.Emit(OpCodes.Call, FunctionValue.callMethods[arity]);
								  }),
						  GenLoadErrorValue(ErrorValue.argTypeError));
			}
		}

		public override void CompileToDoubleOrNan() {
			Compile();
			UnwrapToDoubleOrNan();
		}

		public override CGExpr PEval(PEnv pEnv, bool hasDynamicControl) {
			// When FunctionValue is known, reduce to a CGSdfCall node.  
			// Don't actually call the function (even on constant arguments); could loop.
			CGExpr[] res = PEvalArgs(pEnv, hasDynamicControl);
			if (res[0] is CGValueConst) {
				FunctionValue fv = (res[0] as CGValueConst).Value as FunctionValue;
				if (fv != null) {
					CGExpr[] args = new CGExpr[fv.args.Length];
					int j = 1;
					for (int i = 0; i < args.Length; i++) {
						if (fv.args[i] != ErrorValue.naError) {
							args[i] = CGConst.Make(fv.args[i]);
						}
						else {
							args[i] = res[j++];
						}
					}
					return new CGSdfCall(fv.sdfInfo, args);
				}
				else {
					return new CGError(ErrorValue.argCountError);
				}
			}
			else {
				return new CGApply(res);
			}
		}

		public override CGExpr Residualize(CGExpr[] res) { throw new ImpossibleException("CGApply.Residualize"); }

		public override bool IsSerious(ref int bound) { return true; }

		public override string ToString() { return FormatAsCall("APPLY"); }

		public override void NoteTailPosition() {
			// We currently do not try to optimize tail calls in APPLY
			// isInTailPosition = true;
		}

		protected override Typ GetInputTypWithoutLengthCheck(int pos) {
			switch (pos) {
				// Could infer the expected argument types for a more precise function type:
				case 0:
					return Typ.Function;
				default:
					return Typ.Value;
			}
		}

		public override int Arity {
			get { return es.Length; }
		}

		public override Typ Type() {
			// An SDF in general returns a Value
			return Typ.Value;
		}
	}
}