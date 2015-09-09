// Funcalc, spreadsheet with functions
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
using System.Windows.Forms;

using Corecalc;
using Corecalc.Funcalc;

using CoreCalc.CellAddressing;

// SdfInfo, SdfManager

namespace CoreCalc.GUI {
	public partial class SdfForm : Form {
		private WorkbookForm gui;

		public SdfForm(WorkbookForm gui, Workbook wb) {
			InitializeComponent();
			this.gui = gui;
			PopulateFunctionListBox(false);
		}

		public void PopulateFunctionListBox(bool rememberSelectedIndex) {
			int selectedIndex = functionsListBox.SelectedIndex;
			string selectedName = "";
			if (selectedIndex != -1) {
				selectedName = ((SdfInfo)functionsListBox.Items[selectedIndex]).name;
			}

			functionsListBox.Items.Clear();
			int i = 0;
			foreach (SdfInfo info in SdfManager.GetAllInfos()) {
				functionsListBox.Items.Add(info);
				if (selectedName == info.name) {
					selectedIndex = i;
				}
				i++;
			}
			if (rememberSelectedIndex && selectedIndex != -1) {
				functionsListBox.SelectedIndex = selectedIndex;
			}
		}

		public void PopulateFunctionListBox(string name) {
			name = name.ToUpper();
			for (int i = 0; i < functionsListBox.Items.Count; i++) {
				if (((SdfInfo)functionsListBox.Items[i]).name.ToUpper() == name) {
					functionsListBox.SelectedIndex = i;
				}
			}
		}

		private void functionsListbox_DoubleClick(object sender, EventArgs e) { RefreshInfo(); }

		private void RefreshInfo() {
			if (functionsListBox.SelectedItem != null) {
				SdfInfo info = (SdfInfo)functionsListBox.SelectedItem;
				int minCol = info.outputCell.ca.col, minRow = info.outputCell.ca.row;
				foreach (FullCellAddr cell in info.inputCells) {
					minCol = Math.Min(minCol, cell.ca.col);
					minRow = Math.Min(minRow, cell.ca.row);
				}
				gui.ChangeFocus(new FullCellAddr(info.outputCell.sheet, minCol, minRow));
			}
		}

		private void ShowBytecode_Click(object sender, EventArgs e) {
			if (functionsListBox.SelectedItem != null) {
				SdfInfo info = (SdfInfo)functionsListBox.SelectedItem;
				SdfManager.ShowIL(info);
			}
		}
	}
}