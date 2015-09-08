using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using CoreCalc.CellAddressing;
using CoreCalc.Types;
using CoreCalc.Values;

namespace Corecalc.Funcalc {
	/// <summary>
	/// A CGNormalCellRef is a reference to a cell in an ordinary sheet, not 
	/// a function sheet; must be evaluated each time the function is called.
	/// </summary>
	public class CGNormalCellRef : CGExpr {
		private readonly int index; // If negative, cell ref is illegal

		private static readonly MethodInfo getAddressMethod
			= typeof (CGNormalCellRef).GetMethod("GetAddress");

		private static readonly ValueTable<FullCellAddr> valueTable
			= new ValueTable<FullCellAddr>();

		public CGNormalCellRef(FullCellAddr cellAddr) {
			if (cellAddr.sheet.IsFunctionSheet) {
				this.index = -1; // Illegal cell reference
			}
			else {
				this.index = valueTable.GetIndex(cellAddr);
			}
		}

		public override void Compile() {
			if (index >= 0) {
				ilg.Emit(OpCodes.Ldc_I4, index);
				ilg.Emit(OpCodes.Call, getAddressMethod);
				// HERE
				ilg.Emit(OpCodes.Stloc, tmpFullCellAddr);
				ilg.Emit(OpCodes.Ldloca, tmpFullCellAddr);
				ilg.Emit(OpCodes.Call, FullCellAddr.evalMethod);
			}
			else {
				LoadErrorValue(ErrorValue.Make("#FUNERR: Ref to other function sheet"));
			}
		}

		public override CGExpr PEval(PEnv pEnv, bool hasDynamicControl) { return this; }

		public static FullCellAddr GetAddress(int number) { return valueTable[number]; }

		public override void CountUses(Typ typ, HashBag<FullCellAddr> numberUses) { }

		public override void EvalCond(PathCond evalCond,
									  IDictionary<FullCellAddr, PathCond> evalConds,
									  List<CGCachedExpr> caches) {}

		public override void DependsOn(FullCellAddr here, Action<FullCellAddr> dependsOn) {
			// We do not track dependencies on cells on ordinary sheets
		}

		public override Typ Type() {
			// We don't infer cell types in ordinary sheets, so assume the worst
			return Typ.Value;
		}

		public override void CompileToDoubleOrNan() {
			Compile();
			UnwrapToDoubleOrNan();
		}
	}
}