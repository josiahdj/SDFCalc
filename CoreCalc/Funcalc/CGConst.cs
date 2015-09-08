using System;
using System.Collections.Generic;

using CoreCalc.CellAddressing;
using CoreCalc.Types;
using CoreCalc.Values;

namespace Corecalc.Funcalc {
	/// <summary>
	/// A CGConst is a constant expression.
	/// </summary>
	public abstract class CGConst : CGExpr {
		public static CGConst Make(double d) { return Make(NumberValue.Make(d)); }

		public static CGConst Make(Value v) {
			if (v is NumberValue) {
				return new CGNumberConst((v as NumberValue));
			}
			else if (v is TextValue) {
				return new CGTextConst(v as TextValue);
			}
			else if (v is ErrorValue) {
				return new CGError((v as ErrorValue));
			}
			else {
				return new CGValueConst(v);
			}
		}

		public abstract Value Value { get; }

		public override CGExpr PEval(PEnv pEnv, bool hasDynamicControl) { return this; }

		public override void EvalCond(PathCond evalCond,
									  IDictionary<FullCellAddr, PathCond> evalConds,
									  List<CGCachedExpr> caches) {}

		public override void DependsOn(FullCellAddr here, Action<FullCellAddr> dependsOn) { }

		public override void CountUses(Typ typ, HashBag<FullCellAddr> numberUses) { }
	}
}