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
using System.Diagnostics;
using System.Text;

namespace Corecalc.Funcalc {
  /// <summary>
  /// A CGExpressionBuilder is an expression visitor that builds a CGExpr 
  /// from the expression.
  /// </summary>
  class CGExpressionBuilder : IExpressionVisitor {
    private readonly Dictionary<FullCellAddr, Variable> addressToVariable;
    private FullCellAddr thisFca; // The cell containing the Expr being translated
    private CGExpr result;        // The result of the compilation is left here

    private CGExpressionBuilder(Dictionary<FullCellAddr, Variable> addressToVariable, 
                                FullCellAddr addr) 
    {
      thisFca = addr;
      this.addressToVariable = addressToVariable;
    }

    public static CGExpr BuildExpression(FullCellAddr addr,
      Dictionary<FullCellAddr, Variable> addressToVariable) 
    {
      Cell cell;
      if (!addr.TryGetCell(out cell))
        return new CGTextConst(TextValue.EMPTY);
      else if (cell is NumberCell)
        return new CGNumberConst(((NumberCell)cell).value);
      else if (cell is TextCell)
        return new CGTextConst(((TextCell)cell).value);
      else if (cell is QuoteCell)
        return new CGTextConst(((QuoteCell)cell).value);
      else if (cell is BlankCell)
        return new CGError("#FUNERR: Blank cell in function");
      else if (cell is Formula) {
        // Translate the expr relative to its containing cell at addr
        CGExpressionBuilder cgBuilder = new CGExpressionBuilder(addressToVariable, addr);
        Expr expr = ((Formula)cell).Expr;
        expr.VisitorCall(cgBuilder);
        return cgBuilder.result;
      } else if (cell is ArrayFormula)
        return new CGError("#FUNERR: Array formula in function");
      else
        throw new ImpossibleException("BuildExpression: " + cell);
    }

    public void CallVisitor(CellArea cellArea) {
      result = new CGNormalCellArea(cellArea.MakeArrayView(thisFca));
    }

    public void CallVisitor(CellRef cellRef) {
      FullCellAddr cellAddr = cellRef.GetAbsoluteAddr(thisFca);
      if (cellAddr.sheet != thisFca.sheet)
        // Reference to other sheet, hopefully a normal sheet
        result = new CGNormalCellRef(cellAddr);
      else if (this.addressToVariable.ContainsKey(cellAddr))
        // Reference to a cell that has already been computed in a local variable
        result = new CGCellRef(cellAddr, this.addressToVariable[cellAddr]);
      else // Inline the cell's formula's expression
        result = BuildExpression(cellAddr, addressToVariable);
    }

    public void CallVisitor(FunCall funCall) {
      CGExpr[] expressions = new CGExpr[funCall.es.Length];
      for (int i = 0; i < funCall.es.Length; i++) {
        funCall.es[i].VisitorCall(this);
        expressions[i] = result;
      }
      result = CGComposite.Make(funCall.function.name, expressions);
    }

    public void CallVisitor(Error error) {
      result = new CGError(error.value);
    }

    public void CallVisitor(NumberConst numbConst) {
      result = new CGNumberConst(numbConst.value);
    }

    public void CallVisitor(TextConst textConst) {
      result = new CGTextConst(textConst.value);
    }

    public void CallVisitor(ValueConst valueConst) {
      result = new CGValueConst(valueConst.value);
    }
  }
}
