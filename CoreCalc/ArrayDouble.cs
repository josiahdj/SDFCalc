using System;
using System.Reflection;

namespace Corecalc {
	/// <summary>
	/// An ArrayMatrix is a rectangular array of non-wrapped doubles, 
	/// typically the result of evaluating a linear algebra operation, 
	/// or of unboxing all elements of an ArrayValue.
	/// </summary>
	public class ArrayDouble : ArrayValue {
		// Note: rows and columns are swapped relative to ArrayExplicit,
		// so indexing should go matrix[row, col] -- as in .NET 2D arrays
		public readonly double[,] matrix;

		public new static readonly Type type = typeof(ArrayDouble);
		public static readonly MethodInfo
			makeMethod = type.GetMethod("Make");

		public ArrayDouble(int cols, int rows) {
			this.matrix = new double[rows, cols];
		}

		public ArrayDouble(double[,] matrix) {
			this.matrix = matrix;
		}

		public override int Cols {
			get { return matrix.GetLength(1); }
		}

		public override int Rows {
			get { return matrix.GetLength(0); }
		}

		public override Value this[int col, int row] {
			get { return NumberValue.Make(matrix[row, col]); }
		}

		public override Value View(CellAddr ulCa, CellAddr lrCa) {
			int cols = Cols, rows = Rows, col0 = ulCa.col, row0 = ulCa.row;
			Value[,] vals = new Value[cols, rows];
			for (int c = 0; c < cols; c++)
				for (int r = 0; r < rows; r++)
					vals[c, r] = NumberValue.Make(matrix[row0 + r, col0 + c]);
			return new ArrayExplicit(vals);
		}

		public override Value Slice(CellAddr ulCa, CellAddr lrCa) {
			return View(ulCa, lrCa);
		}

		// Fast, but external array modification could undermine semantics 
		public override double[,] ToDoubleArray2DFast() {
			return matrix;
		}

		public static Value Make(Value v) {
			if (v is ArrayDouble)
				return v;
			else if (v is ArrayValue) {
				ArrayValue arr = v as ArrayValue;
				int cols = arr.Cols, rows = arr.Rows;
				ArrayDouble result = new ArrayDouble(cols, rows);
				for (int r = 0; r < rows; r++)
					for (int c = 0; c < cols; c++)
						result.matrix[r, c] = Value.ToDoubleOrNan(arr[c, r]);
				return result;
			} else
				return ErrorValue.argTypeError;
		}

		public override bool Equals(Value v) {
			return EqualElements(this, v as ArrayValue);
		}
	}
}