using System.Collections.Generic;

using CoreCalc.CellAddressing;
using CoreCalc.Expressions;
using CoreCalc.Types;

namespace Corecalc.Funcalc {
	/// <summary>
	/// A CGStrictOperation is a composite expression that evaluates all
	/// its subexpressions.
	/// </summary>
	public abstract class CGStrictOperation : CGComposite {
		// For partial evaluation; null for CGApply, CGExtern, CGSdfCall:
		public readonly Applier applier;

		public CGStrictOperation(CGExpr[] es, Applier applier)
			: base(es) {
			this.applier = applier;
		}

		public CGExpr[] PEvalArgs(PEnv pEnv, bool hasDynamicControl) {
			CGExpr[] res = new CGExpr[es.Length];
			for (int i = 0; i < es.Length; i++) {
				res[i] = es[i].PEval(pEnv, hasDynamicControl);
			}
			return res;
		}

		public bool AllConstant(CGExpr[] res) {
			for (int i = 0; i < res.Length; i++) {
				if (!(res[i] is CGConst)) {
					return false;
				}
			}
			return true;
		}

		public override CGExpr PEval(PEnv pEnv, bool hasDynamicControl) {
			CGExpr[] res = PEvalArgs(pEnv, hasDynamicControl);
			// If all args are constant then evaluate else residualize:
			if (AllConstant(res)) {
				Expr[] es = new Expr[res.Length];
				for (int i = 0; i < res.Length; i++) {
					es[i] = Const.Make((res[i] as CGConst).Value);
				}
				// Use the interpretive implementation's applier on a fake sheet 
				// and fake cell coordinates, but constant argument expressions:
				return CGConst.Make(applier(null, es, -1, -1));
			}
			else {
				return Residualize(res);
			}
		}

		public abstract CGExpr Residualize(CGExpr[] res);

		public override void EvalCond(PathCond evalCond,
									  IDictionary<FullCellAddr, PathCond> evalConds,
									  List<CGCachedExpr> caches) {
			for (int i = 0; i < es.Length; i++) {
				es[i].EvalCond(evalCond, evalConds, caches);
			}
		}

		public override void CountUses(Typ typ, HashBag<FullCellAddr> numberUses) {
			for (int i = 0; i < es.Length; i++) {
				es[i].CountUses(GetInputTyp(i), numberUses);
			}
		}
	}
}