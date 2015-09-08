namespace Corecalc.Funcalc {
	/// <summary>
	/// A Signature describes the return type and argument types of a built-in function.
	/// </summary>
	public class Signature {
		public readonly Typ retType;
		public readonly Typ[] argTypes; // null means variadic

		public Signature(Typ retType, params Typ[] argTypes) {
			this.retType = retType;
			this.argTypes = argTypes;
		}

		public int Arity {
			get { return argTypes != null ? argTypes.Length : -1; }
		}
	}
}