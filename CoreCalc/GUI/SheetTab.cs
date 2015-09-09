using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using Corecalc;
using Corecalc.Funcalc;

using CoreCalc.CellAddressing;
using CoreCalc.Cells;

namespace CoreCalc.GUI {
	/// <summary>
	/// A SheetTab is a displayed sheet, shown as a tab page on the 
	/// workbook's tab control.
	/// </summary>
	public class SheetTab : TabPage {
		public readonly Sheet sheet;
		private readonly DataGridView dgv;
		private readonly WorkbookForm gui;
		private readonly int[] colOffset, rowOffset;

		public SheetTab(WorkbookForm gui, Sheet sheet)
			: base(sheet.Name) {
			this.gui = gui;
			this.sheet = sheet;
			this.Name = sheet.Name;
			this.dgv = new DataGridView();
			dgv.ShowEditingIcon = false;
			dgv.Dock = DockStyle.Fill;
			Dock = DockStyle.Fill;
			// Display formula in the current cell and computed value in other cells
			dgv.CellFormatting +=
				delegate(Object sender, DataGridViewCellFormattingEventArgs e) {
					int col = e.ColumnIndex, row = e.RowIndex;
					if (col == dgv.CurrentCellAddress.X && row == dgv.CurrentCellAddress.Y) {
						Object obj = sheet.Show(col, row);
						if (obj != null) {
							e.Value = obj;
							e.FormattingApplied = true;
						}
					}
					else {
						Object obj = sheet.ShowValue(col, row);
						if (obj != null) {
							e.Value = obj;
							e.FormattingApplied = true;
						}
					}
				};
			// Show current cell's address, and show formula in formula box
			dgv.CellEnter +=
				delegate(Object sender, DataGridViewCellEventArgs arg) {
					int row = arg.RowIndex, col = arg.ColumnIndex;
					dgv.TopLeftHeaderCell.Value = new CellAddr(col, row).ToString();
					gui.formulaBox.Text = (String)dgv.CurrentCell.FormattedValue;
				};
			// Check that cell's contents is well-formed after edit
			dgv.CellValidating +=
				delegate(Object sender, DataGridViewCellValidatingEventArgs arg) {
					if (dgv.IsCurrentCellInEditMode) { // Update only if cell was edited
						int row = arg.RowIndex, col = arg.ColumnIndex;
						Object value = arg.FormattedValue;
						if (value != null) {
							SetCell(col, row, value.ToString(), arg);
						}
					}
				};
			// Experiment with painting on the data grid view
			dgv.Paint += delegate(Object sender, PaintEventArgs arg) {
							 base.OnPaint(arg);
							 // Update column and row offset tables for drawing arrows between cells:
							 int offset = dgv.RowHeadersWidth;
							 for (int col = 0; col < sheet.Cols; col++) {
								 colOffset[col] = offset;
								 offset += dgv.Columns[col].Width;
							 }
							 colOffset[sheet.Cols] = offset;
							 offset = dgv.ColumnHeadersHeight;
							 for (int row = 0; row < sheet.Rows; row++) {
								 rowOffset[row] = offset;
								 offset += dgv.Rows[row].Height;
							 }
							 rowOffset[sheet.Rows] = offset;
							 Pen pen = new Pen(Color.Blue, 1);
							 pen.EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor;
							 pen.StartCap = System.Drawing.Drawing2D.LineCap.RoundAnchor;
							 if (dgv.SelectedCells.Count > 0) {
								 DataGridViewCell dgvc = dgv.SelectedCells[0];
								 CellAddr ca = new CellAddr(dgvc.ColumnIndex, dgvc.RowIndex);
								 Graphics g = arg.Graphics;
								 g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
								 int x = dgv.RowHeadersWidth,
									 y = dgv.ColumnHeadersHeight,
									 w = dgv.DisplayRectangle.Width - x,
									 h = dgv.DisplayRectangle.Height - y;
								 // Clip headers *before* the scroll translation/transform
								 g.Clip = new Region(new Rectangle(x, y, w, h));
								 g.Transform = new System.Drawing.Drawing2D.Matrix(1,
																0,
																0,
																1,
																-dgv.HorizontalScrollingOffset,
																-dgv.VerticalScrollingOffset);
								 SupportArea.IdempotentForeach = false; // Draw all arrows into a cell
								 DrawDependents(g, pen, gui.dependentsDepth, ca, new HashSet<CellAddr>());
								 DrawPrecedents(g, pen, gui.precedentsDepth, ca, new HashSet<CellAddr>());
							 }
						 };
			dgv.SelectionChanged += delegate(Object sender, EventArgs arg) {
										if (gui.dependentsDepth != 0 || gui.precedentsDepth != 0) {
											gui.dependentsDepth = gui.precedentsDepth = 0;
											Refresh();
										}
									};
			// Strange: to hold sheet, we need an extra row, but not an extra column?
			dgv.ColumnCount = sheet.Cols;
			dgv.RowCount = sheet.Rows + 1;
			// Allocate offset tables to assist drawing arrows between cells
			colOffset = new int[sheet.Cols + 1];
			rowOffset = new int[sheet.Rows + 1];
			dgv.AllowUserToAddRows = false;
			// Put labels on columns and rows, disable (meaningless) row sorting:
			for (int col = 0; col < dgv.ColumnCount; col++) {
				dgv.Columns[col].Name = CellAddr.ColumnName(col);
				dgv.Columns[col].SortMode = DataGridViewColumnSortMode.NotSortable;
			}
			for (int row = 0; row < dgv.RowCount; row++) {
				dgv.Rows[row].HeaderCell.Value = (row + 1).ToString();
			}
			if (sheet.IsFunctionSheet) {
				DataGridViewCellStyle cellStyle = new DataGridViewCellStyle();
				cellStyle.BackColor = Color.LightPink;
				dgv.ColumnHeadersDefaultCellStyle = dgv.RowHeadersDefaultCellStyle = cellStyle;
			}
			// Somewhat arbitrary extension of the width -- using
			// Graphics.MeasureString("0000", dgv.Font) would be better
			dgv.RowHeadersWidth += 20;
			Controls.Add(dgv);
		}

