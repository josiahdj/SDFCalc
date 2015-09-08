using System;
using System.Diagnostics;

namespace Corecalc.Funcalc {
	/// <summary>
	/// An UnwrapInputCell represents the action, in a program list, to 
	/// unwrap an input cell inputVar of type Value to a numberVar of type Number.
	/// </summary>
	public class UnwrapInputCell : CodeGenerate {
		public readonly Variable inputVar, numberVar;

		public UnwrapInputCell(Variable inputVar, Variable numberVar) {
			this.inputVar = inputVar;
			this.numberVar = numberVar;
		}

		public void Compile() {
			Debug.Assert(inputVar.Type == Typ.Value);
			Debug.Assert(numberVar.Type == Typ.Number);
			inputVar.EmitLoad(ilg);
			UnwrapToDoubleOrNan();
			numberVar.EmitStore(ilg);
		}

		public override string ToString() { return String.Format("{0} = UnwrapToDoubleOrNan({1})", numberVar.Name, inputVar.Name); }
	}
}