using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

using CoreCalc.CellAddressing;
using CoreCalc.Types;
using CoreCalc.Values;

namespace Corecalc.Funcalc {
	/// <summary>
	/// A CGCachedExpr holds a number-valued expression and its cached value;
	/// used in evaluation conditions.
	/// </summary>
	public class CGCachedExpr : CGExpr {
		public readonly CGExpr expr;
		private readonly CachedAtom cachedAtom;
		private LocalBuilder cacheVariable; // The cache localvar (double) 
		private int generateCount = 0; // Number of times emitted by CachedAtom.ToCGExpr
		private readonly int cacheNumber; // Within the current SDF, for diagnostics only

		private const int uninitializedBits = -1;
		private static readonly double uninitializedNan = ErrorValue.MakeNan(uninitializedBits);

		private static readonly MethodInfo
			doubleToInt64BitsMethod = typeof (System.BitConverter).GetMethod("DoubleToInt64Bits");

		public CGCachedExpr(CGExpr expr, CachedAtom cachedAtom, List<CGCachedExpr> caches) {
			this.expr = expr;
			this.cachedAtom = cachedAtom;
			this.cacheNumber = caches.Count;
			caches.Add(this);
		}

		public override void Compile() {
			CompileToDoubleOrNan();
			ilg.Emit(OpCodes.Call, NumberValue.makeMethod);
		}

		public override void CompileToDoubleOrNan() {
			if (IsCacheNeeded) {
				EmitCacheAccess();
			}
			else {
				expr.CompileToDoubleOrNan();
			}
		}

		public override void CompileCondition(Gen ifTrue, Gen ifFalse, Gen ifOther) {
			if (IsCacheNeeded) {
				// Call CompileToDoubleProper via base class CompileCondition
				base.CompileCondition(ifTrue, ifFalse, ifOther);
			}
			else {
				expr.CompileCondition(ifTrue, ifFalse, ifOther);
			}
		}

		public override void CompileToDoubleProper(Gen ifProper, Gen ifOther) {
			if (IsCacheNeeded) {
				// Call CompileToDoubleOrNan via base class CompileToDoubleProper
				base.CompileToDoubleProper(ifProper, ifOther);
			}
			else {
				expr.CompileToDoubleProper(ifProper, ifOther);
			}
		}

		public void IncrementGenerateCount() { generateCount++; }

		// Cache only if used in PathCond and worth caching
		private bool IsCacheNeeded {
			get { return generateCount > 0 && !(expr is CGCellRef) && !(expr is CGConst); }
		}

		/// <summary>
		/// This must be called, at most once, before any use of the cache is generated.
		/// </summary>
		public void EmitCacheInitialization() {
			if (IsCacheNeeded) {
				Debug.Assert(cacheVariable == null); // Has not been called before
				cacheVariable = ilg.DeclareLocal(typeof (double));
				// Console.WriteLine("Emitted {0} in localvar {1}", this, cacheVariable);
				ilg.Emit(OpCodes.Ldc_R8, uninitializedNan);
				ilg.Emit(OpCodes.Stloc, cacheVariable);
			}
		}

		/// <summary>
		/// Generate code for each use of the cache.
		/// </summary>
		private void EmitCacheAccess() {
			Debug.Assert(cacheVariable != null); // EmitCacheInitialization() has been called
			ilg.Emit(OpCodes.Ldloc, cacheVariable);
			ilg.Emit(OpCodes.Call, isNaNMethod);
			Label endLabel = ilg.DefineLabel();
			ilg.Emit(OpCodes.Brfalse, endLabel); // Already computed, non-NaN result
			ilg.Emit(OpCodes.Ldloc, cacheVariable);
			ilg.Emit(OpCodes.Call, doubleToInt64BitsMethod);
			ilg.Emit(OpCodes.Conv_I4);
			ilg.Emit(OpCodes.Ldc_I4, uninitializedBits);
			ilg.Emit(OpCodes.Ceq);
			ilg.Emit(OpCodes.Brfalse, endLabel); // Already computed, NaN result
			expr.CompileToDoubleOrNan();
			// ilg.EmitWriteLine("Filled " + this);
			ilg.Emit(OpCodes.Stloc, cacheVariable);
			ilg.MarkLabel(endLabel);
			ilg.Emit(OpCodes.Ldloc, cacheVariable);
		}

		public override CGExpr PEval(PEnv pEnv, bool hasDynamicControl) {
			// Partial evaluation may encounter a cached expression in conditionals
			// etc, and should simply partially evaluate the expression inside the cache.
			// No duplication of cached expression happens in the residual program because 
			// a cached expression appears only once in the regular code, and no occurrence 
			// from evaluation conditions is added to the residual program; see ComputeCell.PEval.
			// New evaluation conditions are added later; see ProgramLines.CompileToDelegate.
			return expr.PEval(pEnv, hasDynamicControl);
		}

		public override void EvalCond(PathCond evalCond,
									  IDictionary<FullCellAddr, PathCond> evalConds,
									  List<CGCachedExpr> caches) {
			throw new ImpossibleException("CGCachedExpr.EvalCond");
		}

		public override void DependsOn(FullCellAddr here, Action<FullCellAddr> dependsOn) { expr.DependsOn(here, dependsOn); }

		public override void CountUses(Typ typ, HashBag<FullCellAddr> numberUses) { expr.CountUses(typ, numberUses); }

		public override string ToString() { return "CACHE#" + cacheNumber + "[" + expr + "]"; }

		public override Typ Type() { return Typ.Number; }
	}
}