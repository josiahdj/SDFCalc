using System;
using System.Collections.Generic;

namespace Corecalc.Funcalc {
	/// <summary>
	/// A CGCellRef is a reference to a single cell on this function sheet.
	/// </summary>
	public class CGCellRef : CGExpr {
		private readonly FullCellAddr cellAddr;
		private readonly Variable var;

		public CGCellRef(FullCellAddr cellAddr, Variable var) {
			this.cellAddr = cellAddr;
			this.var = var;
		}

		public override void Compile() {
			var.EmitLoad(ilg);
			if (var.Type == Typ.Number) {
				WrapDoubleToNumberValue();
			}
			// In other cases, there's no need to wrap the variable's contents
		}

		public override void CompileToDoubleOrNan() {
			Variable doubleVar;
			if (NumberVariables.TryGetValue(cellAddr, out doubleVar)) {
				doubleVar.EmitLoad(ilg);
			}
			else {
				if (var.Type == Typ.Value) {
					var.EmitLoad(ilg);
					UnwrapToDoubleOrNan();
				}
				else if (var.Type == Typ.Number) {
					var.EmitLoad(ilg);
				}
				else // A variable of a type not convertible to a float64, so ArgTypeError
				{
					LoadErrorNan(ErrorValue.argTypeError);
				}
			}
		}

		public override CGExpr PEval(PEnv pEnv, bool hasDynamicControl) { return pEnv[cellAddr]; }

		public override void EvalCond(PathCond evalCond,
									  IDictionary<FullCellAddr, PathCond> evalConds,
									  List<CGCachedExpr> caches) {
			PathCond old;
			if (evalConds.TryGetValue(cellAddr, out old)) {
				evalConds[cellAddr] = old.Or(evalCond);
			}
			else {
				evalConds[cellAddr] = evalCond;
			}
		}

		public override void DependsOn(FullCellAddr here, Action<FullCellAddr> dependsOn) { dependsOn(cellAddr); }

		public override void CountUses(Typ typ, HashBag<FullCellAddr> numberUses) {
			if (typ == Typ.Number) {
				numberUses.Add(this.cellAddr);
			}
		}

		public override string ToString() { return var.Name; }

		public override Typ Type() { return var.Type; }
	}
}