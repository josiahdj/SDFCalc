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
using System.IO;

using Corecalc.IO;

using CoreCalc.GUI;

namespace Corecalc {
	/// <summary>
	/// Class Program contains the Main function to start the GUI version of
	/// Funcalc.
	/// </summary>
	internal static class Program {
		[STAThread]
		public static void Main(String[] args) {
			if (args.Length == 0) {
				// GUI, with new empty workbook
				new WorkbookForm(new Workbook(), display: true);
				return;
			}
			if (args.Length == 1) {
				FileInfo fi = new FileInfo(args[0]);
				Console.WriteLine(fi);
				switch (fi.Extension) {
					case ".xml": // Attempt to open existing workbook in GUI
						Workbook wb = new WorkBookIO().Read(fi.FullName);
						if (wb != null) {
							new WorkbookForm(wb, display: true);
						}
						return;
				}
			}
			Console.WriteLine("Usage: Funcalc [workbook.xml]\n");
		}
	}
}