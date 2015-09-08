// Corecalc, a spreadsheet core implementation 
// ----------------------------------------------------------------------
// Copyright (c) 2006-2014 Peter Sestoft and others

// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use, copy,
// modify, merge, publish, distribute, sublicense, and/or sell copies
// of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

//  * The above copyright notice and this permission notice shall be
//    included in all copies or substantial portions of the Software.

//  * The software is provided "as is", without warranty of any kind,
//    express or implied, including but not limited to the warranties of
//    merchantability, fitness for a particular purpose and
//    noninfringement.  In no event shall the authors or copyright
//    holders be liable for any claim, damages or other liability,
//    whether in an action of contract, tort or otherwise, arising from,
//    out of or in connection with the software or the use or other
//    dealings in the software.
// ----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing; // Size
using SDD2D = System.Drawing.Drawing2D; // Pen etc
using System.Windows.Forms;

using Corecalc.GUI;
using Corecalc.IO;
using Corecalc.Funcalc; // SdfManager

namespace Corecalc {
	/// <summary>
	/// A WorkbookForm is the user interface of an open workbook. 
	/// </summary>
	public partial class WorkbookForm : Form {
		public Workbook Workbook { get; private set; }
		private Benchmarks.Benchmarks test;
		public int precedentsDepth, dependentsDepth;

		public WorkbookForm(Workbook workbook, bool display) {
			SetWorkbook(workbook);
			InitializeComponent();
			StartPosition = FormStartPosition.CenterScreen;
			Size = new Size(900, 600);
			SetStatusLine(null);
			sheetHolder.Selected += sheetHolderSelected;
			DisplayWorkbook();
			if (display) {
				ShowDialog();
			}
		}

		private void sheetHolderSelected(Object sender, TabControlEventArgs e) {
			if (SelectedSheet != null) {
				precedentsDepth = dependentsDepth = 0;
				SelectedSheet.Reshow();
			}
		}

		private void SetWorkbook(Workbook newWorkbook) {
			if (this.Workbook != null) {
				this.Workbook.Clear();
				this.Workbook.OnFunctionsAltered -= workbook_OnFunctionsAltered;
			}
			this.Workbook = newWorkbook;
			this.Workbook.OnFunctionsAltered += workbook_OnFunctionsAltered;
		}

		private void workbook_OnFunctionsAltered(String[] functions) {
			Console.WriteLine("Regenerating modified functions");
			SdfManager.Regenerate(functions);
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e) { System.Environment.Exit(0); }

		private void aboutToolStripMenuItem1_Click(object sender, EventArgs e) {
			Form aboutBox = new AboutBox();
			aboutBox.ShowDialog();
		}

		private void a1ToolStripMenuItem_Click(object sender, EventArgs e) {
			Workbook.format.RefFmt = Formats.RefType.A1;
			Reshow(null);
		}

		private void c0R0ToolStripMenuItem_Click(object sender, EventArgs e) {
			Workbook.format.RefFmt = Formats.RefType.C0R0;
			Reshow(null);
		}

		private void r1C1ToolStripMenuItem_Click(object sender, EventArgs e) {
			Workbook.format.RefFmt = Formats.RefType.R1C1;
			Reshow(null);
		}

		// Add new sheet
		private void newWorkbookToolStripMenuItem_Click(object sender, EventArgs e) { InsertSheet(functionSheet: false); }

		public void InsertSheet(bool functionSheet) {
			String name = "Sheet" + (Workbook.SheetCount + 1);
			InsertSheet(new SheetTab(this, new Sheet(Workbook, name, functionSheet)));
		}

		private void InsertSheet(SheetTab sheetTab) {
			if (Workbook != null) {
				sheetHolder.TabPages.Add(sheetTab);
				sheetHolder.SelectTab(sheetTab);
			}
		}

		// Recalculate and reshow workbook

		private void Recalculate() {
			if (Workbook != null) {
				long elapsed = Workbook.Recalculate();
				Reshow(elapsed);
			}
		}

		private void RecalculateFull() {
			if (Workbook != null) {
				long elapsed = Workbook.RecalculateFull();
				Reshow(elapsed);
			}
		}

		private void RecalculateFullRebuild() {
			if (Workbook != null) {
				long elapsed = Workbook.RecalculateFullRebuild();
				Reshow(elapsed);
			}
		}

		private void recalculateMenuItem_Click(object sender, EventArgs e) { Recalculate(); }

		private void recalculateFullMenuItem_Click(object sender, EventArgs e) { RecalculateFull(); }

		private void recalculateFullRebuildMenuItem_Click(object sender, EventArgs e) { RecalculateFullRebuild(); }

