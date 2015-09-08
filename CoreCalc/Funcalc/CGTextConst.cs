using System.Reflection.Emit;

using CoreCalc.Values;

namespace Corecalc.Funcalc {
	/// <summary>
	/// A CGTextConst is a constant text-valued expression.
	/// </summary>
	public class CGTextConst : CGConst {
		public readonly TextValue value;
		private readonly int index;

		public CGTextConst(TextValue value) {
			this.value = value;
			this.index = TextValue.GetIndex(value.value);
		}

		public override void Compile() {
			if (value.value == "") {
				ilg.Emit(OpCodes.Ldsfld, TextValue.emptyField);
			}
			else {
				ilg.Emit(OpCodes.Ldc_I4, index);
				ilg.Emit(OpCodes.Call, TextValue.fromIndexMethod);
			}
		}

		public override void CompileToDoubleOrNan() { LoadErrorNan(ErrorValue.argTypeError); }

		public override void CompileToDoubleProper(Gen ifProper, Gen ifOther) { ifOther.Generate(ilg); }

		// In Excel, a text used in a conditional produces the error #VALUE! -- mostly
		public override void CompileCondition(Gen ifTrue, Gen ifFalse, Gen ifOther) { ifOther.Generate(ilg); }

		public override Value Value {
			get { return value; }
		}

		public override Typ Type() { return Typ.Text; }
	}
}