using System.Reflection.Emit;

namespace Corecalc.Funcalc {
	/// <summary>
	/// A CGEqual is a comparison e1 = e2.
	/// </summary>
	public class CGEqual : CGComparison {
		private static readonly Applier eqApplier = Function.Get("=").Applier;

		public CGEqual(CGExpr[] es) : base(es, eqApplier) { }

		public static CGExpr Make(CGExpr[] es) {
			if (es.Length == 2) {
				if (es[0].Is(0)) // 0.0=e1 ==> NOT(e1)
				{
					return new CGNot(new CGExpr[] {es[1]});
				}
				else if (es[1].Is(0)) // e0=0.0 ==> NOT(e0)
				{
					return new CGNot(new CGExpr[] {es[0]});
				}
			}
			return new CGEqual(es);
		}

		protected override void GenCompareDouble() { ilg.Emit(OpCodes.Ceq); }

		protected override void GenDoubleFalseJump(Label target) { ilg.Emit(OpCodes.Bne_Un, target); }

		public override CGExpr Residualize(CGExpr[] res) { return Make(res); }

		protected override string Name {
			get { return "="; }
		}
	}
}