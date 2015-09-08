using System.Collections.Generic;
using System.Reflection.Emit;

namespace Corecalc.Funcalc {
	/// <summary>
	/// A CGIf represents an application of the IF built-in function.
	/// </summary>
	public class CGIf : CGComposite {
		public CGIf(CGExpr[] es) : base(es) { }

		// This handles compilation of IF(e0,e1,e2) in a Value context
		public override void Compile() {
			if (es.Length != 3) {
				LoadErrorValue(ErrorValue.argCountError);
			}
			else {
				es[0].CompileCondition(
									   new Gen(delegate { es[1].Compile(); }),
									   new Gen(delegate { es[2].Compile(); }),
									   new Gen(delegate {
												   ilg.Emit(OpCodes.Ldloc, testDouble);
												   WrapDoubleToNumberValue();
											   }));
			}
		}

		// This handles compilation of 5 + IF(e0,e1,e2) and such
		public override void CompileToDoubleOrNan() {
			if (es.Length != 3) {
				LoadErrorValue(ErrorValue.argCountError);
			}
			else {
				es[0].CompileCondition(
									   new Gen(delegate { es[1].CompileToDoubleOrNan(); }),
									   new Gen(delegate { es[2].CompileToDoubleOrNan(); }),
									   new Gen(delegate { ilg.Emit(OpCodes.Ldloc, testDouble); }));
			}
		}

		// This handles compilation of 5 > IF(e0,e1,e2) and such
		public override void CompileToDoubleProper(Gen ifProper, Gen ifOther) {
			if (es.Length != 3) {
				SetArgCountErrorNan();
				ifOther.Generate(ilg);
			}
			else {
				es[0].CompileCondition(
									   new Gen(delegate { es[1].CompileToDoubleProper(ifProper, ifOther); }),
									   new Gen(delegate { es[2].CompileToDoubleProper(ifProper, ifOther); }),
									   ifOther);
			}
		}

		// This handles compilation of IF(IF(e00,e01,e02), e1, e2) and such
		public override void CompileCondition(Gen ifTrue, Gen ifFalse, Gen ifOther) {
			if (es.Length != 3) {
				SetArgCountErrorNan();
				ifOther.Generate(ilg);
			}
			else {
				es[0].CompileCondition(
									   new Gen(delegate { es[1].CompileCondition(ifTrue, ifFalse, ifOther); }),
									   new Gen(delegate { es[2].CompileCondition(ifTrue, ifFalse, ifOther); }),
									   ifOther);
			}
		}

		public override CGExpr PEval(PEnv pEnv, bool hasDynamicControl) {
			CGExpr r0 = es[0].PEval(pEnv, hasDynamicControl);
			if (r0 is CGNumberConst) {
				if ((r0 as CGNumberConst).number.value != 0.0) {
					return es[1].PEval(pEnv, hasDynamicControl);
				}
				else {
					return es[2].PEval(pEnv, hasDynamicControl);
				}
			}
			else {
				return new CGIf(PEvalArgs(pEnv, r0, true));
			}
		}

		public override string ToString() { return FormatAsCall("IF"); }

		public override void EvalCond(PathCond evalCond,
									  IDictionary<FullCellAddr, PathCond> evalConds,
									  List<CGCachedExpr> caches) {
			if (es.Length == 3) {
				CachedAtom atom = new CachedAtom(es[0], caches);
				es[0].EvalCond(evalCond, evalConds, caches);
				es[0] = atom.cachedExpr;
				es[1].EvalCond(evalCond.And(atom), evalConds, caches);
				es[2].EvalCond(evalCond.AndNot(atom), evalConds, caches);
			}
		}

		public override void NoteTailPosition() {
			if (es.Length == 3) {
				es[1].NoteTailPosition();
				es[2].NoteTailPosition();
			}
		}

		public override void CountUses(Typ typ, HashBag<FullCellAddr> numberUses) {
			if (es.Length == 3) {
				es[0].CountUses(Typ.Number, numberUses);
				es[1].CountUses(typ, numberUses);
				es[2].CountUses(typ, numberUses);
			}
		}

		public override Typ Type() {
			if (es.Length != 3) {
				return Typ.Error;
			}
			else {
				return Lub(es[1].Type(), es[2].Type());
			}
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
			get { return 3; }
		}
	}
}