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

using Corecalc;

namespace Corecalc.Funcalc {
	/// <summary>
	/// A DependencyGraph is a graph representation of the dependencies between the function sheet 
	/// cells involved in a sheet-defined function.  Records the set 
	/// nodesPrecedents[fca] of cells directly referred by the formula in cell fca, as 
	/// well as the set nodesDependents[fca] of the cells directly referring to cell fca.
	/// Also determines whether the sheet-defined function references any volatile cells, 
	/// including volatile cells on normal sheets.
	/// </summary>
	public class DependencyGraph {
		/// <summary>
		/// Map from a cell to its dependents: the cells that directly depend on it.
		/// </summary>
		private readonly Dictionary<FullCellAddr, HashSet<FullCellAddr>> nodesDependents;

		/// <summary>
		/// Map from a cell to its precedents: the cells on which it directly depends.
		/// </summary>
		private readonly Dictionary<FullCellAddr, HashSet<FullCellAddr>> nodesPrecedents;

		public readonly FullCellAddr outputCell;
		public readonly FullCellAddr[] inputCells;
		public readonly HashSet<FullCellAddr> inputCellSet;
		// Maps a full cell address to the Formula or CGExpr at that address
		private readonly Func<FullCellAddr, IDepend> getNode;

		public DependencyGraph(FullCellAddr outputCell, FullCellAddr[] inputCells, Func<FullCellAddr, IDepend> getNode) {
			this.outputCell = outputCell;
			this.nodesDependents = new Dictionary<FullCellAddr, HashSet<FullCellAddr>>();
			this.nodesPrecedents = new Dictionary<FullCellAddr, HashSet<FullCellAddr>>();
			this.inputCells = inputCells;
			this.inputCellSet = new HashSet<FullCellAddr>();
			this.inputCellSet.UnionWith(inputCells);
			this.getNode = getNode;
			CollectPrecedents();
		}

		public HashSet<FullCellAddr> GetDependents(FullCellAddr fca) { return nodesDependents[fca]; }

		/// <summary>
		/// Make "precedent" a precedent of "node", and 
		/// make "node" a dependent of "precedent".  
		/// If the required sets of precedents resp. dependents do not exist, 
		/// create them and associate them with "node" resp. "precedent".
		/// </summary>
		/// <exception cref="CyclicException">If a static cycle exists</exception>
		public bool AddPrecedentDependent(FullCellAddr precedent, FullCellAddr node) {
			HashSet<FullCellAddr> precedents;
			if (nodesPrecedents.TryGetValue(node, out precedents)) {
				try {
					precedents.Add(precedent);
				}
				catch (ArgumentException) {
					//Happens if the element already exists: there is a cycle
					throw new CyclicException("Static cycle through cell " + precedent, precedent);
				}
			}
			else {
				precedents = new HashSet<FullCellAddr>();
				precedents.Add(precedent);
				nodesPrecedents.Add(node, precedents);
			}

			HashSet<FullCellAddr> dependents;
			if (nodesDependents.TryGetValue(precedent, out dependents)) {
				if (!dependents.Add(node)) {
					//Happens if the element already exists: there is a cycle
					throw new CyclicException("Static cycle through cell " + node, node);
				}
				else {
					return true;
				}
			}
			else {
				dependents = new HashSet<FullCellAddr>();
				dependents.Add(node);
				nodesDependents.Add(precedent, dependents);
				return false;
			}
		}

		/// <summary>
		/// Tests whether transitiveDependents contains any cell transitively dependent on node.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="possibleDependents"></param>
		/// <returns></returns>
		public bool HasDependent(FullCellAddr node, ICollection<FullCellAddr> transitiveDependents) {
			HashSet<FullCellAddr> visitedCells = new HashSet<FullCellAddr>();
			return HasDependentHelper(node, transitiveDependents, visitedCells);
		}

