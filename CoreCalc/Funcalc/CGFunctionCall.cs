using System.Reflection.Emit;

using CoreCalc.Values;

namespace Corecalc.Funcalc {
	/// <summary>
	/// A CGFunctionCall is a call to a fixed-arity or variable-arity 
	/// strict built-in function.
	/// </summary>
	public class CGFunctionCall : CGStrictOperation {
		protected readonly FunctionInfo functionInfo;

		public CGFunctionCall(FunctionInfo functionInfo, CGExpr[] es)
			: base(es, functionInfo.applier) {
			this.functionInfo = functionInfo;
		}

		public override CGExpr PEval(PEnv pEnv, bool hasDynamicControl) {
			// Volatile functions must be residualized
			if (functionInfo.name == "NOW" || functionInfo.name == "RAND") {
				return Residualize(PEvalArgs(pEnv, hasDynamicControl));
			}
			else {
				return base.PEval(pEnv, hasDynamicControl);
			}
		}

		public override CGExpr Residualize(CGExpr[] res) { return new CGFunctionCall(functionInfo, res); }

		public override void Compile() {
			Gen success =
				new Gen(delegate {
							ilg.Emit(OpCodes.Call, functionInfo.methodInfo);
							if (functionInfo.signature.retType == Typ.Number) {
								WrapDoubleToNumberValue();
							}
						});
			if (Arity < 0) { // Variable arity
				CompileToValueArray(es.Length, 0, es);
				success.Generate(ilg);
			}
			else if (es.Length != Arity) {
				LoadErrorValue(ErrorValue.argCountError);
			}
			else {
				// TODO: ifOther should probably load error from testValue instead?
				Gen ifOther = GenLoadErrorValue(ErrorValue.argTypeError);
				CompileArgumentsAndApply(es, success, ifOther);
			}
		}

		public override void CompileToDoubleOrNan() {
			Gen success =
				new Gen(delegate {
							ilg.Emit(OpCodes.Call, functionInfo.methodInfo);
							if (functionInfo.signature.retType != Typ.Number) {
								UnwrapToDoubleOrNan();
							}
						});
			if (Arity < 0) { // Variable arity 
				CompileToValueArray(es.Length, 0, es);
				success.Generate(ilg);
			}
			else if (es.Length != Arity) {
				LoadErrorNan(ErrorValue.argCountError);
			}
			else {
				// TODO: ifOther should probably load error from testValue instead?
				Gen ifOther = GenLoadErrorNan(ErrorValue.argTypeError);
				CompileArgumentsAndApply(es, success, ifOther);
			}
		}

		// Special case for the argumentless and always-proper 
		// functions RAND and NOW, often used in conditions
		public override void CompileToDoubleProper(Gen ifProper, Gen ifOther) {
			if (es.Length == 0 && (functionInfo.name == "RAND" || functionInfo.name == "NOW")) {
				ilg.Emit(OpCodes.Call, functionInfo.methodInfo);
				ifProper.Generate(ilg);
			}
			else {
				base.CompileToDoubleProper(ifProper, ifOther);
			}
		}

		// Generate code to evaluate all argument expressions, including the receiver es[1]
		// if the method is an instance method, and convert their values to .NET types.

		private void CompileArgumentsAndApply(CGExpr[] es, Gen ifSuccess, Gen ifOther) {
			int argCount = es.Length;
			// The error continuations must pop the arguments computed so far.
			Gen[] errorCont = new Gen[argCount];
			if (argCount > 0) {
				errorCont[0] = ifOther;
			}
			for (int i = 1; i < argCount; i++) {
				int ii = i; // Capture lvalue -- do NOT inline!
				errorCont[ii] = new Gen(delegate {
											ilg.Emit(OpCodes.Pop);
											errorCont[ii - 1].Generate(ilg);
										});
			}
			// Generate code, backwards, to evaluate argument expressions and
			// convert to the .NET method's argument types
			for (int i = argCount - 1; i >= 0; i--) {
				// These local vars capture rvalue rather than lvalue -- do NOT inline them!
				CGExpr ei = es[i];
				Gen localSuccess = ifSuccess;
				Typ argType = functionInfo.signature.argTypes[i];
				Gen ifError = errorCont[i];
				if (argType == Typ.Number) {
					ifSuccess = new Gen(delegate {
											ei.CompileToDoubleOrNan();
											localSuccess.Generate(ilg);
										});
				}
				else if (argType == Typ.Function) {
					ifSuccess = new Gen(delegate {
											ei.Compile();
											CheckType(FunctionValue.type, localSuccess, ifError);
										});
				}
				else if (argType == Typ.Array) {
					ifSuccess = new Gen(delegate {
											ei.Compile();
											CheckType(ArrayValue.type, localSuccess, ifError);
										});
				}
				else if (argType == Typ.Text) {
					ifSuccess = new Gen(delegate {
											ei.Compile();
											CheckType(TextValue.type, localSuccess, ifError);
										});
				}
				else // argType.Value -- TODO: neglects to propagate ErrorValue from argument
				{
					ifSuccess = new Gen(delegate {
											ei.Compile();
											localSuccess.Generate(ilg);
										});
				}
			}
			ifSuccess.Generate(ilg);
		}

		public override bool IsSerious(ref int bound) { return functionInfo.isSerious || base.IsSerious(ref bound); }

		protected override Typ GetInputTypWithoutLengthCheck(int pos) { return Arity < 0 ? Typ.Value : functionInfo.signature.argTypes[pos]; }

		public override Typ Type() { return functionInfo.signature.retType; }

		public override int Arity {
			get { return functionInfo.signature.Arity; }
		}

		public override string ToString() { return FormatAsCall(functionInfo.name); }
	}
}