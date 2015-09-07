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

// Class Expr and its subclasses are used to recursively build formulas

namespace Corecalc {
  // ----------------------------------------------------------------
  
  /// <summary>
  /// An Expr is an expression that may appear in a Formula cell.
  /// </summary>
  public abstract class Expr : IDepend {
    // Update cell references when containing cell is moved (not copied)
    public abstract Expr Move(int deltaCol, int deltaRow);

    // Invalidate off-sheet references when containing cell is copied (not moved)
    public abstract Expr CopyTo(int col, int row);

    // Evaluate expression as if at cell address sheet[col, row]
    public abstract Value Eval(Sheet sheet, int col, int row);

    internal abstract void VisitorCall(IExpressionVisitor visitor);

    // Insert N new rowcols before rowcol R>=0, when we're at rowcol r
    public abstract Adjusted<Expr> InsertRowCols(Sheet modSheet, bool thisSheet,
                                                 int R, int N, int r, bool doRows);

    // Apply refAct once to each CellRef in expression, and areaAct once to each CellArea
    internal abstract void VisitRefs(RefSet refSet, Action<CellRef> refAct, 
                                                    Action<CellArea> areaAct);

    // Increase the support sets of all cells referred from this expression, when
    // the expression appears in the block supported[col..col+cols-1, row..row+rows-1] 
    internal void AddToSupportSets(Sheet supported, int col, int row, int cols, int rows) {
      VisitRefs(new RefSet(),
        cellRef   => cellRef.AddToSupport(supported, col, row, cols, rows),
        cellArea => cellArea.AddToSupport(supported, col, row, cols, rows));
    }

    // Remove sheet[col, row] from the support sets of cells referred from this expression
    public void RemoveFromSupportSets(Sheet sheet, int col, int row) {
      ForEachReferred(sheet, col, row, // Remove sheet[col,row] from support set at fca
        delegate(FullCellAddr fca) {
          Cell cell;
          if (fca.TryGetCell(out cell)) // Will be non-null if support correctly added
            cell.RemoveSupportFor(sheet, col, row);
        });
    }

    // Apply act, once only, to the full cell address of each cell referred from expression
    public void ForEachReferred(Sheet sheet, int col, int row, Action<FullCellAddr> act) {
      VisitRefs(new RefSet(), 
        (CellRef cellRef)  => act(cellRef.GetAbsoluteAddr(sheet, col, row)), 
        (CellArea areaRef) => areaRef.ApplyToFcas(sheet, col, row, act));
    }

    // Call dependsOn(fca) on all cells fca referred from expression, with multiplicity.
    // Cannot be implemented in terms of VisitRefs, which visits only once.
    public abstract void DependsOn(FullCellAddr here, Action<FullCellAddr> dependsOn);
    
    // True if expression textually contains a call to a volatile function
    public abstract bool IsVolatile { get; }

    // Show contents as expression
    public abstract String Show(int col, int row, int ctxpre, Formats fo);
  }
}
