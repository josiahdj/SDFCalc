using System.Reflection.Emit;

namespace Corecalc.Funcalc {
	/// <summary>
	/// A CGGreaterThanOrEqual is a comparison e1 >= e2.
	/// </summary>
	public class CGGreaterThanOrEqual : CGComparison {
		private static readonly Applier geApplier = Function.Get(">=").Applier;

		public CGGreaterThanOrEqual(CGExpr[] es) : base(es, geApplier) { }

		protected override void GenCompareDouble() {
			ilg.Emit(OpCodes.Clt);
			ilg.Emit(OpCodes.Ldc_I4_0);
			ilg.Emit(OpCodes.Ceq);
		}

		protected override void GenDoubleFalseJump(Label target) { ilg.Emit(OpCodes.Blt, target); }

		public override CGExpr Residualize(CGExpr[] res) { return new CGGreaterThanOrEqual(res); }

		protected override string Name {
			get { return ">="; }
		}
	}
}