		private void Reshow(long? elapsed) {
			if (SelectedSheet != null) {
				SelectedSheet.Reshow();
			}
			SetStatusLine(elapsed);
		}

		public void SetStatusLine(long? elapsed) {
			double memory = System.GC.GetTotalMemory(false)/1E6; // In MB
			statusLine.Text = String.Format("{0,-4}  {1,8:F2} MB  {2,8:D}",
											Workbook.format.RefFmt,
											memory,
											Workbook.RecalcCount);
			if (elapsed.HasValue) {
				statusLine.Text += String.Format("  {0,8:D} ms", elapsed.Value);
			}
			if (Workbook.Cyclic != null) {
				statusLine.Text += "  " + Workbook.Cyclic.Message;
			}
		}

		// Set or remove (message=null) cell error mark
		public void SetCyclicError(String message) {
			if (Workbook.Cyclic != null) {
				FullCellAddr culprit = Workbook.Cyclic.culprit;
				sheetHolder.SelectTab(culprit.sheet.Name);
				SelectedSheet.SetCellErrorText(culprit.ca, message);
			}
		}

		// Copy cell
		private void copyToolStripMenuItem_Click(object sender, EventArgs e) {
			if (SelectedSheet != null) {
				SelectedSheet.Copy();
			}
		}

		// Delete cell
		private void deleteToolStripMenuItem_Click(object sender, EventArgs e) {
			if (SelectedSheet != null) {
				SelectedSheet.Delete();
			}
		}

		// Insert column
		private void columnToolStripMenuItem_Click(object sender, EventArgs e) {
			if (SelectedSheet != null) {
				SelectedSheet.InsertColumns(1);
			}
		}

		// Insert row
		private void rowToolStripMenuItem_Click(object sender, EventArgs e) {
			if (SelectedSheet != null) {
				SelectedSheet.InsertRows(1);
			}
		}

		// Paste copied or cut cells, or text
		private void pasteToolStripMenuItem_Click(object sender, EventArgs e) {
			if (SelectedSheet != null) {
				SelectedSheet.Paste();
			}
		}

		// Get selected sheet if any, else null
		private SheetTab SelectedSheet {
			get { return sheetHolder.TabCount > 0 ? sheetHolder.SelectedTab as SheetTab : null; }
		}

		public Benchmarks.Benchmarks Test {
			get {
				if (test == null) {
					test = new Benchmarks.Benchmarks(true);
				}
				return test;
			}
		}

		private void formulaBox_TextChanged(object sender, EventArgs e) {
			if (SelectedSheet != null) {
				SelectedSheet.ChangeCurrentText(formulaBox.Text);
			}
		}

		private void formulaBox_KeyPress(object sender, KeyPressEventArgs e) {
			if (SelectedSheet != null) {
				if (e.KeyChar == (char)Keys.Return) {
					SelectedSheet.SetCurrentCell(formulaBox.Text);
					e.Handled = true;
				}
				else if (e.KeyChar == (char)Keys.Escape) {
					SelectedSheet.Focus();
					e.Handled = true;
				}
			}
		}

		private void importSheetToolStripMenuItem_Click(object sender, EventArgs e) {
			OpenFileDialog ofd = new OpenFileDialog();
			WorkBookIO workbookio = new WorkBookIO();
			ofd.Filter = workbookio.SupportedFormatFilter();
			ofd.FilterIndex = workbookio.DefaultFormatIndex();
			if (ofd.ShowDialog() == DialogResult.OK) {
				Clear();
				Workbook wb = workbookio.Read(ofd.FileName);
				if (wb != null) {
					SetWorkbook(wb);
					DisplayWorkbook();
				}
			}
		}

		private void DisplayWorkbook() {
			if (Workbook != null) {
				foreach (Sheet sheet in Workbook) {
					sheetHolder.TabPages.Add(new SheetTab(this, sheet));
				}
				RecalculateFull();
				if (sheetHolder.TabCount > 0) {
					sheetHolder.SelectTab(0);
				}
			}
		}

		public void Clear() { sheetHolder.TabPages.Clear(); }

		private void benchmarkStandardRecalculation_Click(object sender, EventArgs e) {
			int runs = 0;
			if (int.TryParse(numberOfRunsTextBox.Text, out runs)) {
				Test.BenchmarkRecalculation(this, runs);
			}
		}

		private void fullRecalculationMenuItem_Click(object sender, EventArgs e) {
			int runs = 0;
			if (int.TryParse(numberOfRunsTextBox.Text, out runs)) {
				Test.BenchmarkRecalculationFull(this, runs);
			}
		}

