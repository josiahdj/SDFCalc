using System;
using System.Text;

namespace Corecalc.Funcalc {
	/// <summary>
	/// A FunctionType is the type of a sheet-defined function, 
	/// such as Number * Text -> Value.
	/// </summary>
	public class FunctionType : SdfType {
		public readonly SdfType[] arguments;
		public readonly SdfType returntype;

		public FunctionType(SdfType[] arguments, SdfType returntype) {
			this.arguments = arguments;
			this.returntype = returntype;
		}

		public Type[] ArgumentDotNetTypes() {
			Type[] res = new Type[arguments.Length];
			for (int i = 0; i < arguments.Length; i++) {
				res[i] = arguments[i].GetDotNetType();
			}
			return res;
		}

		public override Type GetDotNetType() { throw new Exception("Function type not allowed"); }

		public override String ToString() {
			StringBuilder sb = new StringBuilder();
			sb.Append("(");
			if (arguments.Length > 0) {
				sb.Append(arguments[0]);
			}
			for (int i = 1; i < arguments.Length; i++) {
				sb.Append(" * ").Append(arguments[i]);
			}
			sb.Append(" -> ").Append(returntype).Append(")");
			return sb.ToString();
		}
	}
}