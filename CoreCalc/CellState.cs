namespace Corecalc {
	/// <summary>
	/// The recalculation state of a Formula cell.
	/// </summary>
	public enum CellState {
		Dirty,
		Enqueued,
		Computing,
		Uptodate
	}
}