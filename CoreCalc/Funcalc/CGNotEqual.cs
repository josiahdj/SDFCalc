using System.Reflection.Emit;

namespace Corecalc.Funcalc {
	/// <summary>
	/// A CGNotEqual is a comparison e1 <> e2.
	/// </summary>
	public class CGNotEqual : CGComparison {
		private CGNotEqual(CGExpr[] es) : base(es, Function.Get("<>").Applier) { }

		public static CGExpr Make(CGExpr[] es) {
			if (es.Length == 2) {
				if (es[0].Is(0)) // 0.0<>e1 ==> AND(e1)
				{
					return new CGAnd(new CGExpr[] {es[1]});
				}
				else if (es[1].Is(0)) // e0<>0.0 ==> AND(e0)
				{
					return new CGAnd(new CGExpr[] {es[0]});
				}
			}
			return new CGNotEqual(es);
		}

		protected override void GenCompareDouble() {
			ilg.Emit(OpCodes.Ceq);
			ilg.Emit(OpCodes.Ldc_I4_0);
			ilg.Emit(OpCodes.Ceq);
		}

		protected override void GenDoubleFalseJump(Label target) { ilg.Emit(OpCodes.Beq, target); }

		public override CGExpr Residualize(CGExpr[] res) { return Make(res); }

		protected override string Name {
			get { return "<>"; }
		}
	}
}