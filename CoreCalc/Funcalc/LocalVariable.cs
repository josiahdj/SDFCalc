using System;
using System.Reflection.Emit;

namespace Corecalc.Funcalc {
	/// <summary>
	/// A LocalVariable represents a .NET IL variable holding a computed
	/// (intermediate or output) cell of a sheet-defined function.
	/// </summary>
	public class LocalVariable : Variable {
		private LocalBuilder localBuilder; // null until var emitted

		public LocalVariable(String name, Typ type)
			: base(name, type) { }

		private LocalBuilder GetLocalBuilder(ILGenerator ilg) {
			if (localBuilder == null) {
				if (Type == Typ.Number) {
					this.localBuilder = ilg.DeclareLocal(typeof (double));
				}
				else {
					this.localBuilder = ilg.DeclareLocal(Value.type);
				}
			}
			return localBuilder;
		}

		public override void EmitLoad(ILGenerator ilg) { ilg.Emit(OpCodes.Ldloc, GetLocalBuilder(ilg)); }

		public override void EmitStore(ILGenerator ilg) { ilg.Emit(OpCodes.Stloc, GetLocalBuilder(ilg)); }

		public override Variable Fresh() { return new LocalVariable(Name, Type); }
	}
}