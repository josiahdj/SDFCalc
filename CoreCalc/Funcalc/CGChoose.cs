using System.Collections.Generic;
using System.Reflection.Emit;

namespace Corecalc.Funcalc {
	/// <summary>
	/// A CGChoose represents an application of the CHOOSE built-in function.
	/// </summary>
	public class CGChoose : CGComposite {
		public CGChoose(CGExpr[] es) : base(es) { }

		public override void Compile() {
			es[0].CompileToDoubleProper(new Gen(delegate {
													Label endLabel = ilg.DefineLabel();
													Label[] labels = new Label[es.Length - 1];
													for (int i = 1; i < es.Length; i++) {
														labels[i - 1] = ilg.DefineLabel();
													}
													ilg.Emit(OpCodes.Conv_I4);
													ilg.Emit(OpCodes.Ldc_I4, 1);
													ilg.Emit(OpCodes.Sub);
													ilg.Emit(OpCodes.Switch, labels);
													LoadErrorValue(ErrorValue.valueError);
													for (int i = 1; i < es.Length; i++) {
														ilg.Emit(OpCodes.Br, endLabel);
														ilg.MarkLabel(labels[i - 1]);
														es[i].Compile();
													}
													ilg.MarkLabel(endLabel);
												}),
										GenLoadErrorValue(ErrorValue.argTypeError)
				);
		}

		public override void CompileToDoubleOrNan() {
			es[0].CompileToDoubleProper(new Gen(delegate {
													Label endLabel = ilg.DefineLabel();
													Label[] labels = new Label[es.Length - 1];
													for (int i = 1; i < es.Length; i++) {
														labels[i - 1] = ilg.DefineLabel();
													}
													ilg.Emit(OpCodes.Conv_I4);
													ilg.Emit(OpCodes.Ldc_I4, 1);
													ilg.Emit(OpCodes.Sub);
													ilg.Emit(OpCodes.Switch, labels);
													LoadErrorNan(ErrorValue.valueError);
													for (int i = 1; i < es.Length; i++) {
														ilg.Emit(OpCodes.Br, endLabel);
														ilg.MarkLabel(labels[i - 1]);
														es[i].CompileToDoubleOrNan();
													}
													ilg.MarkLabel(endLabel);
												}),
										GenLoadErrorNan(ErrorValue.argTypeError)
				);
		}

		public override CGExpr PEval(PEnv pEnv, bool hasDynamicControl) {
			CGExpr r0 = es[0].PEval(pEnv, hasDynamicControl);
			if (r0 is CGNumberConst) {
				int index = (int)((r0 as CGNumberConst).number.value);
				if (index < 1 || index >= es.Length) {
					return new CGError(ErrorValue.valueError);
				}
				else {
					return es[index].PEval(pEnv, hasDynamicControl);
				}
			}
			else {
				return new CGChoose(PEvalArgs(pEnv, r0, true /* has dynamic control */));
			}
		}

		public override string ToString() { return FormatAsCall("CHOOSE"); }

		public override void EvalCond(PathCond evalCond,
									  IDictionary<FullCellAddr, PathCond> evalConds,
									  List<CGCachedExpr> caches) {
			if (es.Length >= 1) {
				CachedAtom atom = new CachedAtom(es[0], caches);
				CGCachedExpr cached = atom.cachedExpr;
				es[0].EvalCond(evalCond, evalConds, caches);
				es[0] = cached;
				for (int i = 1; i < es.Length; i++) {
					CGExpr iConst = CGConst.Make(i);
					CGExpr cond = new CGEqual(new CGExpr[] {cached, iConst});
					es[i].EvalCond(evalCond.And(new CachedAtom(cond, caches)), evalConds, caches);
				}
			}
		}

		public override void NoteTailPosition() {
			for (int i = 1; i < es.Length; i++) {
				es[i].NoteTailPosition();
			}
		}

		public override void CountUses(Typ typ, HashBag<FullCellAddr> numberUses) {
			es[0].CountUses(Typ.Number, numberUses);
			for (int i = 1; i < es.Length; i++) {
				es[i].CountUses(typ, numberUses);
			}
		}

		public override Typ Type() {
			Typ result = Typ.Error;
			for (int i = 1; i < es.Length; i++) {
				result = Lub(result, es[i].Type());
			}
			return result;
		}

		protected override Typ GetInputTypWithoutLengthCheck(int pos) {
			if (pos == 0) {
				return Typ.Number;
			}
			else {
				return Typ.Value;
			}
		}

		public override int Arity {
			get { return es.Length; }
		}
	}
}