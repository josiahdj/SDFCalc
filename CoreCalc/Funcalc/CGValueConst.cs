using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Corecalc.Funcalc {
	/// <summary>
	/// A CGValueConst is a general constant-valued expression; arises
	/// from partial evaluation only.
	/// </summary>
	public class CGValueConst : CGConst {
		public readonly int index;

		private static readonly MethodInfo loadValueConstMethod
			= typeof (CGValueConst).GetMethod("LoadValueConst");

		private static readonly ValueTable<Value> valueTable
			= new ValueTable<Value>();

		public CGValueConst(Value value) { this.index = valueTable.GetIndex(value); }

		public override void Compile() {
			ilg.Emit(OpCodes.Ldc_I4, index);
			ilg.Emit(OpCodes.Call, loadValueConstMethod);
		}

		public override void CompileToDoubleOrNan() { LoadErrorNan(ErrorValue.argTypeError); }

		// Used only (through reflection and) from generated code 
		public static Value LoadValueConst(int index) { return valueTable[index]; }

		public override Value Value {
			get { return valueTable[index]; }
		}

		public override string ToString() { return String.Format("CGValueConst({0}) at {1}", valueTable[index], index); }

		public override Typ Type() { return Typ.Value; }
	}
}