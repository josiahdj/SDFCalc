using System.Reflection.Emit;

namespace Corecalc.Funcalc {
	/// <summary>
	/// A CGGreaterThan is a comparison e1 > e2.
	/// </summary>
	public class CGGreaterThan : CGComparison {
		private static readonly Applier gtApplier = Function.Get(">").Applier;

		public CGGreaterThan(CGExpr[] es) : base(es, gtApplier) { }

		protected override void GenCompareDouble() { ilg.Emit(OpCodes.Cgt); }

		protected override void GenDoubleFalseJump(Label target) { ilg.Emit(OpCodes.Ble, target); }

		public override CGExpr Residualize(CGExpr[] res) { return new CGGreaterThan(res); }

		protected override string Name {
			get { return ">"; }
		}
	}
}