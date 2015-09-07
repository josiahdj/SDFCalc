using System.Collections.Generic;

namespace Corecalc {
	/// <summary>
	/// A RefSet is a set of CellRefs and CellAreas already seen by a VisitRefs visitor.
	/// </summary>
	internal class RefSet {
		private readonly HashSet<CellRef> cellRefsSeen = new HashSet<CellRef>();
		private readonly HashSet<CellArea> cellAreasSeen = new HashSet<CellArea>();

		public void Clear() {
			cellRefsSeen.Clear();
			cellAreasSeen.Clear();
		}

		public bool SeenBefore(CellRef cellRef) {
			return !cellRefsSeen.Add(cellRef);
		}

		public bool SeenBefore(CellArea cellArea) {
			return !cellAreasSeen.Add(cellArea);
		}
	}
}