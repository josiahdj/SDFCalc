using System;
using System.Reflection.Emit;

namespace Corecalc.Funcalc {
	/// <summary>
	/// A LocalArgument represents a .NET IL function argument holding
	/// an input of a sheet-defined function.
	/// </summary>
	public class LocalArgument : Variable {
		private readonly short argumentNumber;

		public LocalArgument(String name, Typ type, short argumentNumber)
			: base(name, type) { this.argumentNumber = argumentNumber; }

		public override void EmitLoad(ILGenerator ilg) { ilg.Emit(OpCodes.Ldarg, argumentNumber); }

		public override void EmitStore(ILGenerator ilg) { throw new ImpossibleException("LocalArgument.EmitStore unexpected"); }

		public override Variable Fresh() { throw new ImpossibleException("LocalArgument.Fresh()"); }
	}
}