		private void recalculationFullRebuildMenuItem_Click(object sender, EventArgs e) {
			int runs = 0;
			if (int.TryParse(numberOfRunsTextBox.Text, out runs)) {
				Test.BenchmarkRecalculationFullRebuild(this, runs);
			}
		}

		private void newFunctionSheetMenuItem_Click(object sender, EventArgs e) { this.InsertSheet(functionSheet: true); }

		private void sheetHolder_DrawItem(object sender, DrawItemEventArgs e) {
			int currentIndex = e.Index;
			Graphics g = e.Graphics;
			TabControl tc = (TabControl)sender;
			TabPage tp = tc.TabPages[currentIndex];

			Sheet currentSheet = Workbook[currentIndex];
			Brush theBrush = new SolidBrush(Color.Black);
			StringFormat sf = new StringFormat();
			sf.Alignment = StringAlignment.Center;
			Color col = currentSheet.IsFunctionSheet ? Color.LightPink : tp.BackColor;
			g.FillRectangle(new SolidBrush(col), e.Bounds);
			g.DrawString(currentSheet.Name, tc.Font, theBrush, e.Bounds, sf);
		}

		private void sdfMenuItem_Click(object sender, EventArgs e) {
			Form sdfForm = Application.OpenForms["sdf"];
			if (sdfForm == null) {
				sdfForm = new SdfForm(this, Workbook);
				sdfForm.Show();
			}
			else {
				((SdfForm)sdfForm).PopulateFunctionListBox(true);
			}
		}

		public void ChangeFocus(FullCellAddr fca) {
			sheetHolder.SelectTab(fca.sheet.Name);
			SheetTab sheetTab = (SheetTab)sheetHolder.SelectedTab;
			sheetTab.ScrollTo(fca);
		}

		public void ChangeCellBackgroundColor(FullCellAddr fca, Color c) {
			sheetHolder.SelectTab(fca.sheet.Name);
			SelectedSheet.ChangeCellBackgroundColor(fca, c);
		}

		private void showFormulasMenuItem_Click(object sender, EventArgs e) {
			Workbook.format.ShowFormulas = showFormulasToolStripMenuItem.Checked;
			// Wrap formula text in cells
			//DataGridViewCellStyle dgvcs = new DataGridViewCellStyle();
			//dgvcs.WrapMode = showFormulasToolStripMenuItem.Checked ? DataGridViewTriState.True : DataGridViewTriState.False;
			//foreach (SheetTab sheetTab in sheetHolder.TabPages)
			//  sheetTab.SetRowCellStyle(dgvcs);
			Reshow(null);
		}

		private void morePrecedentsToolStripMenuItem_Click(object sender, EventArgs e) {
			if (SelectedSheet != null) {
				precedentsDepth++;
				SelectedSheet.Refresh();
			}
		}

		private void fewerPrecedentsToolStripMenuItem_Click(object sender, EventArgs e) {
			if (SelectedSheet != null) {
				precedentsDepth = Math.Max(0, precedentsDepth - 1);
				SelectedSheet.Refresh();
			}
		}

		private void moreDependentsToolStripMenuItem_Click(object sender, EventArgs e) {
			if (SelectedSheet != null) {
				dependentsDepth++;
				SelectedSheet.Refresh();
			}
		}

		private void fewerDependentsToolStripMenuItem_Click(object sender, EventArgs e) {
			if (SelectedSheet != null) {
				dependentsDepth = Math.Max(0, dependentsDepth - 1);
				SelectedSheet.Refresh();
			}
		}

		private void eraseArrowsToolStripMenuItem_Click(object sender, EventArgs e) {
			precedentsDepth = dependentsDepth = 0;
			if (SelectedSheet != null) {
				SelectedSheet.Refresh();
			}
		}

		private void cutToolStripMenuItem_Click(object sender, EventArgs e) {
			// TODO, not implemented
		}
	}

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
							 pen.EndCap = SDD2D.LineCap.ArrowAnchor;
							 pen.StartCap = SDD2D.LineCap.RoundAnchor;
							 if (dgv.SelectedCells.Count > 0) {
								 DataGridViewCell dgvc = dgv.SelectedCells[0];
								 CellAddr ca = new CellAddr(dgvc.ColumnIndex, dgvc.RowIndex);
								 Graphics g = arg.Graphics;
								 g.SmoothingMode = SDD2D.SmoothingMode.AntiAlias;
								 int x = dgv.RowHeadersWidth,
									 y = dgv.ColumnHeadersHeight,
									 w = dgv.DisplayRectangle.Width - x,
									 h = dgv.DisplayRectangle.Height - y;
								 // Clip headers *before* the scroll translation/transform
								 g.Clip = new Region(new Rectangle(x, y, w, h));
								 g.Transform = new SDD2D.Matrix(1,
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