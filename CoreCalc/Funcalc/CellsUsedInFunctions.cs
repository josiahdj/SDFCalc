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
using System.Collections.Generic;
using System.Text;

using CoreCalc.CellAddressing;
using CoreCalc.Types;

namespace Corecalc.Funcalc {
	/// <summary>
	/// A CellsUsedInFunctions maps each function sheet cell to the
	/// names of sheet-defined functions using that cell, and conversely,
	/// maps each name of a sheet-defined function to the cells that 
	/// function uses.  There should be a single instance, as a static field 
	/// in class SdfManager.
	/// </summary>
	public class CellsUsedInFunctions {
		public readonly Dictionary<FullCellAddr, HashSet<string>> addressToFunctionList
			= new Dictionary<FullCellAddr, HashSet<string>>();

		private readonly Dictionary<string, List<FullCellAddr>> functionToAddressList
			= new Dictionary<string, List<FullCellAddr>>();

		// Bags, because multiple sheet-defined functions may use the same cells as 
		// input cell or as output cell
		public readonly HashBag<FullCellAddr>
			inputCellBag = new HashBag<FullCellAddr>(),
			outputCellBag = new HashBag<FullCellAddr>();

		public void Clear() {
			addressToFunctionList.Clear();
			functionToAddressList.Clear();
			inputCellBag.Clear();
			outputCellBag.Clear();
		}

		internal ICollection<string> GetFunctionsUsingAddresses(ICollection<FullCellAddr> fcas) {
			ISet<string> affectedFunctions = new SortedSet<string>();
			foreach (FullCellAddr fca in fcas) {
				HashSet<string> names;
				if (fca.sheet.IsFunctionSheet && addressToFunctionList.TryGetValue(fca, out names)) {
					affectedFunctions.UnionWith(names);
				}
			}
			return affectedFunctions;
		}

		internal void AddFunction(SdfInfo info, ICollection<FullCellAddr> addrs) {
			HashSet<FullCellAddr> inputCellSet = new HashSet<FullCellAddr>();
			inputCellSet.UnionWith(info.inputCells);
			foreach (FullCellAddr addr in addrs) {
				if (!inputCellSet.Contains(addr)) {
					AddCellToFunction(info.name, addr);
				}
			}
			List<FullCellAddr> addrsList = new List<FullCellAddr>(addrs);
			functionToAddressList[info.name] = addrsList;
			inputCellBag.AddAll(info.inputCells);
			outputCellBag.Add(info.outputCell);
		}

		internal void RemoveFunction(SdfInfo info) {
			inputCellBag.RemoveAll(info.inputCells);
			outputCellBag.Remove(info.outputCell);
			List<FullCellAddr> addresses;
			if (!functionToAddressList.TryGetValue(info.name, out addresses)) {
				return;
			}
			foreach (FullCellAddr addr in addresses) {
				addressToFunctionList.Remove(addr);
			}
			functionToAddressList.Remove(info.name);
		}

		private void AddCellToFunction(String info, FullCellAddr addr) {
			HashSet<String> names;
			if (!addressToFunctionList.TryGetValue(addr, out names)) {
				names = new HashSet<String>();
				addressToFunctionList[addr] = names;
			}
			names.Add(info);
		}
	}
}