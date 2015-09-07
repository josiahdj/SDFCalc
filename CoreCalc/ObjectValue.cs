using System;

namespace Corecalc {
	/// <summary>
	/// An ObjectValue holds a .NET object, typically resulting from calling
	/// an external function; may be null.
	/// </summary>
	public class ObjectValue : Value {
		public readonly Object value;
		public static readonly ObjectValue nullObjectValue = new ObjectValue(null);

		public ObjectValue(Object value) {
			this.value = value;
		}

		// Used from EXTERN and from generated bytecode
		public static Value Make(Object o) {
			if (o == null)
				return nullObjectValue;
			else
				return new ObjectValue(o);
		}

		public override bool Equals(Value v) {
			return v is ObjectValue && (v as ObjectValue).value.Equals(value);
		}

		public override Object ToObject() {
			return value;
		}

		public override String ToString() {
			return value == null ? "null" : value.ToString();
		}
	}
}