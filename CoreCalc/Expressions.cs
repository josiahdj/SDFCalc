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
using System.Text;
using System.Diagnostics;

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
        (CellRef cellRef)   => cellRef.AddToSupport(supported, col, row, cols, rows),
        (CellArea cellArea) => cellArea.AddToSupport(supported, col, row, cols, rows));
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

  /// <summary>
  /// A Const expression is a constant, immutable and sharable.
  /// </summary>
  abstract class Const : Expr {
    public static Const Make(Value value) {
      if (value is NumberValue)
        return new NumberConst((value as NumberValue).value);
      else if (value is TextValue)
        return new TextConst((value as TextValue).value);
      else
        return new ValueConst(value);
    }

    public override Expr Move(int deltaCol, int deltaRow) {
      return this;
    }

    // Any expression can be copied with sharing
    public override Expr CopyTo(int col, int row) {
      return this;
    }

    public override Adjusted<Expr> InsertRowCols(Sheet modSheet, bool thisSheet,
                                                 int R, int N, int r, bool doRows) {
      return new Adjusted<Expr>(this);
    }

    internal override void VisitRefs(RefSet refSet, Action<CellRef> refAct, Action<CellArea> areaAct) 
    { }

    public override void DependsOn(FullCellAddr here, Action<FullCellAddr> dependsOn) { }

    public override bool IsVolatile {
      get { return false; }
    }
  }

  /// <summary>
  /// A NumberConst is a constant number-valued expression.
  /// </summary>
  class NumberConst : Const {
    public readonly NumberValue value;

    public NumberConst(double d) {
      Debug.Assert(!Double.IsNaN(d) && !Double.IsInfinity(d));
      value = (NumberValue)NumberValue.Make(d);
    }

    public override Value Eval(Sheet sheet, int col, int row) {
      return value;
    }

    internal override void VisitorCall(IExpressionVisitor visitor) {
      visitor.CallVisitor(this);
    }

    public override String Show(int col, int row, int ctxpre, Formats fo) {
      return value.ToString();
    }
  }

  /// <summary>
  /// A TextConst is a constant string-valued expression.
  /// </summary>
  class TextConst : Const {
    public readonly TextValue value;

    public TextConst(String s) {
      value = TextValue.MakeInterned(s);
    }

    public override Value Eval(Sheet sheet, int col, int row) {
      return value;
    }

    internal override void VisitorCall(IExpressionVisitor visitor) {
      visitor.CallVisitor(this);
    }

    public override String Show(int col, int row, int ctxpre, Formats fo) {
      return "\"" + value + "\"";
    }
  }
 
  /// <summary>
  /// A ValueConst is an arbitrary constant valued expression, used only
  /// for partial evaluation; there is no corresponding formula source syntax.
  /// </summary>
  class ValueConst : Const {
    public readonly Value value;

    public ValueConst(Value value) {
      this.value = value;
    }

    public override Value Eval(Sheet sheet, int col, int row) {
      return value;
    }

    internal override void VisitorCall(IExpressionVisitor visitor) {
      visitor.CallVisitor(this);
    }

    public override String Show(int col, int row, int ctxpre, Formats fo) {
      return "ValueConst[" + value + "]";
    }
  }

  /// <summary>
  /// An Error expression represents a static error, e.g. invalid cell reference.
  /// </summary>
  class Error : Const {
    private readonly String error;
    public readonly ErrorValue value;
    public static readonly Error refError = new Error(ErrorValue.refError);

    public Error(String msg) : this(ErrorValue.Make(msg)) { }

    public Error(ErrorValue value) {
      this.value = value;
      this.error = this.value.ToString();
    }

    public override Value Eval(Sheet sheet, int col, int row) {
      return value;
    }

    public override String Show(int col, int row, int ctxpre, Formats fo) {
      return error;
    }

    internal override void VisitorCall(IExpressionVisitor visitor) {
      visitor.CallVisitor(this);
    }
  }

  /// <summary>
  /// A FunCall expression is an operator application such as 1+$A$4 or a function
  /// call such as RAND() or SIN(4*A$7) or SUM(B4:B52; 3) or IF(A1; A2; 1/A1).
  /// </summary>
  class FunCall : Expr {
    public readonly Function function;   // Non-null
    public readonly Expr[] es;           // Non-null, elements non-null

    private FunCall(String name, params Expr[] es) 
      : this(Function.Get(name), es) { }

    private FunCall(Function function, Expr[] es) {
      // Assert: function != null, all es[i] != null
      this.function = function;
      this.es = es;
    }

    public static Expr Make(String name, Expr[] es) {
      Function function = Function.Get(name);
      if (function == null)
        function = Function.MakeUnknown(name);
      for (int i = 0; i < es.Length; i++)
        if (es[i] == null)
          es[i] = new Error("#SYNTAX");
      if (name == "SPECIALIZE" && es.Length > 1)
        return new FunCall("SPECIALIZE", Make("CLOSURE", es));
      else
        return new FunCall(function, es);
    }

    // Arguments are passed unevaluated to cater for non-strict IF 

    public override Value Eval(Sheet sheet, int col, int row) {
      return function.Applier(sheet, es, col, row);
    }

    public override Expr Move(int deltaCol, int deltaRow) {
      Expr[] newEs = new Expr[es.Length];
      for (int i = 0; i < es.Length; i++)
        newEs[i] = es[i].Move(deltaCol, deltaRow);
      return new FunCall(function, newEs);
    }

    // Can be copied with sharing if arguments can
    public override Expr CopyTo(int col, int row) {
      bool same = true;
      Expr[] newEs = new Expr[es.Length];
      for (int i = 0; i < es.Length; i++) {
        newEs[i] = es[i].CopyTo(col, row);
        same &= Object.ReferenceEquals(newEs[i], es[i]);
      }
      return same ? this : new FunCall(function, newEs);
    }

    public override Adjusted<Expr> InsertRowCols(Sheet modSheet, bool thisSheet,
                                                 int R, int N, int r, bool doRows) 
    {
      Expr[] newEs = new Expr[es.Length];
      int upper = int.MaxValue;
      bool same = true;
      for (int i = 0; i < es.Length; i++) {
        Adjusted<Expr> ae
          = es[i].InsertRowCols(modSheet, thisSheet, R, N, r, doRows);
        upper = Math.Min(upper, ae.upper);
        same = same && ae.same;
        newEs[i] = ae.e;
      }
      return new Adjusted<Expr>(new FunCall(function, newEs), upper, same);
    }

    // Show infixed operators as infix and without excess parentheses

    public override String Show(int col, int row, int ctxpre, Formats fo) {
      StringBuilder sb = new StringBuilder();
      int pre = function.fixity;
      if (pre == 0) { // Not operator
        sb.Append(function.name).Append("(");
        for (int i = 0; i < es.Length; i++) {
          if (i > 0)
            sb.Append(", ");
          sb.Append(es[i].Show(col, row, 0, fo));
        }
        sb.Append(")");
      } else { // Operator.  Assume es.Length is 1 or 2 
        if (es.Length == 2) {
          // If precedence lower than context, add parens
          if (pre < ctxpre)
            sb.Append("(");
          sb.Append(es[0].Show(col, row, pre, fo));
          sb.Append(function.name);
          // Only higher precedence right operands avoid parentheses
          sb.Append(es[1].Show(col, row, pre + 1, fo));
          if (pre < ctxpre)
            sb.Append(")");
        } else if (es.Length == 1) {
          sb.Append(function.name == "NEG" ? "-" : function.name);
          sb.Append(es[0].Show(col, row, pre, fo));
        } else
          throw new ImpossibleException("Operator not unary or binary");
      }
      return sb.ToString();
    }

    internal override void VisitRefs(RefSet refSet, Action<CellRef> refAct, Action<CellArea> areaAct) 
    {
      foreach (Expr e in es)
        e.VisitRefs(refSet, refAct, areaAct);
    }

    public override void DependsOn(FullCellAddr here, Action<FullCellAddr> dependsOn) {
      foreach (Expr e in es)
        e.DependsOn(here, dependsOn);
    }

    public override bool IsVolatile {
      get {
        if (function.IsVolatile(es))
          return true;
        foreach (Expr e in es)
          if (e.IsVolatile)
            return true;
        return false;
      }
    }

    internal override void VisitorCall(IExpressionVisitor visitor) {
      visitor.CallVisitor(this);
    }
  }

  /// <summary>
  /// A CellRef expression refers to a single cell, eg. 
  /// is A1 or $A1 or A$1 or $A$1 or Sheet1!A1.
  /// </summary>
  class CellRef : Expr, IEquatable<CellRef> {
    public readonly RARef raref;
    public readonly Sheet sheet;    // non-null if sheet-absolute

    public CellRef(Sheet sheet, RARef raref) {
      this.sheet = sheet;
      this.raref = raref;
    }

    // Evaluate cell ref by evaluating the cell referred to 

    public override Value Eval(Sheet sheet, int col, int row) {
      CellAddr ca = raref.Addr(col, row);
      Cell cell = (this.sheet ?? sheet)[ca];
      return cell == null ? null : cell.Eval(sheet, ca.col, ca.row);
    }

    public FullCellAddr GetAbsoluteAddr(Sheet sheet, int col, int row) {
      return new FullCellAddr(this.sheet ?? sheet, raref.Addr(col, row));
    }

    public FullCellAddr GetAbsoluteAddr(FullCellAddr fca) {
      return GetAbsoluteAddr(fca.sheet, fca.ca.col, fca.ca.row);
    }

    // Clone and move (when the containing formula is moved, not copied!)
    public override Expr Move(int deltaCol, int deltaRow) {
      return new CellRef(sheet, raref.Move(deltaCol, deltaRow));
    }

    // Can be copied with sharing iff reference is within sheet
    public override Expr CopyTo(int col, int row) {
      if (raref.ValidAt(col, row))
        return this;
      return
        Error.refError;
    }

    public override Adjusted<Expr> InsertRowCols(Sheet modSheet, bool thisSheet,
                                                 int R, int N, int r,
                                                 bool doRows) {
      if (sheet == modSheet || sheet == null && thisSheet) {
        Adjusted<RARef> adj = raref.InsertRowCols(R, N, r, doRows);
        return new Adjusted<Expr>(new CellRef(sheet, adj.e), adj.upper, adj.same);
      } else
        return new Adjusted<Expr>(this);
    }

    internal void AddToSupport(Sheet supported, int col, int row, int cols, int rows) 
    {
      Sheet referredSheet = this.sheet ?? supported;
      int ca = raref.colRef, ra = raref.rowRef;
      int r1 = row, r2 = row + rows - 1, c1 = col, c2 = col + cols - 1;
      Interval referredCols, referredRows;
      Func<int, Interval> supportedCols, supportedRows;
      RefAndSupp(raref.colAbs, ca, c1, c2, out referredCols, out supportedCols);
      RefAndSupp(raref.rowAbs, ra, r1, r2, out referredRows, out supportedRows);
      // Outer iteration is made over the shorter interval for efficiency
      if (referredCols.Length < referredRows.Length)
        referredCols.ForEach(c => {
          Interval suppCols = supportedCols(c);
          referredRows.ForEach(r =>
            referredSheet.AddSupport(c, r, supported, suppCols, supportedRows(r)));
        });
      else
        referredRows.ForEach(r => {
          Interval suppRows = supportedRows(r);
          referredCols.ForEach(c =>
            referredSheet.AddSupport(c, r, supported, supportedCols(c), suppRows));
        }); 
    }

    // This uses the notation from the book's analysis of the row aspect of support sets
    private static void RefAndSupp(bool abs, int ra, int r1, int r2,
        out Interval referred, out Func<int, Interval> supported) {
      if (abs) { // case abs          
        referred = new Interval(ra, ra);
        supported = r => new Interval(r1, r2);
      } else {   // case rel
        referred = new Interval(r1 + ra, r2 + ra);
        supported = r => new Interval(r - ra, r - ra);
      }
    }

    internal override void VisitRefs(RefSet refSet, Action<CellRef> refAct, Action<CellArea> areaAct) 
    {
      if (!refSet.SeenBefore(this))
        refAct(this);
    }

    public override void DependsOn(FullCellAddr here, Action<FullCellAddr> dependsOn) {
      dependsOn(GetAbsoluteAddr(here));
    }

    public override bool IsVolatile {
      get { return false; }
    }

    public bool Equals(CellRef that) {
      return this.raref.Equals(that.raref);
    }

    public override int GetHashCode() {
      return raref.GetHashCode();
    }

    public override String Show(int col, int row, int ctxpre, Formats fo) {
      String s = raref.Show(col, row, fo);
      return sheet == null ? s : sheet.Name + "!" + s;
    }

    internal override void VisitorCall(IExpressionVisitor visitor) {
      visitor.CallVisitor(this);
    }
  }

  /// <summary>
  /// A CellArea expression refers to a rectangular cell area, eg. is
  /// A1:C4 or $A$1:C4 or A1:$C4 or Sheet1!A1:C4
  /// </summary>
  class CellArea : Expr, IEquatable<CellArea> {
    // It would be desirable to store the cell area in normalized form, 
    // with ul always to the left and above lr; but this cannot be done 
    // because it may be mixed relative/absolute as in $A1:$A$5, which 
    // when copied from B1 to B10 is no longer normalized.
    private readonly RARef ul, lr;  // upper left, lower right
    public readonly Sheet sheet;    // non-null if sheet-absolute

    public CellArea(Sheet sheet,
                    bool ulColAbs, int ulColRef, bool ulRowAbs, int ulRowRef,
                    bool lrColAbs, int lrColRef, bool lrRowAbs, int lrRowRef)
      : this(sheet,
             new RARef(ulColAbs, ulColRef, ulRowAbs, ulRowRef),
             new RARef(lrColAbs, lrColRef, lrRowAbs, lrRowRef)) {
    }

    public CellArea(Sheet sheet, RARef ul, RARef lr) {
      this.sheet = sheet;
      this.ul = ul;
      this.lr = lr;
    }

    // Evaluate cell area by returning an array view of it

    public override Value Eval(Sheet sheet, int col, int row) {
      return MakeArrayView(sheet, col, row);
    }

    public ArrayView MakeArrayView(FullCellAddr fca) {
      return MakeArrayView(fca.sheet, fca.ca.col, fca.ca.row);
    }

    public ArrayView MakeArrayView(Sheet sheet, int col, int row) {
      CellAddr ulCa = ul.Addr(col, row), lrCa = lr.Addr(col, row);
      ArrayView view = ArrayView.Make(ulCa, lrCa, this.sheet ?? sheet);
      // Forcing the evaluation of all cells in an array view value.
      // TODO: Doing this repeatedly, as in ManyDependents.xml, is costly
      for (int c = 0; c < view.Cols; c++)
        for (int r = 0; r < view.Rows; r++) {
          // Value ignore = view[c, r];
        }
      return view;
    }

    public void ApplyToFcas(Sheet sheet, int col, int row, Action<FullCellAddr> act) {
      CellAddr ulCa = ul.Addr(col, row), lrCa = lr.Addr(col, row);
      ArrayView.Make(ulCa, lrCa, this.sheet ?? sheet).Apply(act);
    }

    // Clone and move (when the containing formula is moved, not copied)
    public override Expr Move(int deltaCol, int deltaRow) {
      return new CellArea(sheet,
                          ul.Move(deltaCol, deltaRow),
                          lr.Move(deltaCol, deltaRow));
    }

    // Can copy cell area with sharing iff corners are within sheet
    public override Expr CopyTo(int col, int row) {
      if (ul.ValidAt(col, row) && lr.ValidAt(col, row))
        return this;
      else
        return Error.refError;
    }

    public override Adjusted<Expr> InsertRowCols(Sheet modSheet, bool thisSheet,
                                                 int R, int N, int r,
                                                 bool doRows) {
      if (sheet == modSheet || sheet == null && thisSheet) {
        Adjusted<RARef> ulNew = ul.InsertRowCols(R, N, r, doRows),
                        lrNew = lr.InsertRowCols(R, N, r, doRows);
        int upper = Math.Min(ulNew.upper, lrNew.upper);
        return new Adjusted<Expr>(new CellArea(sheet, ulNew.e, lrNew.e),
                                  upper, ulNew.same && lrNew.same);
      } else
        return new Adjusted<Expr>(this);
    }

    internal void AddToSupport(Sheet supported, int col, int row, int cols, int rows) {
      Sheet referredSheet = this.sheet ?? supported;
      Interval referredRows, referredCols;
      Func<int, Interval> supportedRows, supportedCols;
      int ra = ul.rowRef, rb = lr.rowRef, r1 = row, r2 = row + rows - 1;
      RefAndSupp(ul.rowAbs, lr.rowAbs, ra, rb, r1, r2, out referredRows, out supportedRows);
      int ca = ul.colRef, cb = lr.colRef, c1 = col, c2 = col + cols - 1;
      RefAndSupp(ul.colAbs, lr.colAbs, ca, cb, c1, c2, out referredCols, out supportedCols);
      // Outer iteration should be over the shorter interval for efficiency
      if (referredCols.Length < referredRows.Length)
        referredCols.ForEach(c => {
          Interval suppCols = supportedCols(c);
          referredRows.ForEach(r =>
            referredSheet.AddSupport(c, r, supported, suppCols, supportedRows(r)));
        });
      else
        referredRows.ForEach(r => {
          Interval suppRows = supportedRows(r);
          referredCols.ForEach(c =>
            referredSheet.AddSupport(c, r, supported, supportedCols(c), suppRows));
        }); 
    }

    // This uses notation from the book's discussion of the row dimension of support sets,
    // works equally well for the column dimension.
    // Assumes r1 <= r2 but nothing about the order of ra and rb
    private static void RefAndSupp(bool ulAbs, bool lrAbs, int ra, int rb, int r1, int r2,
        out Interval referred, out Func<int, Interval> supported) {
      if (ulAbs) {
        if (lrAbs) { // case abs-abs
          SortInts(ref ra, ref rb);
          referred = new Interval(ra, rb);
          supported = r => new Interval(r1, r2);
        } else {     // case abs-rel
          referred = new Interval(Math.Min(ra, r1 + rb), Math.Max(ra, r2 + rb));
          supported = r => ra < r ? new Interval(Math.Max(r1, r - rb), r2)
                         : ra > r ? new Interval(r1, Math.Min(r2, r - rb))
                                  : new Interval(r1, r2);
        }
      } else {
        if (lrAbs) { // case rel-abs
          referred = new Interval(Math.Min(r1 + ra, rb), Math.Max(r2 + ra, rb));
          supported = r => rb > r ? new Interval(r1, Math.Min(r2, r - ra))
                         : rb < r ? new Interval(Math.Max(r1, r - ra), r2)
                                  : new Interval(r1, r2);
        } else {     // case rel-rel
          SortInts(ref ra, ref rb);
          referred = new Interval(r1 + ra, r2 + rb);
          supported = r => new Interval(Math.Max(r1, r - rb), Math.Min(r2, r - ra));
        }
      }
    }

    private static void SortInts(ref int a, ref int b) {
      if (a > b) {
        int tmp = a; a = b; b = tmp;
      }
    }

    internal override void VisitRefs(RefSet refSet, Action<CellRef> refAct, Action<CellArea> areaAct) 
    {
      if (!refSet.SeenBefore(this))
        areaAct(this);
    }

    public override void DependsOn(FullCellAddr here, Action<FullCellAddr> dependsOn) {
      ApplyToFcas(here.sheet, here.ca.col, here.ca.row, dependsOn);
    }

    public override bool IsVolatile {
      get { return false; }
    }

    public bool Equals(CellArea that) {
      return that != null && this.ul.Equals(that.ul) && this.lr.Equals(that.lr);
    }

    public override int GetHashCode() {
      return lr.GetHashCode() * 511 + ul.GetHashCode();
    } 

    public override String Show(int col, int row, int ctxpre, Formats fo) {
      String s = ul.Show(col, row, fo) + ":" + lr.Show(col, row, fo);
      return sheet == null ? s : sheet.Name + "!" + s;
    }

    internal override void VisitorCall(IExpressionVisitor visitor) {
      visitor.CallVisitor(this);
    }
  }

  /// <summary>
  /// An IExpressionVisitor is used to traverse the Expr abstract syntax used in formulas.
  /// </summary>
  interface IExpressionVisitor {
    void CallVisitor(NumberConst numbConst);
    void CallVisitor(TextConst textConst);
    void CallVisitor(ValueConst valueConst);
    void CallVisitor(Error expr);
    void CallVisitor(FunCall funCall);
    void CallVisitor(CellRef cellRef);
    void CallVisitor(CellArea cellArea);
  }

  /// <summary>
  /// A RefSet is a set of CellRefs and CellAreas already seen by a VisitRefs visitor.
  /// </summary>
  internal class RefSet {
    private readonly HashSet<CellRef> cellRefsSeen = new HashSet<CellRef>();
    private readonly HashSet<CellArea> cellAreasSeen = new HashSet<CellArea>();

    public void Clear() {
      cellRefsSeen.Clear();
      cellAreasSeen.Clear();
    }

    public bool SeenBefore(CellRef cellRef) {
      return !cellRefsSeen.Add(cellRef);
    }

    public bool SeenBefore(CellArea cellArea) {
      return !cellAreasSeen.Add(cellArea);
    }
  }
}
