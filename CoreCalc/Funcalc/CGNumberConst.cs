using System;
using System.Reflection.Emit;

using CoreCalc.Values;

namespace Corecalc.Funcalc {
	/// <summary>
	/// A CGNumberConst is a constant number-valued expression.
	/// </summary>
	public class CGNumberConst : CGConst {
		public readonly NumberValue number;

		public CGNumberConst(NumberValue number) { this.number = number; }

		public override void Compile() {
			// sestoft: Would be better to load the NumberValue from this instance, 
			// but then it would need to be stored somewhere (e.g. in a global Value
			// array from which it can be loaded).  See notes.txt.
			if (number.value == 0.0) {
				ilg.Emit(OpCodes.Ldsfld, NumberValue.zeroField);
			}
			else if (number.value == 1.0) {
				ilg.Emit(OpCodes.Ldsfld, NumberValue.oneField);
			}
			else if (number.value == Math.PI) {
				ilg.Emit(OpCodes.Ldsfld, NumberValue.piField);
			}
			else {
				ilg.Emit(OpCodes.Ldc_R8, number.value);
				WrapDoubleToNumberValue();
			}
		}

		public override void CompileToDoubleOrNan() { ilg.Emit(OpCodes.Ldc_R8, number.value); }

		public override void CompileToDoubleProper(Gen ifProper, Gen ifOther) {
			if (double.IsInfinity(number.value) || double.IsNaN(number.value)) {
				ilg.Emit(OpCodes.Ldc_R8, number.value);
				ilg.Emit(OpCodes.Stloc, testDouble);
				ifOther.Generate(ilg);
			}
			else {
				ilg.Emit(OpCodes.Ldc_R8, number.value);
				ifProper.Generate(ilg);
			}
		}

		public override void CompileCondition(Gen ifTrue, Gen ifFalse, Gen ifOther) {
			if (Double.IsInfinity(number.value) || Double.IsNaN(number.value)) {
				ilg.Emit(OpCodes.Ldc_R8, number.value);
				ilg.Emit(OpCodes.Stloc, testDouble);
				ifOther.Generate(ilg);
			}
			else if (number.value != 0) {
				ifTrue.Generate(ilg);
			}
			else {
				ifFalse.Generate(ilg);
			}
		}

		public override Value Value {
			get { return number; }
		}

		public override string ToString() { return number.value.ToString(); }

		public override Typ Type() { return Typ.Number; }
	}
}