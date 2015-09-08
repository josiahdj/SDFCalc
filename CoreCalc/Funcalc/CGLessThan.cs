using System.Reflection.Emit;

namespace Corecalc.Funcalc {
	/// <summary>
	/// A CGLessThan is a comparison e1 < e2.
	/// </summary>
	public class CGLessThan : CGComparison {
		private static readonly Applier ltApplier = Function.Get("<").Applier;

		public CGLessThan(CGExpr[] es) : base(es, ltApplier) { }

		protected override void GenCompareDouble() { ilg.Emit(OpCodes.Clt); }

		protected override void GenDoubleFalseJump(Label target) { ilg.Emit(OpCodes.Bge, target); }

		public override CGExpr Residualize(CGExpr[] res) { return new CGLessThan(res); }

		protected override string Name {
			get { return "<"; }
		}
	}
}