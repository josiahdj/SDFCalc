using System;
using System.Collections.Generic;
using System.Linq;

using CoreCalc.Cells;

namespace Corecalc {
	/// <summary>
	/// A SheetRep represents a sheet's cell array sparsely, quadtree style, with four 
	/// levels of tiles, each conceptually a 16-column by 32-row 2D array, 
	/// for up to SIZEW = 2^16 = 64K columns and SIZEH = 2^20 = 1M rows.
	/// </summary>
	internal class SheetRep : IEnumerable<Cell> {
		// Sizes are chosen so that addresses can be calculated by bit-operations,
		// and the 2D tiles are represented by 1D arrays for speed.  The mask 
		// MW equals ...01111 with LOGW 1's, so (c&MW) equals (c%M); MH ditto.

		private const int // Could be uint, but then indexes must be cast to int
			LOGW = 4,
			W = 1 << LOGW,
			MW = W - 1,
			SIZEW = 1 << (4*LOGW),
			// cols
			LOGH = 5,
			H = 1 << LOGH,
			MH = H - 1,
			SIZEH = 1 << (4*LOGH); // rows

		private readonly Cell[][][][] tile0 = new Cell[W*H][][][];

		public Cell this[int c, int r] {
			get {
				if (c < 0 || SIZEW <= c || r < 0 || SIZEH <= r) {
					return null;
				}
				Cell[][][] tile1 = tile0[(((c >> (3*LOGW)) & MW) << LOGH) | ((r >> (3*LOGH)) & MH)];
				Cell[][] tile2 = tile1?[(((c >> (2*LOGW)) & MW) << LOGH) | ((r >> (2*LOGH)) & MH)];
				Cell[] tile3 = tile2?[(((c >> (1*LOGW)) & MW) << LOGH) | ((r >> (1*LOGH)) & MH)];
				return tile3?[((c & MW) << LOGH) | (r & MH)];
			}
			set {
				if (c < 0 || SIZEW <= c || r < 0 || SIZEH <= r) {
					return;
				}
				int index0 = (((c >> (3*LOGW)) & MW) << LOGH) | ((r >> (3*LOGH)) & MH);
				Cell[][][] tile1 = tile0[index0];
				if (tile1 == null) {
					if (value == null) {
						return;
					}
					else {
						tile1 = tile0[index0] = new Cell[W*H][][];
					}
				}
				int index1 = (((c >> (2*LOGW)) & MW) << LOGH) | ((r >> (2*LOGH)) & MH);
				Cell[][] tile2 = tile1[index1];
				if (tile2 == null) {
					if (value == null) {
						return;
					}
					else {
						tile2 = tile1[index1] = new Cell[W*H][];
					}
				}
				int index2 = (((c >> (1*LOGW)) & MW) << LOGH) | ((r >> (1*LOGH)) & MH);
				Cell[] tile3 = tile2[index2];
				if (tile3 == null) {
					if (value == null) {
						return;
					}
					else {
						tile3 = tile2[index2] = new Cell[W*H];
					}
				}
				int index3 = ((c & MW) << LOGH) | (r & MH);
				tile3[index3] = value;
			}
		}

		// Yield all the sheet's non-null cells
		public IEnumerator<Cell> GetEnumerator() {
			return (from tile1 in tile0
					where tile1 != null
					from tile2 in tile1
					where tile2 != null
					from tile3 in tile2
					where tile3 != null
					from cell in tile3
					where cell != null
					select cell).GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }

		// Sparse iteration, over non-null cells only
		public void Forall(Action<int, int, Cell> act) {
			int i0 = 0;
			foreach (Cell[][][] tile1 in tile0) {
				int i1 = 0, c0 = (i0 >> LOGH) << (3*LOGW), r0 = (i0 & MH) << (3*LOGH);
				if (tile1 != null) {
					foreach (Cell[][] tile2 in tile1) {
						int i2 = 0, c1 = (i1 >> LOGH) << (2*LOGW), r1 = (i1 & MH) << (2*LOGH);
						if (tile2 != null) {
							foreach (Cell[] tile3 in tile2) {
								int i3 = 0, c2 = (i2 >> LOGH) << (1*LOGW), r2 = (i2 & MH) << (1*LOGH);
								if (tile3 != null) {
									foreach (Cell cell in tile3) {
										if (cell != null) {
											act(c0 | c1 | c2 | i3 >> LOGH, r0 | r1 | r2 | i3 & MH, cell);
										}
										i3++;
									}
								}
								i2++;
							}
						}
						i1++;
					}
				}
				i0++;
			}
		}
	}
}