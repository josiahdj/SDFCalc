using System;
using System.Reflection.Emit;

using CoreCalc.Values;

namespace Corecalc.Funcalc {
	/// <summary>
	/// A CGArithmetic2 is an application of a two-argument numeric-valued 
	/// built-in operator or function.
	/// </summary>
	public class CGArithmetic2 : CGStrictOperation {
		public readonly OpCode opCode;
		public readonly String op;

		public CGArithmetic2(OpCode opCode, String op, CGExpr[] es)
			: base(es, Function.Get(op).Applier) {
			this.opCode = opCode;
			this.op = op;
		}

		// Reductions such as 0*e==>0 are a bit dubious when you 
		// consider that e could evaluate to ArgType error or similar:
		public CGExpr Make(CGExpr[] es) {
			if (es.Length == 2) {
				if (op == "+" && es[0].Is(0)) {
					return es[1]; // 0+e = e
				}
				else if ((op == "+" || op == "-") && es[1].Is(0)) {
					return es[0]; // e+0 = e-0 = e
				}
				else if (op == "-" && es[0].Is(0)) {
					return new CGNeg(new CGExpr[] {es[1]}); // 0-e = -e
				}
				else if (op == "*" && (es[0].Is(0) || es[1].Is(0))) {
					return new CGNumberConst(NumberValue.ZERO); // 0*e = e*0 = 0 (**)
				}
				else if (op == "*" && es[0].Is(1)) {
					return es[1]; // 1*e = e
				}
				else if ((op == "*" || op == "/") && es[1].Is(1)) {
					return es[0]; // e*1 = e/1 = e
				}
				else if (op == "^" && (es[0].Is(1) || es[1].Is(1))) {
					return es[0]; // e^1 = e and also 1^e = 1 (IEEE)
				}
				else if (op == "^" && es[1].Is(0)) {
					return new CGNumberConst(NumberValue.ONE); // e^0 = 1 (IEEE)
				}
			}
			return new CGArithmetic2(opCode, op, es);
		}

		public override void Compile() {
			CompileToDoubleOrNan();
			WrapDoubleToNumberValue();
		}

		public override void CompileToDoubleOrNan() {
			es[0].CompileToDoubleOrNan();
			es[1].CompileToDoubleOrNan();
			ilg.Emit(opCode);
		}

		public override CGExpr Residualize(CGExpr[] res) {
			return Make(res);
//      return new CGArithmetic2(opCode, op, res);
		}

		public override Typ Type() { return Typ.Number; }

		protected override Typ GetInputTypWithoutLengthCheck(int pos) { return Typ.Number; }

		public override int Arity {
			get { return 2; }
		}

		public override string ToString() {
			if (es.Length == 2) {
				return "(" + es[0] + op + es[1] + ")";
			}
			else {
				return "Err: ArgCount";
			}
		}
	}
}