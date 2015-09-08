using System;

using CoreCalc.CellAddressing;

namespace Corecalc {
	/// <summary>
	/// A ClipboardCell is a copied cell and its cell address for holding 
	/// in the MS Windows clipboard.
	/// </summary>
	[Serializable]
	public class ClipboardCell {
		public const String COPIED_CELL = "CopiedCell";
		public const String CUT_CELL = "CutCell";
		public readonly String FromSheet;
		public readonly CellAddr FromCellAddr;

		public ClipboardCell(String fromSheet, CellAddr fromCellAddr) {
			this.FromSheet = fromSheet;
			this.FromCellAddr = fromCellAddr;
		}
	}
}