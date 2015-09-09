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
using System.Drawing;
using System.Windows.Forms;

using Corecalc;
using Corecalc.Funcalc;
using Corecalc.IO;

using CoreCalc.CellAddressing;
using CoreCalc.IO;
using CoreCalc.Types;

using NotImplementedException = CoreCalc.Types.NotImplementedException;
// Size
// Pen etc

// SdfManager

namespace CoreCalc.GUI {
	/// <summary>
	/// A WorkbookForm is the user interface of an open workbook. 
	/// </summary>
	public partial class WorkbookForm : Form {
		public Workbook Workbook { get; private set; }
		private Corecalc.Benchmarks.Benchmarks test;
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
			if (Workbook != null) {
				Workbook.Clear();
				Workbook.OnFunctionsAltered -= workbook_OnFunctionsAltered;
			}
			Workbook = newWorkbook;
			Workbook.OnFunctionsAltered += workbook_OnFunctionsAltered;
		}

		private void workbook_OnFunctionsAltered(String[] functions) {
			Console.WriteLine("Regenerating modified functions");
			SdfManager.Regenerate(functions);
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e) { Environment.Exit(0); }

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
			SelectedSheet?.Reshow();
			SetStatusLine(elapsed);
		}

		public void SetStatusLine(long? elapsed) {
			double memory = GC.GetTotalMemory(false)/1E6; // In MB
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
			SelectedSheet?.Copy();
		}

		// Delete cell
		private void deleteToolStripMenuItem_Click(object sender, EventArgs e) {
			SelectedSheet?.Delete();
		}

		// Insert column
		private void columnToolStripMenuItem_Click(object sender, EventArgs e) {
			SelectedSheet?.InsertColumns(1);
		}

		// Insert row
		private void rowToolStripMenuItem_Click(object sender, EventArgs e) {
			SelectedSheet?.InsertRows(1);
		}

		// Paste copied or cut cells, or text
		private void pasteToolStripMenuItem_Click(object sender, EventArgs e) {
			SelectedSheet?.Paste();
		}

		// Get selected sheet if any, else null
		private SheetTab SelectedSheet => sheetHolder.TabCount > 0 ? sheetHolder.SelectedTab as SheetTab : null;

		public Corecalc.Benchmarks.Benchmarks Test => test ?? (test = new Corecalc.Benchmarks.Benchmarks(true));

		private void formulaBox_TextChanged(object sender, EventArgs e) {
			SelectedSheet?.ChangeCurrentText(formulaBox.Text);
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

		private void openWorkbookToolStripMenuItem_Click(object sender, EventArgs e) {
			var dbWorkbookIO = new DbIOFormat();
			Clear();
			var wb = dbWorkbookIO.Read();
			if (wb != null) {
				SetWorkbook(wb);
				DisplayWorkbook();
			}
		}

		private void saveWorkbookToolStripMenuItem_Click(object sender, EventArgs e) {
			// TODO: save the workbook back to the DB
			throw new NotImplementedException("'Save' doesn't do anything, yet.");
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
			int runs;
			if (int.TryParse(numberOfRunsTextBox.Text, out runs)) {
				Test.BenchmarkRecalculation(this, runs);
			}
		}

		private void fullRecalculationMenuItem_Click(object sender, EventArgs e) {
			int runs;
			if (int.TryParse(numberOfRunsTextBox.Text, out runs)) {
				Test.BenchmarkRecalculationFull(this, runs);
			}
		}

		private void recalculationFullRebuildMenuItem_Click(object sender, EventArgs e) {
			int runs;
			if (int.TryParse(numberOfRunsTextBox.Text, out runs)) {
				Test.BenchmarkRecalculationFullRebuild(this, runs);
			}
		}

		private void newFunctionSheetMenuItem_Click(object sender, EventArgs e) { InsertSheet(functionSheet: true); }

		private void sheetHolder_DrawItem(object sender, DrawItemEventArgs e) {
			int currentIndex = e.Index;
			Graphics g = e.Graphics;
			TabControl tc = (TabControl)sender;
			TabPage tp = tc.TabPages[currentIndex];

			Sheet currentSheet = Workbook[currentIndex];
			Brush theBrush = new SolidBrush(Color.Black);
			StringFormat sf = new StringFormat {Alignment = StringAlignment.Center};
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
			SelectedSheet?.Refresh();
		}

		private void cutToolStripMenuItem_Click(object sender, EventArgs e) {
			// TODO, not implemented
		}
	}
}