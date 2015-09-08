using System;
using System.Reflection.Emit;

using CoreCalc.CellAddressing;
using CoreCalc.Values;

namespace Corecalc.Funcalc {
	/// <summary>
	/// A CGComposite represents an expression that may have subexpressions, 
	/// such as an arithmetic operation, a call to a built-in function 
	/// (including IF, AND, OR, RAND, LN, EXP, ...) or to a sheet-defined or 
	/// external function.
	/// </summary>
	public abstract class CGComposite : CGExpr {
		protected readonly CGExpr[] es;

		protected CGComposite(CGExpr[] es) { this.es = es; }

		public static CGExpr Make(String name, CGExpr[] es) {
			name = name.ToUpper();
			// This switch should agree with the function table in Functions.cs
			switch (name) {
				case "+":
					return new CGArithmetic2(OpCodes.Add, name, es);
				case "*":
					return new CGArithmetic2(OpCodes.Mul, name, es);
				case "-":
					return new CGArithmetic2(OpCodes.Sub, name, es);
				case "/":
					return new CGArithmetic2(OpCodes.Div, name, es);

				case "=":
					return CGEqual.Make(es);
				case "<>":
					return CGNotEqual.Make(es);
				case ">":
					return new CGGreaterThan(es);
				case "<=":
					return new CGLessThanOrEqual(es);
				case "<":
					return new CGLessThan(es);
				case ">=":
					return new CGGreaterThanOrEqual(es);

				// Non-strict, or other special treatment
				case "AND":
					return new CGAnd(es);
				case "APPLY":
					return new CGApply(es);
				case "CHOOSE":
					return new CGChoose(es);
				case "CLOSURE":
					return new CGClosure(es);
				case "ERR":
					return new CGError(es);
				case "EXTERN":
					return new CGExtern(es);
				case "IF":
					return new CGIf(es);
				case "NA":
					if (es.Length == 0) {
						return new CGError(ErrorValue.naError);
					}
					else {
						return new CGError(ErrorValue.argCountError);
					}
				case "NEG":
					return new CGNeg(es);
				case "NOT":
					return new CGNot(es);
				case "OR":
					return new CGOr(es);
				case "PI":
					if (es.Length == 0) {
						return new CGNumberConst(NumberValue.PI);
					}
					else {
						return new CGError(ErrorValue.argCountError);
					}
				case "VOLATILIZE":
					if (es.Length == 1) {
						return es[0];
					}
					else {
						return new CGError(ErrorValue.argCountError);
					}
				default:
					// The general case for most built-in functions with unspecific argument types
					FunctionInfo functionInfo;
					if (FunctionInfo.Find(name, out functionInfo)) {
						return new CGFunctionCall(functionInfo, es);
					}
					else { // May be a sheet-defined function
						SdfInfo sdfInfo = SdfManager.GetInfo(name);
						if (sdfInfo != null) {
							return new CGSdfCall(sdfInfo, es);
						}
						else {
							return new CGError(ErrorValue.nameError);
						}
					}
			}
		}

		protected abstract Typ GetInputTypWithoutLengthCheck(int pos);

		public Typ GetInputTyp(int pos) {
			if (0 <= pos && pos < Arity) {
				return GetInputTypWithoutLengthCheck(pos);
			}
			else {
				return Typ.Value;
			}
		}

		public CGExpr[] PEvalArgs(PEnv pEnv, CGExpr r0, bool hasDynamicControl) {
			CGExpr[] res = new CGExpr[es.Length];
			res[0] = r0;
			for (int i = 1; i < es.Length; i++) {
				res[i] = es[i].PEval(pEnv, hasDynamicControl);
			}
			return res;
		}

		public override bool IsSerious(ref int bound) {
			bool serious = base.IsSerious(ref bound);
			for (int i = 0; !serious && i < es.Length; i++) {
				serious = es[i].IsSerious(ref bound);
			}
			return serious;
		}

		public override void DependsOn(FullCellAddr here, Action<FullCellAddr> dependsOn) {
			foreach (CGExpr e in es) {
				e.DependsOn(here, dependsOn);
			}
		}

		protected String FormatAsCall(String name) { return FunctionValue.FormatAsCall(name, es); }

		public abstract int Arity { get; }
	}
}