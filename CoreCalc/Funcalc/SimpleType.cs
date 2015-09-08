using System;

namespace Corecalc.Funcalc {
	/// <summary>
	/// A SimpleType is simple (non-composite) type such as Double, String, StringBuilder.
	/// </summary>
	public class SimpleType : SdfType {
		public readonly Type type;

		public SimpleType(Type type) { this.type = type; }

		public override Type GetDotNetType() { return type; }

		public override string ToString() { return type.ToString(); }
	}
}