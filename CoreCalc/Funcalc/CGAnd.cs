using System.Collections.Generic;
using System.Reflection.Emit;

using CoreCalc.CellAddressing;
using CoreCalc.Types;
using CoreCalc.Values;

namespace Corecalc.Funcalc {
	/// <summary>
	/// A CGAnd represents a variadic AND operation, strict only
	/// in its first argument, although that's not Excel semantics.
	/// </summary>
	public class CGAnd : CGComposite {
		public CGAnd(CGExpr[] es) : base(es) { }

		public override void Compile() {
			CompileCondition(
							 new Gen(delegate { ilg.Emit(OpCodes.Ldsfld, NumberValue.oneField); }),
							 new Gen(delegate { ilg.Emit(OpCodes.Ldsfld, NumberValue.zeroField); }),
							 GenLoadTestDoubleErrorValue()
				);
		}

		public override void CompileToDoubleOrNan() {
			CompileCondition(
							 new Gen(delegate { ilg.Emit(OpCodes.Ldc_R8, 1.0); }),
							 new Gen(delegate { ilg.Emit(OpCodes.Ldc_R8, 0.0); }),
							 new Gen(delegate { ilg.Emit(OpCodes.Ldloc, testDouble); }));
		}

		public override void CompileToDoubleProper(Gen ifProper, Gen ifOther) {
			CompileCondition(
							 new Gen(delegate {
										 ilg.Emit(OpCodes.Ldc_R8, 1.0);
										 ifProper.Generate(ilg);
									 }),
							 new Gen(delegate {
										 ilg.Emit(OpCodes.Ldc_R8, 0.0);
										 ifProper.Generate(ilg);
									 }),
							 ifOther);
		}

		public override void CompileCondition(Gen ifTrue, Gen ifFalse, Gen ifOther) {
			for (int i = es.Length - 1; i >= 0; i--) {
				// These declarations are needed to capture rvalues rather than lvalues:
				CGExpr ei = es[i];
				Gen localIfTrue = ifTrue;
				ifTrue = new Gen(delegate { ei.CompileCondition(localIfTrue, ifFalse, ifOther); });
			}
			ifTrue.Generate(ilg);
		}

		public override CGExpr PEval(PEnv pEnv, bool hasDynamicControl) {
			List<CGExpr> res = new List<CGExpr>();
			for (int i = 0; i < es.Length; i++) {
				CGExpr ri = es[i].PEval(pEnv, hasDynamicControl || res.Count > 0);
				if (ri is CGNumberConst) {
					// A FALSE operand makes the AND false; a TRUE operand can be ignored
					if ((ri as CGNumberConst).number.value == 0.0) {
						return new CGNumberConst(NumberValue.ZERO);
					}
				}
				else {
					res.Add(ri);
				}
			}
			// The residual AND consists of the non-constant operands, if any
			if (res.Count == 0) {
				return new CGNumberConst(NumberValue.ONE);
			}
			else {
				return new CGAnd(res.ToArray());
			}
		}

		public override void EvalCond(PathCond evalCond,
									  IDictionary<FullCellAddr, PathCond> evalConds,
									  List<CGCachedExpr> caches) {
			for (int i = 0; i < es.Length; i++) {
				es[i].EvalCond(evalCond, evalConds, caches);
				if (SHORTCIRCUIT_EVALCONDS && i != es.Length - 1) {
					// Take short-circuit evaluation into account for precision
					CachedAtom atom = new CachedAtom(es[i], caches);
					evalCond = evalCond.And(atom);
					es[i] = atom.cachedExpr;
				}
			}
		}

		public override string ToString() { return FormatAsCall("AND"); }

		public override void CountUses(Typ typ, HashBag<FullCellAddr> numberUses) {
			foreach (CGExpr e in es) {
				e.CountUses(Typ.Number, numberUses);
			}
		}

		public override Typ Type() { return Typ.Number; }

		protected override Typ GetInputTypWithoutLengthCheck(int pos) { return Typ.Number; }

		public override int Arity {
			get { return es.Length; }
		}
	}
}