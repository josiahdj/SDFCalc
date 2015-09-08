using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;

namespace Corecalc.Funcalc {
	/// <summary>
	/// A CGSdfCall is a call to a sheet-defined function.
	/// </summary>
	public class CGSdfCall : CGStrictOperation {
		private readonly SdfInfo sdfInfo;
		private bool isInTailPosition = false;

		public CGSdfCall(SdfInfo sdfInfo, CGExpr[] es)
			: base(es, null) {
			this.sdfInfo = sdfInfo;
		}

		public override void Compile() {
			if (es.Length != sdfInfo.arity) {
				LoadErrorValue(ErrorValue.argCountError);
			}
			else {
				ilg.Emit(OpCodes.Ldsfld, SdfManager.sdfDelegatesField);
				ilg.Emit(OpCodes.Ldc_I4, sdfInfo.index);
				ilg.Emit(OpCodes.Ldelem_Ref);
				ilg.Emit(OpCodes.Castclass, sdfInfo.MyType);
				for (int i = 0; i < es.Length; i++) {
					es[i].Compile();
				}
				if (isInTailPosition) {
					ilg.Emit(OpCodes.Tailcall);
				}
				ilg.Emit(OpCodes.Call, sdfInfo.MyInvoke);
				if (isInTailPosition) {
					ilg.Emit(OpCodes.Ret);
				}
			}
		}

		public override void CompileToDoubleOrNan() {
			Compile();
			UnwrapToDoubleOrNan();
		}

		public override CGExpr PEval(PEnv pEnv, bool hasDynamicControl) {
			CGExpr[] res = PEvalArgs(pEnv, hasDynamicControl);
			// The static argument positions are those that have args[i] != naError
			Value[] args = new Value[res.Length];
			bool anyStatic = false, allStatic = true;
			for (int i = 0; i < res.Length; i++) {
				if (res[i] is CGConst) {
					args[i] = (res[i] as CGConst).Value;
					anyStatic = true;
				}
				else {
					args[i] = ErrorValue.naError; // Generalize to dynamic
					allStatic = false;
				}
			}
			if (!hasDynamicControl) {
				// This would be wrong, because the called function might contain
				// volatile functions and in that case should residualize:
				// if (allStatic)       // If all arguments static, just call the SDF
				//  return CGConst.Make(sdfInfo.Apply(args));
				// else 
				if (anyStatic) // Specialize if there are static arguments
				{
					return Specialize(res, args);
				}
				// Do nothing if all arguments are dynamic
			}
			else {
				// If under dynamic control reduce to longest static prefix
				// where the argument values agree.
				// TODO: This is wrong -- should always specialize when the call is not 
				// recursive
				ICollection<Value[]> pending = SdfManager.PendingSpecializations(sdfInfo.name);
				Value[] maxArray = null;
				int maxCount = 0;
				foreach (Value[] vs in pending) {
					int agree = AgreeCount(vs, args);
					if (agree > maxCount) {
						maxCount = agree;
						maxArray = vs;
					}
				}
				if (maxCount > 0) {
					SetNaInArgs(maxArray, args);
					return Specialize(res, args);
				}
			}
			return new CGSdfCall(sdfInfo, res);
		}

		private static int AgreeCount(Value[] xs, Value[] ys) {
			Debug.Assert(xs.Length == ys.Length);
			int count = 0;
			for (int i = 0; i < xs.Length; i++) {
				if (xs[i] != ErrorValue.naError && ys[i] != ErrorValue.naError && xs[i].Equals(ys[i])) {
					count++;
				}
			}
			return count;
		}

		private static void SetNaInArgs(Value[] xs, Value[] args) {
			Debug.Assert(xs.Length == args.Length);
			for (int i = 0; i < xs.Length; i++) {
				if (!xs[i].Equals(args[i])) {
					args[i] = ErrorValue.naError; // Generalize to dynamic
				}
			}
		}

		private CGSdfCall Specialize(CGExpr[] res, Value[] args) {
			FunctionValue fv = new FunctionValue(sdfInfo, args);
			SdfInfo residualSdf = SdfManager.SpecializeAndCompile(fv);
			CGExpr[] residualArgs = new CGExpr[fv.Arity];
			int j = 0;
			for (int i = 0; i < args.Length; i++) {
				if (args[i] == ErrorValue.naError) {
					residualArgs[j++] = res[i];
				}
			}
			return new CGSdfCall(residualSdf, residualArgs);
		}

		public override CGExpr Residualize(CGExpr[] res) { throw new ImpossibleException("CGSdfCall.Residualize"); }

		public override bool IsSerious(ref int bound) { return true; }

		public override string ToString() { return FormatAsCall(sdfInfo.name); }

		public override void NoteTailPosition() { isInTailPosition = true; }

		public override Typ Type() { return Typ.Value; }

		protected override Typ GetInputTypWithoutLengthCheck(int pos) { return Typ.Value; }

		public override int Arity {
			get { return sdfInfo.arity; }
		}
	}
}