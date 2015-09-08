using System.Reflection.Emit;

namespace Corecalc.Funcalc {
	/// <summary>
	/// A CGLessThanOrEqual is a comparison e1 <= e2.
	/// </summary>
	public class CGLessThanOrEqual : CGComparison {
		private static readonly Applier leApplier = Function.Get("<=").Applier;

		public CGLessThanOrEqual(CGExpr[] es) : base(es, leApplier) { }

		protected override void GenCompareDouble() {
			// OpCodes.Not negates all int bits and doesn't work here!
			ilg.Emit(OpCodes.Cgt);
			ilg.Emit(OpCodes.Ldc_I4_0);
			ilg.Emit(OpCodes.Ceq);
		}

		protected override void GenDoubleFalseJump(Label target) { ilg.Emit(OpCodes.Bgt, target); }

		public override CGExpr Residualize(CGExpr[] res) { return new CGLessThanOrEqual(res); }

		protected override string Name {
			get { return "<="; }
		}
	}
}