using System.Reflection.Emit;

namespace Corecalc.Funcalc {
	/// <summary>
	/// A CGNot is an application of the NOT built-in function.
	/// </summary>
	public class CGNot : CGArithmetic1 {
		private static readonly Applier notApplier = Function.Get("NOT").Applier;

		public CGNot(CGExpr[] es) : base(es, notApplier) { }

		public override void CompileToDoubleOrNan() {
			// sestoft: Seems a bit redundant?
			CompileToDoubleProper(
								  new Gen(delegate { }),
								  new Gen(delegate { ilg.Emit(OpCodes.Ldloc, testDouble); }));
		}

		public override void CompileToDoubleProper(Gen ifProper, Gen ifOther) {
			if (es.Length != Arity) {
				SetArgCountErrorNan();
				ifOther.Generate(ilg);
			}
			else {
				es[0].CompileToDoubleProper(
										    new Gen(delegate {
														ilg.Emit(OpCodes.Ldc_R8, 0.0);
														ilg.Emit(OpCodes.Ceq);
														ilg.Emit(OpCodes.Conv_R8);
														ifProper.Generate(ilg);
													}),
											ifOther);
			}
		}

		public override void CompileCondition(Gen ifTrue, Gen ifFalse, Gen ifOther) { es[0].CompileCondition(ifFalse, ifTrue, ifOther); }

		public override CGExpr Residualize(CGExpr[] res) { return new CGNot(res); }

		public override string ToString() { return FormatAsCall("NOT"); }
	}
}