		// Painting arrows to dependents and precedents of cell ca
		private void DrawDependents(Graphics g, Pen pen, int depth, CellAddr ca, HashSet<CellAddr> done) {
			if (depth > 0 && !done.Contains(ca)) {
				done.Add(ca);
				Cell cell = sheet[ca];
				if (cell != null) {
					cell.ForEachSupported(delegate(Sheet suppSheet, int suppCol, int suppRow) {
											  CellAddr dependent = new CellAddr(suppCol, suppRow);
											  if (suppSheet == sheet) {
												  CellToCellArrow(g, pen, ca, dependent);
												  DrawDependents(g, pen, depth - 1, dependent, done);
											  }
										  });
				}
			}
		}

		private void DrawPrecedents(Graphics g, Pen pen, int depth, CellAddr ca, HashSet<CellAddr> done) {
			if (depth > 0 && !done.Contains(ca)) {
				done.Add(ca);
				Cell cell = sheet[ca];
				if (cell != null) {
					cell.ForEachReferred(sheet,
										 ca.col,
										 ca.row,
										 delegate(FullCellAddr precedent) {
											 if (precedent.sheet == sheet) {
												 CellToCellArrow(g, pen, precedent.ca, ca);
												 DrawPrecedents(g, pen, depth - 1, precedent.ca, done);
											 }
										 });
				}
			}
		}

		private void CellToCellArrow(Graphics g, Pen pen, CellAddr ca1, CellAddr ca2) {
			if (dgv[ca1.col, ca1.row].Displayed || dgv[ca2.col, ca2.row].Displayed) {
				// Sadly, dgv.GetCellDisplayRectangle returns Empty outside visible area
				int x1 = (colOffset[ca1.col] + colOffset[ca1.col + 1])/2,
					y1 = (rowOffset[ca1.row] + rowOffset[ca1.row + 1])/2,
					x2 = (colOffset[ca2.col] + colOffset[ca2.col + 1])/2,
					y2 = (rowOffset[ca2.row] + rowOffset[ca2.row + 1])/2;
				g.DrawLine(pen, x1, y1, x2, y2);
			}
		}

