namespace CoreCalc.CellAddressing {
	/// <summary>
	/// An Adjusted<T> represents an adjusted expression or 
	/// a relative/absolute ref, for use in method InsertRowCols.
	/// </summary>
	/// <typeparam name="T">The type of adjusted entity: Expr or RaRef.</typeparam>
	public struct Adjusted<T> {
		public readonly T e; // The adjusted Expr or RaRef
		public readonly int upper; // ... invalid for rows >= upper
		public readonly bool same; // Adjusted is identical to original

		public Adjusted(T e, int upper, bool same) {
			this.e = e;
			this.upper = upper;
			this.same = same;
		}

		public Adjusted(T e) : this(e, int.MaxValue, true) { }
	}
}