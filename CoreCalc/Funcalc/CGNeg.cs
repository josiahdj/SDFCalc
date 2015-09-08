using System.Reflection.Emit;

namespace Corecalc.Funcalc {
	/// <summary>
	/// A CGNeg is an application of the numeric negation operation.
	/// </summary>
	public class CGNeg : CGArithmetic1 {
		private static readonly Applier negApplier = Function.Get("NEG").Applier;

		public CGNeg(CGExpr[] es) : base(es, negApplier) { }

		public override void CompileToDoubleOrNan() {
			es[0].CompileToDoubleOrNan();
			ilg.Emit(OpCodes.Neg);
		}

		public override void CompileToDoubleProper(Gen ifProper, Gen ifOther) {
			if (es.Length != Arity) {
				SetArgCountErrorNan();
				ifOther.Generate(ilg);
			}
			else {
				es[0].CompileToDoubleProper(
										    new Gen(delegate {
														ilg.Emit(OpCodes.Neg);
														ifProper.Generate(ilg);
													}),
											ifOther);
			}
		}

		// -x is true/false/infinite/NaN if and only if x is:
		public override void CompileCondition(Gen ifTrue, Gen ifFalse, Gen ifOther) { es[0].CompileCondition(ifTrue, ifFalse, ifOther); }

		public override CGExpr Residualize(CGExpr[] res) { return new CGNeg(res); }

		public override string ToString() { return "-" + es[0]; }
	}
}