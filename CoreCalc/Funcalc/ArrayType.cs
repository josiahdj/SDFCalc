using System;

namespace Corecalc.Funcalc {
	/// <summary>
	/// An ArrayType is a CLI array type such as String[] or String[,] 
	/// or a spreadsheet array type such as Number array.
	/// </summary>
	public class ArrayType : SdfType {
		public readonly SdfType elementtype;
		public readonly int dim;

		public ArrayType(SdfType elementtype, int dim) {
			this.elementtype = elementtype;
			this.dim = dim;
		}

		public override Type GetDotNetType() {
			if (dim == 1) // Special case, see System.Type.MakeArrayType(int)
			{
				return elementtype.GetDotNetType().MakeArrayType();
			}
			else {
				return elementtype.GetDotNetType().MakeArrayType(dim);
			}
		}

		public override String ToString() { return elementtype + (dim == 1 ? "[]" : dim == 2 ? "[,]" : "UNHANDLED"); }
	}
}