		// Attempt to parse s as cell contents, and set selected cell(s)
		
		private void SetCell(int col, int row, String text, DataGridViewCellValidatingEventArgs arg = null) {
			Cell cell = Cell.Parse(text, sheet.workbook, col, row);
			ArrayFormula oldArrayFormula = sheet[col, row] as ArrayFormula;
			gui.SetCyclicError(null);
			if (cell == null) {
				if (text.TrimStart(' ').StartsWith("=")) { // Ill-formed formula, cancel edit
					if (arg != null) {
						ErrorMessage("Bad formula");
						arg.Cancel = true;
					}
					return;
				}
				else if (!String.IsNullOrWhiteSpace(text)) // Assume a quote expression
				{
					cell = Cell.Parse("'" + text, sheet.workbook, col, row);
				}
			}
			DataGridViewSelectedCellCollection dgvscc = dgv.SelectedCells;
			if (dgvscc.Count > 1 && cell is Formula) { // Array formula
				int ulCol = col, ulRow = row, lrCol = col, lrRow = row;
				foreach (DataGridViewCell dgvc in dgvscc) {
					ulCol = Math.Min(ulCol, dgvc.ColumnIndex);
					ulRow = Math.Min(ulRow, dgvc.RowIndex);
					lrCol = Math.Max(lrCol, dgvc.ColumnIndex);
					lrRow = Math.Max(lrRow, dgvc.RowIndex);
				}
				CellAddr ulCa = new CellAddr(ulCol, ulRow),
						 lrCa = new CellAddr(lrCol, lrRow);
				if (oldArrayFormula != null && arg != null
					&& !(oldArrayFormula.caf.ulCa.Equals(ulCa)
						 && oldArrayFormula.caf.lrCa.Equals(lrCa))) {
					ErrorMessage("Cannot edit part of array formula");
					arg.Cancel = true;
					return;
				}
				sheet.SetArrayFormula(cell, col, row, ulCa, lrCa);
			}
			else { // One-cell formula, or constant, or null (parse error)
				if (oldArrayFormula != null && arg != null) {
					ErrorMessage("Cannot edit part of array formula");
					arg.Cancel = true;
					return;
				}
				sheet.SetCell(cell, col, row);
			}
			RecalculateAndShow();
			gui.SetCyclicError("Cyclic dependency");
		}

		private void ErrorMessage(String msg) { MessageBox.Show(msg, msg, MessageBoxButtons.OK, MessageBoxIcon.Error); }

		// Copy sheet's currently active cell to Clipboard, also in text format

		public void Copy() {
			CellAddr ca = new CellAddr(dgv.CurrentCellAddress);
			DataObject data = new DataObject();
			data.SetData(DataFormats.Text, sheet[ca].Show(ca.col, ca.row, sheet.workbook.format));
			data.SetData(ClipboardCell.COPIED_CELL, new ClipboardCell(sheet.Name, ca));
			Clipboard.Clear();
			Clipboard.SetDataObject(data, false);
		}

		// Paste from the Clipboard.  Need to distinguish:
		// 1. Paste from Corecalc formula Copy: preserve sharing
		// 2. Paste from Corecalc cell Cut: adjust referring cells and this, if formula
		// 3. Paste from text (e.g. from Excel), parse to new cell

		public void Paste() {
			if (Clipboard.ContainsData(ClipboardCell.COPIED_CELL)) {
				// Copy Corecalc cell
				ClipboardCell cc = (ClipboardCell)Clipboard.GetData(ClipboardCell.COPIED_CELL);
				Console.WriteLine("Pasting copied cell " + Clipboard.GetText());
				Cell cell = sheet.workbook[cc.FromSheet][cc.FromCellAddr];
				int col, row, cols, rows;
				GetSelectedColsRows(out col, out row, out cols, out rows);
				sheet.PasteCell(cell, col, row, cols, rows);
				RecalculateAndShow();
			}
			else if (Clipboard.ContainsData(ClipboardCell.CUT_CELL)) {
				// Move Corecalc cell
				Console.WriteLine("Pasting moved cell not implemented.");
			}
			else if (Clipboard.ContainsText()) {
				// Insert text (from external source)
				CellAddr ca = new CellAddr(dgv.CurrentCellAddress);
				String text = Clipboard.GetText();
				Console.WriteLine("Pasting text " + text);
				SetCell(ca.col, ca.row, text);
			}
		}

