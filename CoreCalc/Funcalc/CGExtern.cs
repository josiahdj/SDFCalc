using System;
using System.Collections.Generic;
using System.Reflection.Emit;

using CoreCalc.Types;
using CoreCalc.Values;

namespace Corecalc.Funcalc {
	/// <summary>
	/// A CGExtern is a call to EXTERN, ie. an external .NET function.
	/// </summary>
	internal class CGExtern : CGStrictOperation {
		// If ef==null then errorValue is set to the error that occurred 
		// during lookup, and the other fields are invalid
		private readonly ExternalFunction ef;
		private readonly ErrorValue errorValue;
		private readonly Typ resType;
		private readonly Typ[] argTypes;

		private static readonly ISet<Type>
			signed32 = new HashSet<Type>(),
			unsigned32 = new HashSet<Type>(),
			numeric = new HashSet<Type>();

		static CGExtern() {
			signed32.Add(typeof (System.Int32));
			signed32.Add(typeof (System.Int16));
			signed32.Add(typeof (System.SByte));
			unsigned32.Add(typeof (System.UInt32));
			unsigned32.Add(typeof (System.UInt16));
			unsigned32.Add(typeof (System.Byte));
			numeric.Add(typeof (System.Double));
			numeric.Add(typeof (System.Single));
			numeric.Add(typeof (System.Int64));
			numeric.Add(typeof (System.UInt64));
			numeric.Add(typeof (System.Boolean));
			numeric.UnionWith(signed32);
			numeric.UnionWith(unsigned32);
		}

		public CGExtern(CGExpr[] es)
			: base(es, null) {
			if (es.Length < 1) {
				errorValue = ErrorValue.argCountError;
			}
			else {
				CGTextConst nameAndSignatureConst = es[0] as CGTextConst;
				if (nameAndSignatureConst == null) {
					errorValue = ErrorValue.argTypeError;
				}
				else {
					try {
						// This retrieves the method from cache, or creates it:
						ef = ExternalFunction.Make(nameAndSignatureConst.value.value);
						if (ef.arity != es.Length - 1) {
							ef = null;
							errorValue = ErrorValue.argCountError;
						}
						else {
							resType = FromType(ef.ResType);
							argTypes = new Typ[ef.arity];
							for (int i = 0; i < argTypes.Length; i++) {
								argTypes[i] = FromType(ef.ArgType(i));
							}
						}
					}
					catch (Exception exn) // Covers a multitude of sins
					{
						errorValue = ErrorValue.Make(exn.Message);
					}
				}
			}
		}

		private static Typ FromType(Type t) {
			if (numeric.Contains(t)) {
				return Typ.Number;
			}
			else if (t == typeof (System.String)) {
				return Typ.Text;
			}
			else {
				return Typ.Value;
			}
		}

		public override void Compile() {
			if (ef == null) {
				LoadErrorValue(errorValue);
			}
			else {
				// If argument evaluation is successful, call the external function 
				// and convert its result to Value; if unsuccessful, return ArgTypeError
				Gen success;
				// First some return type special cases, avoid boxing:
				if (ef.ResType == typeof (System.Double)) {
					success = new Gen(delegate {
										  ef.EmitCall(ilg);
										  ilg.Emit(OpCodes.Call, NumberValue.makeMethod);
									  });
				}
				else if (numeric.Contains(ef.ResType)) {
					success = new Gen(delegate {
										  ef.EmitCall(ilg);
										  ilg.Emit(OpCodes.Conv_R8);
										  ilg.Emit(OpCodes.Call, NumberValue.makeMethod);
									  });
				}
				else if (ef.ResType == typeof (char)) {
					success = new Gen(delegate {
										  ef.EmitCall(ilg);
										  ilg.Emit(OpCodes.Call, TextValue.fromNakedCharMethod);
									  });
				}
				else if (ef.ResType == typeof (void)) {
					success = new Gen(delegate {
										  ef.EmitCall(ilg);
										  ilg.Emit(OpCodes.Ldsfld, TextValue.voidField);
									  });
				}
				else {
					success = new Gen(delegate {
										  ef.EmitCall(ilg);
										  if (ef.ResType.IsValueType) {
											  ilg.Emit(OpCodes.Box, ef.ResType);
										  }
										  ilg.Emit(OpCodes.Call, ef.ResConverter.Method);
									  });
				}
				Gen ifOther = GenLoadErrorValue(ErrorValue.argTypeError);
				CompileArgumentsAndApply(es, success, ifOther);
			}
		}

		public override void CompileToDoubleOrNan() {
			if (ef == null) {
				ilg.Emit(OpCodes.Ldc_R8, errorValue.ErrorNan);
			}
			else {
				// sestoft: This is maybe correct
				Gen ifOther = GenLoadErrorNan(ErrorValue.argTypeError);
				if (ef.ResType == typeof (System.Double)) {
					// If argument evaluation is successful, call external function 
					// and continue with ifDouble; otherwise continue with ifOther
					Gen success = new Gen(delegate { ef.EmitCall(ilg); });
					CompileArgumentsAndApply(es, success, ifOther);
				}
				else if (numeric.Contains(ef.ResType)) {
					// If argument evaluation is successful, call external function, convert 
					// to float64
					Gen success =
						new Gen(delegate {
									ef.EmitCall(ilg);
									ilg.Emit(OpCodes.Conv_R8);
								});
					CompileArgumentsAndApply(es, success, ifOther);
				}
				else // Result type cannot be converted to a float64
				{
					ifOther.Generate(ilg);
				}
			}
		}

