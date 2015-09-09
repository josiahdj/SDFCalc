using System;
using System.Diagnostics;
using System.Windows.Forms;

using Corecalc;

namespace CoreCalc.IO {
	public class DbIOFormat {
		private const int MINROWS = 10, MINCOLS = 10; // Must be positive


		public Workbook Read() {
			Workbook wb = null;
			Stopwatch watch = new Stopwatch();
			watch.Reset();
			watch.Start();
			try {
				wb = makeEmptySheet();
				if (wb != null) {
					loadWorkbook();
				}
			}
			catch (Exception exn) {
				string msg = "Cannot read workbook";
				MessageBox.Show(msg + "\n" + exn, msg, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			finally {
			}
			watch.Stop();
			Console.WriteLine("in {0} ms", watch.ElapsedMilliseconds);
			return wb;
		}

		private Workbook makeEmptySheet() {
			var wb = new Workbook();
			int cols = MINCOLS, rows = MINROWS; // In the sheet
			rows = Math.Max(rows, MINROWS);
			cols = Math.Max(cols, MINCOLS);
			new Sheet(wb, "Sheet 1", cols, rows, false);
			return wb;
		}

		private static void loadWorkbook() { throw new NotImplementedException(); }
	}
}