		private void GetSelectedColsRows(out int col, out int row, out int cols, out int rows) {
			// Apparently SelectedColumns and SelectedRows only concern selection 
			// of entire columns and rows, so we need to wade through all selected cells.
			int ulCol = int.MaxValue,
				ulRow = int.MaxValue,
				lrCol = int.MinValue,
				lrRow = int.MinValue;
			foreach (DataGridViewCell dgvc in dgv.SelectedCells) {
				ulCol = Math.Min(ulCol, dgvc.ColumnIndex);
				ulRow = Math.Min(ulRow, dgvc.RowIndex);
				lrCol = Math.Max(lrCol, dgvc.ColumnIndex);
				lrRow = Math.Max(lrRow, dgvc.RowIndex);
			}
			col = ulCol;
			cols = lrCol - ulCol + 1;
			row = ulRow;
			rows = lrRow - ulRow + 1;
		}

		public void Delete() {
			CellAddr ca = new CellAddr(dgv.CurrentCellAddress);
			sheet.SetCell(null, ca.col, ca.row);
			RecalculateAndShow();
		}

		public void InsertRows(int N) {
			sheet.InsertRowCols(dgv.CurrentCellAddress.Y, N, doRows: true);
			RecalculateAndShow();
		}

		public void InsertColumns(int N) {
			sheet.InsertRowCols(dgv.CurrentCellAddress.X, N, doRows: false);
			RecalculateAndShow();
		}

		public void RecalculateAndShow() {
			long elapsed = sheet.workbook.Recalculate();
			Reshow();
			gui.SetStatusLine(elapsed);
		}

		public void Reshow() {
			sheet.ShowAll(delegate(int c, int r, String value) { dgv[c, r].Value = value; });
			if (FunctionSheet) {
				foreach (FullCellAddr fca in SdfManager.cellToFunctionMapper.addressToFunctionList.Keys) {
					if (fca.sheet == sheet) {
						ChangeCellBackgroundColor(fca, Color.LightCyan);
					}
				}
				foreach (FullCellAddr fca in SdfManager.cellToFunctionMapper.inputCellBag) {
					if (fca.sheet == sheet) {
						ChangeCellBackgroundColor(fca, Color.LightGreen);
					}
				}
				foreach (FullCellAddr fca in SdfManager.cellToFunctionMapper.outputCellBag) {
					if (fca.sheet == sheet) {
						ChangeCellBackgroundColor(fca, Color.LightSkyBlue);
					}
				}
			}
			gui.formulaBox.Text = (String)dgv.CurrentCell.FormattedValue;
			dgv.Focus();
		}

		public void ChangeCurrentText(String text) { dgv.CurrentCell.Value = text; }

		public void SetCurrentCell(String text) {
			CellAddr ca = new CellAddr(dgv.CurrentCellAddress);
			SetCell(ca.col, ca.row, text);
		}

		public void ChangeCellBackgroundColor(FullCellAddr fca, Color c) { dgv[fca.ca.col, fca.ca.row].Style.BackColor = c; }

		public void SetCellErrorText(CellAddr ca, String message) { dgv[ca.col, ca.row].ErrorText = message; }

		public void ScrollTo(FullCellAddr fca) { dgv.FirstDisplayedScrollingRowIndex = Math.Max(0, fca.ca.row - 1); }

		public string SheetName {
			get { return sheet.Name; }
		}

		public bool FunctionSheet {
			get { return sheet.IsFunctionSheet; }
			set { sheet.IsFunctionSheet = value; }
		}
	}
}