		// Generate code to evaluate all argument expressions, including the receiver es[1]
		// if the method is an instance method, and convert their values to .NET types.

		private void CompileArgumentsAndApply(CGExpr[] es, Gen ifSuccess, Gen ifOther) {
			int argCount = es.Length - 1;
			// The error continuations must pop the arguments computed so far:
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
			// convert to external method's argument types
			for (int i = argCount - 1; i >= 0; i--) {
				// These local vars capture rvalue rather than lvalue -- do NOT inline them!
				CGExpr ei = es[i + 1];
				Gen localSuccess = ifSuccess;
				int argIndex = i;
				Type argType = ef.ArgType(i);
				Gen ifError = errorCont[i];
				// First some special cases to avoid boxing:
				if (argType == typeof (System.Double)) {
					ifSuccess = new Gen(
						delegate {
							ei.CompileToDoubleOrNan();
							localSuccess.Generate(ilg);
						});
				}
				else if (argType == typeof (System.Single)) {
					ifSuccess = new Gen(
						delegate {
							ei.CompileToDoubleOrNan();
							ilg.Emit(OpCodes.Conv_R4);
							localSuccess.Generate(ilg);
						});
				}
				else if (signed32.Contains(argType)) {
					ifSuccess = new Gen(
						delegate {
							ei.CompileToDoubleProper(
													 new Gen(delegate {
																 ilg.Emit(OpCodes.Conv_I4);
																 localSuccess.Generate(ilg);
															 }),
													 ifError);
						});
				}
				else if (unsigned32.Contains(argType)) {
					ifSuccess = new Gen(
						delegate {
							ei.CompileToDoubleProper(
													 new Gen(delegate {
																 ilg.Emit(OpCodes.Conv_U4);
																 localSuccess.Generate(ilg);
															 }),
													 ifError);
						});
				}
				else if (argType == typeof (System.Int64)) {
					ifSuccess = new Gen(
						delegate {
							ei.CompileToDoubleProper(
													 new Gen(delegate {
																 ilg.Emit(OpCodes.Conv_I8);
																 localSuccess.Generate(ilg);
															 }),
													 ifError);
						});
				}
				else if (argType == typeof (System.UInt64)) {
					ifSuccess = new Gen(
						delegate {
							ei.CompileToDoubleProper(
													 new Gen(delegate {
																 ilg.Emit(OpCodes.Conv_U8);
																 localSuccess.Generate(ilg);
															 }),
													 ifError);
						});
				}
				else if (argType == typeof (System.Boolean)) {
					ifSuccess = new Gen(
						delegate {
							ei.CompileToDoubleProper(
													 new Gen(delegate {
																 ilg.Emit(OpCodes.Ldc_R8, 0.0);
																 ilg.Emit(OpCodes.Ceq);
																 localSuccess.Generate(ilg);
															 }),
													 ifError);
						});
				}
				else if (argType == typeof (System.Char)) {
					ifSuccess = new Gen(
						delegate {
							ei.Compile();
							ilg.Emit(OpCodes.Call, TextValue.toNakedCharMethod);
							localSuccess.Generate(ilg);
						});
				}
				else if (argType == typeof (System.String)) {
					ifSuccess = new Gen(
						delegate {
							ei.Compile();
							UnwrapToString(localSuccess, ifError);
						});
				}
				else // General cases: String[], double[], double[,], ...
				{
					ifSuccess = new Gen(
						delegate {
							ei.Compile();
							ilg.Emit(OpCodes.Call, ef.ArgConverter(argIndex).Method);
							if (argType.IsValueType) // must unbox wrapped value type, but this is too simple-minded
							{
								ilg.Emit(OpCodes.Unbox, argType);
							}
							localSuccess.Generate(ilg);
						});
				}
			}
			ifSuccess.Generate(ilg);
		}

		public override CGExpr PEval(PEnv pEnv, bool hasDynamicControl) {
			// Always residualize; external function could have effects or be volatile
			return new CGExtern(PEvalArgs(pEnv, hasDynamicControl));
		}

		public override CGExpr Residualize(CGExpr[] res) { throw new ImpossibleException("CGExtern.Residualize"); }

		public override bool IsSerious(ref int bound) { return true; }

		protected override Typ GetInputTypWithoutLengthCheck(int pos) {
			if (ef == null) {
				return Typ.Value;
			}
			switch (pos) {
				case 0:
					return Typ.Text;
				default:
					return argTypes[pos - 1];
			}
		}

		public override int Arity {
			get { return es.Length; }
		}

		public override Typ Type() {
			if (ef == null) {
				return Typ.Value;
			}
			else {
				return resType; // From external function signature
			}
		}
	}
}