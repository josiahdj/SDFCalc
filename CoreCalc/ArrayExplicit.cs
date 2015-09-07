namespace Corecalc {
	/// <summary>
	/// An ArrayExplicit is a view of a materialized array of Values, typically
	/// resulting from evaluating TRANSPOSE or other array-valued functions.
	/// </summary>
	public class ArrayExplicit : ArrayValue {
		public readonly CellAddr ulCa, lrCa;      // ulCa to the left and above lrCa
		public readonly Value[,] values;          // non-null
		private readonly int cols, rows;

		public ArrayExplicit(Value[,] values)
			: this(new CellAddr(0, 0), new CellAddr(values.GetLength(0) - 1, values.GetLength(1) - 1), values) { }

		public ArrayExplicit(CellAddr ulCa, CellAddr lrCa, Value[,] values) {
			this.ulCa = ulCa;
			this.lrCa = lrCa;
			this.values = values;
			this.cols = lrCa.col - ulCa.col + 1;
			this.rows = lrCa.row - ulCa.row + 1;
		}

		public override Value View(CellAddr ulCa, CellAddr lrCa) {
			return new ArrayExplicit(ulCa.Offset(this.ulCa), lrCa.Offset(this.ulCa), values);
		}

		public override Value Slice(CellAddr ulCa, CellAddr lrCa) {
			return View(ulCa, lrCa);
		}

		public override int Cols { get { return cols; } }

		public override int Rows { get { return rows; } }

		// Evaluate and get value at offset [col, row], 0-based
		public override Value this[int col, int row] {
			get {
				if (0 <= col && col < Cols && 0 <= row && row < Rows) {
					int c = ulCa.col + col, r = ulCa.row + row;
					return values[c, r];
				} else
					return ErrorValue.naError;
			}
		}

		public override bool Equals(Value v) {
			return EqualElements(this, v as ArrayValue);
		}
	}
}