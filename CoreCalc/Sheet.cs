// Funcalc, a spreadsheet core implementation 
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
using System.Diagnostics;
using SC = System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Corecalc {
  /// <summary>
  /// A Sheet is a rectangular [col,row]-indexable collection of Cells, 
  /// represented by a SheetRep object.
  /// A Sheet belongs to a single Workbook, and only once.
  /// </summary>
  public sealed class Sheet : IEnumerable<Cell> {
    public const int cols = 20, rows = 1000;   // Default sheet size
    private String name;
    public readonly Workbook workbook;         // Non-null
    private readonly SheetRep cells;
    public int Cols { get; private set; }
    public int Rows { get; private set; }

    private bool isFunctionSheet;

    public Sheet(Workbook workbook, String name, bool functionSheet)
      : this(workbook, name, cols, rows, functionSheet) {
    }

    public Sheet(Workbook workbook, String name, int cols, int rows, bool functionSheet) {
      this.workbook = workbook;
      this.name = name;
      this.cells = new SheetRep();
      Cols = cols;
      Rows = rows;
      this.isFunctionSheet = functionSheet;
      workbook.AddSheet(this);
    }

    // Recalculate all cells by evaluating their contents
    public void RecalculateFull() {
      cells.Forall((col, row, cell) => cell.Eval(this, col, row));
    }

    // Show the contents of all non-null cells
    public void ShowAll(Action<int,int,String> show) {
      // Should use cells.Forall(show) but that may not clean up newly empty cells?
      for (int col = 0; col < Cols; col++)
        for (int row = 0; row < Rows; row++) {
          Cell cell = cells[col, row];
          show(col, row, cell != null ? ShowValue(col, row) : null);
        }
    }

    // Reset recomputation flags after a circularity has been found
    public void ResetCellState() {
      foreach (Cell cell in cells)
        if (cell != null)
          cell.ResetCellState();
    }

    // From parsed constant or formula or null but not array formula, at sheet[col, row]
    public void SetCell(Cell cell, int col, int row) {
      Debug.Assert(!(cell is ArrayFormula));
      this[col, row] = cell;
      if (cell != null)
        cell.AddToSupportSets(this, col, row, 1, 1);
    }

    // Insert cell, which must be Formula, as array formula 
    // in area ((ulCol,ulRow), (lrCol, lrRow))
    public void SetArrayFormula(Cell cell, int col, int row, CellAddr ulCa, CellAddr lrCa) 
    {
      Formula formula = cell as Formula;
      if (cell == null)
        throw new Exception("Invalid array formula");
      else {
        CachedArrayFormula caf = new CachedArrayFormula(formula, this, col, row, ulCa, lrCa);
        // Increase support sets of cells referred by formula
        formula.AddToSupportSets(this, col, row, 1, 1);  
        Interval displayCols = new Interval(ulCa.col, lrCa.col), 
                 displayRows = new Interval(ulCa.row, lrCa.row);
        // The underlying formula supports (only) the ArrayFormula cells in display range
        formula.ResetSupportSet();
        formula.AddSupport(this, col, row, this, displayCols, displayRows);
        int cols = lrCa.col - ulCa.col + 1, rows = lrCa.row - ulCa.row + 1;
        for (int c = 0; c < cols; c++)
          for (int r = 0; r < rows; r++)
            this[ulCa.col + c, ulCa.row + r] = new ArrayFormula(caf, c, r);
      }
    }

    // Copy cell to area ((col,row), (col+cols-1,row+rows-1))
    // Probably makes no sense when the cell is an array formula
    public void PasteCell(Cell cell, int col, int row, int cols, int rows) {
      for (int c = 0; c < cols; c++)
        for (int r = 0; r < rows; r++)
          // TODO: When cell contains CellRef or CellArea, should check that
          // references are not invalid, as in A0 or A-1:B2
          this[col + c, row + r] = cell.CloneCell(col, row);
      cell.AddToSupportSets(this, col, row, cols, rows);
    }

    // Move cell (fromCol, fromRow) to cell (col, row)
    public void MoveCell(int fromCol, int fromRow, int col, int row) {
      Cell cell = cells[fromCol, fromRow];
      this[col, row] = cell.MoveContents(col - fromCol, row - fromRow);
    }

    // Insert N new rows just before row R >= 0
    // TODO: Consider replacing all assignments to cells[,] with assignments to this[,],
    // for proper maintenance of support sets and volatile status
    public void InsertRowCols(int R, int N, bool doRows) {
      // Check that this will not split a array formula
      if (R >= 1)
        if (doRows) {
          for (int col = 0; col < Cols; col++) {
            Cell cell = cells[col, R - 1];
            ArrayFormula mf = cell as ArrayFormula;
            if (mf != null && mf.Contains(col, R))
              throw new Exception("Row insert would split array formula");
          }
        }
        else {
          for (int row = 0; row < Rows; row++) {
            Cell cell = cells[R - 1, row];
            ArrayFormula mf = cell as ArrayFormula;
            if (mf != null && mf.Contains(R, row))
              throw new Exception("Column insert would split array formula");
          }
        }
      // Adjust formulas in all sheets.  The dictionary records adjusted
      // expressions to preserve sharing of expressions where possible.
      Dictionary<Expr, Adjusted<Expr>> adjusted
        = new Dictionary<Expr, Adjusted<Expr>>();
      foreach (Sheet sheet in workbook) {
        for (int r = 0; r < sheet.Rows; r++)
          for (int c = 0; c < sheet.Cols; c++) {
            Cell cell = sheet.cells[c, r];
            if (cell != null)
              cell.InsertRowCols(adjusted, this, sheet == this, R, N,
               doRows ? r : c, doRows);
          }
      }
      if (doRows) {
        // Move the rows R, R+1, ... later by N rows in current sheet
        for (int r = Rows - 1; r >= R + N; r--)
          for (int c = 0; c < Cols; c++)
            cells[c, r] = cells[c, r - N];
        // Finally, null out the fresh rows
        for (int r = 0; r < N; r++)
          for (int c = 0; c < Cols; c++)
            cells[c, r + R] = null;
      }
      else {
        // Move the columns R, R+1, ... later by N columns in current sheet
        for (int c = Cols - 1; c >= R + N; c--)
          for (int r = 0; r < Rows; r++)
            cells[c, r] = cells[c - N, r];
        // Finally, null out the fresh columns
        for (int c = 0; c < N; c++)
          for (int r = 0; r < Rows; r++)
            cells[c + R, r] = null;
      }
    }

    // Show contents, if any, of cell at (col, row)
    public String Show(int col, int row) {
      if (0 <= col && col < Cols && 0 <= row && row < Rows) {
        Cell cell = cells[col, row];
        if (cell != null)
          return cell.Show(col, row, workbook.format);
      }
      return null;
    }

    // Show value (or contents), if any, of cell (col, row)
    public String ShowValue(int col, int row) {
      if (0 <= col && col < Cols && 0 <= row && row < Rows) {
        Cell cell = cells[col, row];
        if (cell != null)
          if (workbook.format.ShowFormulas)
            return cell.Show(col, row, workbook.format);
          else
            return cell.ShowValue(this, col, row);
      }
      return null;
    }

    // Get and set cell contents, maintaining support and volatility information
    public Cell this[int col, int row] {
      get {
        return col < Cols && row < Rows ? cells[col, row] : null;
      }
      set {
        if (col < Cols && row < Rows) {
          Cell oldCell = cells[col, row];
          if (oldCell != value) {
            if (oldCell != null) {
              oldCell.TransferSupportTo(ref value);  // Assumes oldCell used nowhere else
              workbook.DecreaseVolatileSet(oldCell, this, col, row);
              oldCell.RemoveFromSupportSets(this, col, row);
            }
            workbook.IncreaseVolatileSet(value, this, col, row);
            // We do not add to support sets here; that would fragment the support sets
            cells[col, row] = value;
            workbook.RecordCellChange(col, row, this);
          }
        }
      }
    }

    public Cell this[CellAddr ca] {
      get {
        return this[ca.col, ca.row];
      }
      private set {
        this[ca.col, ca.row] = value;
      }
    }

    public String Name {
      get { return name; }
      set { name = value; }
    }

    public bool IsFunctionSheet {
      get { return isFunctionSheet; }
      set { isFunctionSheet = value; }
    }

    public IEnumerator<Cell> GetEnumerator() {
      return cells.GetEnumerator();
    }

    SC.IEnumerator SC.IEnumerable.GetEnumerator() {
      return GetEnumerator();
    }

    // Detect blocks of formula copies, for finding compact support sets
    public void AddToSupportSets() {
      int sheetCols = Cols, sheetRows = Rows;
      cells.Forall((int col, int row, Cell cell) =>
        {
          if (cell is ArrayFormula) {
            // Do not try to detect copies of array formulas.
            // CHECK THIS!
            ArrayFormula af = cell as ArrayFormula;
            af.AddToSupportSets(this, col, row, 1, 1);
          }
          else if (cell is Formula) {
            Formula f = cell as Formula;
            if (!f.Visited) {
              Expr expr = f.Expr;
              // (1) Find a large rectangle containing only formula f
              // (1a) Find largest square of copies with upper left corner = (col, row)
              int size = 1;
              while (col + size < sheetCols && row + size < sheetRows
                      && CheckCol(col + size, row, expr, size)
                      && CheckRow(col, row + size, expr, size)) {
                size++;
              }
              // All cells in sheet[col..col+size-1, row..row+size-1] contain expression expr
              // (1b) Try extending square with more rows below
              int rows = size;
              while (row + rows < sheetRows && CheckRow(col, row + rows, expr, size - 1))
                rows++;
              // sheet[col..col+size-1, row..row+rows-1] contains expr
              // (1c) Try extending square with more columns to the right
              int cols = size;
              while (col + cols < sheetCols && CheckCol(col + cols, row, expr, size - 1))
                cols++;
              // sheet[col..col+cols-1, row..row+size-1] contains expr
              if (rows > cols)
                cols = size;
              else
                rows = size;
              // All cells in sheet[col..col+cols-1, row..row+rows-1] contain expression expr
              // (2) Mark all cells in the rectangle visited
              for (int deltaCol = 0; deltaCol < cols; deltaCol++)
                for (int deltaRow = 0; deltaRow < rows; deltaRow++)
                  (this[col + deltaCol, row + deltaRow] as Formula).Visited = true;
              // (3) Update the support sets of cells referred to from expr
              expr.AddToSupportSets(this, col, row, cols, rows);
            }
          }
        });
      this.ResetCellState(); // Undo changes made to the sheet's cells' states
    }

    // Check the row sheet[col..col+size, row] for formulas identical to expr
    private bool CheckRow(int col, int row, Expr expr, int size) {
      for (int deltaCol=0; deltaCol<=size; deltaCol++) {
        Formula fcr = this[col+deltaCol, row] as Formula;
        if (fcr == null || fcr.Visited || fcr.Expr != expr)
          return false;
      }
      return true;
    }

    // Check the column sheet[col, row..row+size] for formulas identical to expr
    private bool CheckCol(int col, int row, Expr expr, int size) {
      for (int deltaRow=0; deltaRow<=size; deltaRow++) {
        Formula fcr = this[col, row+deltaRow] as Formula;
        if (fcr == null || fcr.Visited || fcr.Expr != expr)
          return false;
      }
      return true;
    }

    // Add supportedSheet[supportedColumns, supportedRows] to support set of this[col,row]
    public void AddSupport(int col, int row, Sheet supportedSheet, 
                           Interval supportedCols, Interval supportedRows) 
    {
      if (this[col, row] == null) 
        this[col, row] = new BlankCell();
      if (this[col, row] != null) // May still be null because outside sheet
        this[col, row].AddSupport(this, col, row, supportedSheet, supportedCols, supportedRows);
    }

    public void IncreaseVolatileSet() {
      cells.Forall((col, row, cell) => workbook.IncreaseVolatileSet(cell, this, col, row));
    }

    public override string ToString() {
      return name;
    }
  }

  /// <summary>
  /// A SheetRep represents a sheet's cell array sparsely, quadtree style, with four 
  /// levels of tiles, each conceptually a 16-column by 32-row 2D array, 
  /// for up to SIZEW = 2^16 = 64K columns and SIZEH = 2^20 = 1M rows.

  /// </summary>
  class SheetRep : IEnumerable<Cell> {
    // Sizes are chosen so that addresses can be calculated by bit-operations,
    // and the 2D tiles are represented by 1D arrays for speed.  The mask 
    // MW equals ...01111 with LOGW 1's, so (c&MW) equals (c%M); MH ditto.

    private const int // Could be uint, but then indexes must be cast to int
      LOGW = 4, W = 1 << LOGW, MW = W - 1, SIZEW = 1 << (4 * LOGW), // cols
      LOGH = 5, H = 1 << LOGH, MH = H - 1, SIZEH = 1 << (4 * LOGH); // rows
    private readonly Cell[][][][] tile0 = new Cell[W * H][][][];

    public Cell this[int c, int r] {
      get {
        if (c < 0 || SIZEW <= c || r < 0 || SIZEH <= r) 
          return null;
        Cell[][][] tile1 = tile0[(((c >> (3 * LOGW)) & MW) << LOGH) | ((r >> (3 * LOGH)) & MH)];
        if (tile1 == null)
          return null;
        Cell[][] tile2 = tile1[(((c >> (2 * LOGW)) & MW) << LOGH) | ((r >> (2 * LOGH)) & MH)];
        if (tile2 == null)
          return null;
        Cell[] tile3 = tile2[(((c >> (1 * LOGW)) & MW) << LOGH) | ((r >> (1 * LOGH)) & MH)];
        if (tile3 == null)
          return null;
        return tile3[((c & MW) << LOGH) | (r & MH)];
      }
      set {
        if (c < 0 || SIZEW <= c || r < 0 || SIZEH <= r)
          return;
        int index0 = (((c >> (3 * LOGW)) & MW) << LOGH) | ((r >> (3 * LOGH)) & MH);
        Cell[][][] tile1 = tile0[index0];
        if (tile1 == null)
          if (value == null)
            return;
          else
            tile1 = tile0[index0] = new Cell[W * H][][];
        int index1 = (((c >> (2 * LOGW)) & MW) << LOGH) | ((r >> (2 * LOGH)) & MH);
        Cell[][] tile2 = tile1[index1];
        if (tile2 == null)
          if (value == null)
            return;
          else
            tile2 = tile1[index1] = new Cell[W * H][];
        int index2 = (((c >> (1 * LOGW)) & MW) << LOGH) | ((r >> (1 * LOGH)) & MH);
        Cell[] tile3 = tile2[index2];
        if (tile3 == null)
          if (value == null)
            return;
          else
            tile3 = tile2[index2] = new Cell[W * H];
        int index3 = ((c & MW) << LOGH) | (r & MH);
        tile3[index3] = value;
      }
    }

    // Yield all the sheet's non-null cells
    public IEnumerator<Cell> GetEnumerator() {
      foreach (Cell[][][] tile1 in tile0)
        if (tile1 != null)
          foreach (Cell[][] tile2 in tile1)
            if (tile2 != null)
              foreach (Cell[] tile3 in tile2)
                if (tile3 != null)
                  foreach (Cell cell in tile3)
                    if (cell != null)
                      yield return cell;
    }

    SC.IEnumerator SC.IEnumerable.GetEnumerator() {
      return GetEnumerator();
    }

    // Sparse iteration, over non-null cells only
    public void Forall(Action<int, int, Cell> act) {
      int i0 = 0;
      foreach (Cell[][][] tile1 in tile0) {
        int i1 = 0, c0 = (i0 >> LOGH) << (3 * LOGW), r0 = (i0 & MH) << (3 * LOGH);
        if (tile1 != null)
          foreach (Cell[][] tile2 in tile1) {
            int i2 = 0, c1 = (i1 >> LOGH) << (2 * LOGW), r1 = (i1 & MH) << (2 * LOGH);
            if (tile2 != null)
              foreach (Cell[] tile3 in tile2) {
                int i3 = 0, c2 = (i2 >> LOGH) << (1 * LOGW), r2 = (i2 & MH) << (1 * LOGH);
                if (tile3 != null)
                  foreach (Cell cell in tile3) {
                    if (cell != null)
                      act(c0 | c1 | c2 | i3 >> LOGH, r0 | r1 | r2 | i3 & MH, cell);
                    i3++;
                  }
                i2++;
              }
            i1++;
          }
        i0++;
      }
    }
  }
}
