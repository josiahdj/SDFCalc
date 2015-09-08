using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Corecalc.Funcalc {
	/// <summary>
	/// A CGNormalCellArea is a reference to a single cell on a normal data sheet.
	/// </summary>
	public class CGNormalCellArea : CGExpr {
		private readonly int index; // If negative, array reference is illegal

		private static readonly MethodInfo getArrayViewMethod
			= typeof (CGNormalCellArea).GetMethod("GetArrayView");

		private static readonly ValueTable<ArrayView> valueTable
			= new ValueTable<ArrayView>();

		public CGNormalCellArea(ArrayView array) {
			if (array.sheet.IsFunctionSheet) {
				this.index = -1; // Illegal cell area
			}
			else {
				this.index = valueTable.GetIndex(array);
			}
		}

		public override void Compile() {
			if (index >= 0) {
				ilg.Emit(OpCodes.Ldc_I4, index);
				ilg.Emit(OpCodes.Call, getArrayViewMethod);
			}
			else {
				LoadErrorValue(ErrorValue.Make("#FUNERR: Range on function sheet"));
			}
		}

		public override CGExpr PEval(PEnv pEnv, bool hasDynamicControl) { return this; }

		public override void CompileToDoubleOrNan() { GenLoadErrorNan(ErrorValue.argTypeError); }

		// Used only (through reflection and) from generated code 
		public static ArrayView GetArrayView(int number) { return valueTable[number]; }

		public override void EvalCond(PathCond evalCond,
									  IDictionary<FullCellAddr, PathCond> evalConds,
									  List<CGCachedExpr> caches) {}

		public override void DependsOn(FullCellAddr here, Action<FullCellAddr> dependsOn) {
			// We do not track dependencies on cells or areas on ordinary sheets
		}

		public override Typ Type() { return Typ.Array; }

		public override void CountUses(Typ typ, HashBag<FullCellAddr> numberUses) { }
	}
}