		private bool HasDependentHelper(FullCellAddr node,
										ICollection<FullCellAddr> transitiveDependents,
										HashSet<FullCellAddr> visitedCells) {
			HashSet<FullCellAddr> dependents;
			if (nodesDependents.TryGetValue(node, out dependents)) {
				foreach (FullCellAddr addr in transitiveDependents) {
					if (dependents.Contains(addr)) {
						return true;
					}
				}
				foreach (FullCellAddr addr in dependents) {
					if (visitedCells.Add(addr)) // returns true only first time
					{
						if (HasDependentHelper(addr, transitiveDependents, visitedCells)) {
							return true;
						}
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Tests whether possiblePrecedents contains any cell that node 
		/// transitively depends on. 
		/// </summary>
		/// <param name="node"></param>
		/// <param name="transitivePrecedents"></param>
		/// <returns></returns>
		public bool HasPrecedent(FullCellAddr node, ICollection<FullCellAddr> transitivePrecedents) {
			HashSet<FullCellAddr> visitedCells = new HashSet<FullCellAddr>();
			return HasPrecedentHelper(node, transitivePrecedents, visitedCells);
		}

		private bool HasPrecedentHelper(FullCellAddr node, ICollection<FullCellAddr> transitivePrecedents, HashSet<FullCellAddr> visitedCells) {
			HashSet<FullCellAddr> precedents;
			if (nodesPrecedents.TryGetValue(node, out precedents)) {
				foreach (FullCellAddr addr in transitivePrecedents) {
					if (precedents.Contains(addr)) {
						return true;
					}
				}
				foreach (FullCellAddr addr in precedents) {
					if (visitedCells.Add(addr)) {
						if (HasPrecedentHelper(addr, transitivePrecedents, visitedCells)) {
							return true;
						}
					}
				}
			}
			return false;
		}

		internal bool GetPrecedents(FullCellAddr node, out HashSet<FullCellAddr> precedents) { return nodesPrecedents.TryGetValue(node, out precedents); }

		/// <summary>
		/// Build the (two-way) dependency graph whose nodes are the 
		/// output cell and all cells (on the same function sheet) on 
		/// which it transitively depends.
		/// </summary>
		/// <exception cref="CyclicException"></exception>
		public void CollectPrecedents() {
			GetTransitivePrecedents(outputCell);
		}

		public IList<FullCellAddr> GetAllNodes() {
			// The full set of precedent cells is the output cell plus all cells
			// on which something depends -- true, but a funny way to compute it.
			IList<FullCellAddr> result = new List<FullCellAddr>();
			foreach (FullCellAddr fca in nodesDependents.Keys) {
				result.Add(fca);
			}
			result.Add(outputCell);
			return result;
		}

		/// <summary>
		/// Find all transitive precedents, that is, cells 
		/// that this cell transitively depends on.
		/// The cell thisFca is within the function sheet being translated.  
		/// Cells outside the function sheet are not traced.
		/// </summary>
		/// <param name="thisFca"></param>
		/// <exception cref="CyclicException"></exception>
		private void GetTransitivePrecedents(FullCellAddr thisFca) {
			ISet<FullCellAddr> precedents = new HashSet<FullCellAddr>();
			IDepend node = getNode(thisFca);
			if (node != null) {
				node.DependsOn(thisFca, delegate(FullCellAddr fca) { precedents.Add(fca); });
			}

			// Now precedents is the set of cells directly referred from 
			// the Expr in cell thisFca; that is, that cell's direct precedents

			foreach (FullCellAddr addr in precedents) {
				// Trace dependencies only from cells on this sheet, 
				// and don't trace precedents of input cells
				if (addr.sheet == thisFca.sheet) {
					if (!AddPrecedentDependent(addr, thisFca) && !inputCellSet.Contains(addr)) {
						GetTransitivePrecedents(addr);
					}
				}
			}
		}

		/// <summary>
		/// Build a list of all the Formula and ArrayFormula cells that the outputCell 
		/// depends on, in calculation order.
		/// </summary>
		/// <returns>Cells sorted in calculation order; output cell last.</returns>
		public IList<FullCellAddr> PrecedentOrder() {
			HashList<FullCellAddr> sorted = new HashList<FullCellAddr>();
			AddNode(sorted, outputCell);
			return sorted.ToArray();
		}

		private void AddNode(HashList<FullCellAddr> sorted, FullCellAddr node) {
			HashSet<FullCellAddr> precedents;
			if (GetPrecedents(node, out precedents)) {
				Cell cell;
				foreach (FullCellAddr precedent in precedents) {
					// By including only non-input Formula and ArrayFormula cells, we avoid that 
					// constant cells get stored in local variables.  The result will not contain 
					// constants cells (and will contain an input cell only if it is also the 
					// output cell), so must the raw graph to find all cells belonging to an SDF.
					if (!sorted.Contains(precedent)
						&& precedent.TryGetCell(out cell)
						&& (cell is Formula || cell is ArrayFormula)
						&& !inputCellSet.Contains(precedent)) {
						AddNode(sorted, precedent);
					}
				}
			}
			sorted.Add(node); // Last in HashList
		}
	}
}