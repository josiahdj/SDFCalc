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
using System.Reflection.Emit;
using System.Text;
using System.Diagnostics;
using System.Linq;

namespace Corecalc.Funcalc {
  /// <summary>
  /// A ProgramLines object holds the cells involved in the definitions of 
  /// a particular sheet-defined function, including its output cell, its input
  /// cells, all intermediate cells as a topologically sorted list of ComputeCells,
  /// and further data used during code generation and partial evaluation.
  /// </summary>
  public class ProgramLines : CodeGenerate {
    private readonly FullCellAddr outputCell;
    private readonly FullCellAddr[] inputCells;
    // Code to unwrap input cells to float64 when expedient:
    private readonly List<UnwrapInputCell> unwrapInputCells;
    // Code to evaluate all cells in order, ending with the output cell:
    private readonly List<ComputeCell> programList;
    // Map intermediate cell (address) to the formula in that cell:
    // Invariant: fcaToComputeCell[fca].cellAddr == fca
    private readonly IDictionary<FullCellAddr, ComputeCell> fcaToComputeCell;
    // The following dictionary maps both input cells and intermediate cells:
    // Invariant: fcaToComputeCell[fca].var == addressToVariable[fca] when both defined
    public readonly Dictionary<FullCellAddr, Variable> addressToVariable;
    // The list of CGCachedExpr used in evaluation conditions of this program
    private readonly List<CGCachedExpr> caches = new List<CGCachedExpr>();

    public ProgramLines(FullCellAddr outputCell, FullCellAddr[] inputCells) {
      this.outputCell = outputCell;
      this.inputCells = inputCells;
      programList = new List<ComputeCell>();
      fcaToComputeCell = new Dictionary<FullCellAddr, ComputeCell>();
      unwrapInputCells = new List<UnwrapInputCell>();
      addressToVariable = new Dictionary<FullCellAddr, Variable>();
      for (short i = 0; i < inputCells.Length; i++) {
        FullCellAddr addr = inputCells[i];
        addressToVariable.Add(addr, new LocalArgument(addr.ToString(), Typ.Value, i));
      }
    }

    public static Delegate CreateSdfDelegate(SdfInfo sdfInfo, DependencyGraph dpGraph, IList<FullCellAddr> cellList) {
      Debug.Assert(sdfInfo.inputCells == dpGraph.inputCells);
      Debug.Assert(sdfInfo.outputCell == dpGraph.outputCell);
      ProgramLines program = new ProgramLines(sdfInfo.outputCell, sdfInfo.inputCells);
      program.AddComputeCells(dpGraph, cellList);  
      // TODO: This is not the final program, so order may not respect eval cond dependencies!
      sdfInfo.Program = program;  // Save ComputeCell list for later partial evaluation
      return program.CompileToDelegate(sdfInfo);
    }

    public Delegate CompileToDelegate(SdfInfo sdfInfo) {
      Debug.Assert(sdfInfo.Program == this);  // Which is silly, lots of redundancy
      // Create dynamic method with signature: Value CGMethod(Value, Value, ...) in class Function:
      DynamicMethod method = new DynamicMethod("CGMethod", Value.type, sdfInfo.MyArgumentTypes,
                                               Function.type, true);
      ILGenerator ilg = method.GetILGenerator();
      CodeGenerate.Initialize(ilg);
      sdfInfo.Program.EvalCondReorderCompile();
      ilg.Emit(OpCodes.Ret);
      return method.CreateDelegate(sdfInfo.MyType);
    }

    /// <summary>
    /// Compiles the topologically sorted list of Expr to a list (program) 
    /// of ComputeCells, encapsulating CGExprs.  Builds a map from cellAddr to 
    /// local variable ids, for compiling sheet-internal cellrefs to ldloc instructions.  
    /// </summary>
    public void AddComputeCells(DependencyGraph dpGraph, IList<FullCellAddr> cellList) {
      Debug.Assert(dpGraph.outputCell == cellList[cellList.Count - 1]);
      CGExpr outputExpr;
      if (cellList.Count == 0 || cellList.Count == 1 && dpGraph.inputCellSet.Contains(cellList.Single()))
        // The output cell is also an input cell; load it:
        outputExpr = new CGCellRef(dpGraph.outputCell, addressToVariable[dpGraph.outputCell]);
      else {
        // First process all non-output cells, and ignore all input cells:
        foreach (FullCellAddr cellAddr in cellList) {
          if (cellAddr.Equals(dpGraph.outputCell))
            continue;
          HashSet<FullCellAddr> dependents = dpGraph.GetDependents(cellAddr);
          int minUses = dependents.Count;
          if (minUses == 1) {
            FullCellAddr fromFca = dependents.First();
            minUses = Math.Max(minUses, GetCount(fromFca, cellAddr));
          }
          // Now if minUses==1 then there is at most one use of the cell at cellAddr,
          // and no local variable is needed.  Otherwise, allocate a local variable:
          if (minUses > 1) {
            CGExpr newExpr = CGExpressionBuilder.BuildExpression(cellAddr, addressToVariable);
            Variable var = new LocalVariable(cellAddr.ToString(), newExpr.Type());
            AddComputeCell(cellAddr, new ComputeCell(newExpr, var, cellAddr));
          }
        }
        // Then process the output cell:
        outputExpr = CGExpressionBuilder.BuildExpression(dpGraph.outputCell, addressToVariable);
      }
      // Add the output cell expression last, without a variable to bind it to; hence the null,
      // also indicating that (only) the output cell is in tail position:
      AddComputeCell(dpGraph.outputCell, new ComputeCell(outputExpr, null, dpGraph.outputCell));
    }

    public void AddComputeCell(FullCellAddr fca, ComputeCell ccell) {
      programList.Add(ccell);
      fcaToComputeCell.Add(fca, ccell);
      if (ccell.var != null)
        addressToVariable.Add(fca, ccell.var);
    }

    public void Compile() {
      foreach (UnwrapInputCell uwic in unwrapInputCells)
        uwic.Compile();
      foreach (ComputeCell expr in programList)
        expr.Compile();
    }

    public FullCellAddr[] ResidualInputs(FunctionValue fv) {
      // The residual input cells are those that have input value NA
      FullCellAddr[] residualInputs = new FullCellAddr[fv.Arity];
      int j = 0;
      for (int i = 0; i < fv.args.Length; i++)
        if (fv.args[i] == ErrorValue.naError)
          residualInputs[j++] = inputCells[i];
      return residualInputs;
    }

    // Partially evaluate the programList with respect to the given static inputs, 
    // producing a new ProgramLines object.
    public ProgramLines PEval(Value[] args, FullCellAddr[] residualInputs) {
      PEnv pEnv = new PEnv();
      // Map static input cells to their constant values:
      for (int i = 0; i < args.Length; i++)
        pEnv[inputCells[i]] = CGConst.Make(args[i]);
      ProgramLines residual = new ProgramLines(outputCell, residualInputs);
      // PE-time environment PEnv maps each residual input cell address to the delegate argument:
      for (int i = 0; i < residualInputs.Length; i++) {
        FullCellAddr input = residualInputs[i];
        pEnv[input] = new CGCellRef(input, residual.addressToVariable[input]);
      }
      // Process the given function's compute cells in dependency order, output last:
      foreach (ComputeCell ccell in programList) {
        ComputeCell rCcell = ccell.PEval(pEnv);
        if (rCcell != null)
          residual.AddComputeCell(ccell.cellAddr, rCcell);
      }
      residual = residual.PruneZeroUseCells();
      return residual;
    }

    private ProgramLines PruneZeroUseCells() {
      // This is slightly more general than necessary, since we know that the 
      // new order of FullCellAddrs could be embedded in the old one.  So it would suffice
      // to simply count the number of uses of each FullCellAddr rather than do this sort.
      DependencyGraph dpGraph = new DependencyGraph(outputCell, inputCells, GetComputeCell);
      IList<FullCellAddr> prunedList = dpGraph.PrecedentOrder();
      ProgramLines prunedProgram = new ProgramLines(outputCell, inputCells);
      foreach (FullCellAddr cellAddr in prunedList)
        prunedProgram.AddComputeCell(cellAddr, GetComputeCell(cellAddr));
      return prunedProgram;
    }

    /// CodeGenerate.Initialize(ilg) must be called first.
    public void EvalCondReorderCompile() {
      ComputeEvalConds();
      // Re-sort the expressions to reflect new dependencies 
      // introduced by evaluation conditions
      DependencyGraph augmentedGraph
        = new DependencyGraph(outputCell, inputCells, GetComputeCell);
      IList<FullCellAddr> augmentedList = augmentedGraph.PrecedentOrder(); 
      ProgramLines finalProgram = new ProgramLines(outputCell, inputCells);
      foreach (FullCellAddr cellAddr in augmentedList)
        finalProgram.AddComputeCell(cellAddr, GetComputeCell(cellAddr));
      // This relies on all pathconds having been generated at this point:
      EmitCacheInitializations();
      finalProgram.CreateUnwrappedNumberCells();
      finalProgram.Compile();
    }

    /// <summary>
    /// Insert code to unwrap the computed value of a cell, if the cell 
    /// has type Value but is referred to as a Number more than once.
    /// Also register the unwrapped version of the variable 
    /// in the NumberVariables dictionary.
    /// CodeGenerate.Initialize(ilg) must be called first.
    /// </summary>
    public void CreateUnwrappedNumberCells() {
      HashBag<FullCellAddr> numberUses = CountNumberUses();
      foreach (KeyValuePair<FullCellAddr, int> numberUseCount in numberUses.ItemMultiplicities()) {
        FullCellAddr fca = numberUseCount.Key;
        if (numberUseCount.Value >= 2 && addressToVariable[fca].Type == Typ.Value) {
          Variable numberVar = new LocalVariable(fca + "_number", Typ.Number);
          ComputeCell ccell;
          if (fcaToComputeCell.TryGetValue(fca, out ccell)) // fca is ordinary computed cell
            ccell.NumberVar = numberVar;
          else // fca is an input cell
            unwrapInputCells.Add(new UnwrapInputCell(addressToVariable[fca], numberVar));
          NumberVariables.Add(fca, numberVar);
        }
      }
    }

    /// <summary>
    /// Count number of references from cell at fromFca to cell address toFca
    /// </summary>
    private static int GetCount(FullCellAddr fromFca, FullCellAddr toFca) {
      int count = 0;
      Cell fromCell;
      if (fromFca.TryGetCell(out fromCell))
        fromCell.DependsOn(fromFca,
          delegate(FullCellAddr fca) { if (toFca.Equals(fca)) count++; });
      return count;
    }

    public FullCellAddr[] InputCells {
      get { return inputCells; }
    }

    public HashBag<FullCellAddr> CountNumberUses() {
      var numberUses = new HashBag<FullCellAddr>();
      foreach (ComputeCell ccell in programList)
        ccell.CountUses(ccell.Type, numberUses);
      return numberUses;
    }

    public override string ToString() {
      StringBuilder sb = new StringBuilder();
      int counter = 0;
      foreach (ComputeCell expr in programList)
        sb.Append(counter++).Append("0: ").AppendLine(expr.ToString());
      return sb.ToString();
    }

    public void ComputeEvalConds() {
      const int THRESHOLD = 30;
      // Compute evaluation condition for each cell
      IDictionary<FullCellAddr, PathCond> evalConds = new Dictionary<FullCellAddr, PathCond>();
      evalConds[outputCell] = PathCond.TRUE;
      // The outputCell is also the first ccell processed below
      for (int i = programList.Count - 1; i >= 0; i--) {
        ComputeCell ccell = programList[i];
        int bound = THRESHOLD;
        bool isSerious = ccell.expr.IsSerious(ref bound);
        PathCond evalCond = evalConds[ccell.cellAddr];
        // Console.WriteLine("evalConds[{0}{1}] = {2}\n", casv.cellAddr, isSerious ? "" : ":TRIVIAL", evalCond);
        if (isSerious && !evalCond.Is(true)) {
          Console.WriteLine("Setting EvalCond[{0}] = {1}", ccell.cellAddr, evalCond);
          ccell.EvalCond = evalCond.ToCGExpr();
        }
        ccell.expr.EvalCond(evalCond, evalConds, caches);
      }
    }

    /// CodeGenerate.Initialize(ilg) must be called first.
    public void EmitCacheInitializations() {
      foreach (CGCachedExpr cache in caches)
        cache.EmitCacheInitialization();
    }

    public ComputeCell GetComputeCell(FullCellAddr fca) {
      return fcaToComputeCell[fca];
    }
  }

  /// <summary>
  /// A ComputeCell represents a (intermediate or output) cell of a sheet-defined 
  /// function and its associated IL local variable (if intermediate cell), the
  /// cell's evaluation condition, if any, and a double-type IL local variable if
  /// the cell is known to be number-valued.
  /// </summary> 
  public class ComputeCell : CodeGenerate, IDepend {
    public readonly CGExpr expr;
    public readonly Variable var; // Null only for the unique output cell
    public readonly FullCellAddr cellAddr;
    private CGExpr evalCond;      // If non-null, then conditional evaluation
    private Variable numberVar;   // If non-null, unwrap to this Number variable
    // If var==null then numberVar==null

    public ComputeCell(CGExpr expr, Variable var, FullCellAddr cellAddr) {
      this.expr = expr;
      this.var = var;
      // The output cell's expression is in tail position:
      if (var == null)
        this.expr.NoteTailPosition();
      this.cellAddr = cellAddr;
      this.numberVar = null;
    }

    public CGExpr EvalCond {
      get { return evalCond; }
      set { this.evalCond = value; }
    }

    public Variable NumberVar {
      set { this.numberVar = value; }
    }
  
    /// <summary>
    /// Generate code to compute the expression's value and storing it in the IL
    /// local variable; possibly to unwrap to a number variable; and possibly under
    /// the control of an evaluation condition evalCond.
    /// </summary>
    public virtual void Compile() {
      EvalCondCompile(delegate {
        if (var != null && var.Type == Typ.Number)
          expr.CompileToDoubleOrNan();
        else
          expr.Compile();
        if (var != null)
          var.EmitStore(ilg);
        if (numberVar != null) {
          Debug.Assert(var.Type == Typ.Value);
          var.EmitLoad(ilg);
          UnwrapToDoubleOrNan();
          numberVar.EmitStore(ilg);
        }
      });
    }

    protected void EvalCondCompile(Action compile) {
      if (evalCond != null)
        evalCond.CompileToDoubleProper(
          new Gen(delegate {
            Label endLabel = ilg.DefineLabel();
            ilg.Emit(OpCodes.Ldc_R8, 0.0);
            ilg.Emit(OpCodes.Beq, endLabel);
            compile();
            ilg.MarkLabel(endLabel);
          }),
          new Gen(delegate { }));
      else
        compile();
    }

    public virtual void DependsOn(FullCellAddr here, Action<FullCellAddr> dependsOn) {
      expr.DependsOn(here, dependsOn);
      if (evalCond != null)
        evalCond.DependsOn(here, dependsOn);
    }

    public void CountUses(Typ typ, HashBag<FullCellAddr> numberUses) {
      expr.CountUses(typ, numberUses);
      if (evalCond != null)
        evalCond.CountUses(Typ.Number, numberUses);
    }

    // Returns residual ComputeCell or null if no cell needed
    public ComputeCell PEval(PEnv pEnv) {
      CGExpr rCond = null;
      if (evalCond != null) // Never the case for an output cell
        rCond = evalCond.PEval(pEnv, false /* not dynamic control */);
      if (rCond is CGNumberConst)
        if ((rCond as CGNumberConst).number.value != 0.0)
          rCond = null;     // eval cond constant TRUE, discard eval cond
        else
          return null;      // eval cond constant FALSE, discard entire compute cell
      // If residual eval cond is not TRUE then expr has dynamic control
      CGExpr rExpr = expr.PEval(pEnv, rCond != null);
      if (rExpr is CGConst && var != null) {
        // If cell's value is constant and it is not an output cell just put in PEnv
        pEnv[cellAddr] = rExpr;
        return null;
      } else {
        // Else create fresh local variable for the residual cell, and make 
        // PEnv map cell address to that local variable:          
        Variable newVar = var != null ? var.Fresh() : null;
        pEnv[cellAddr] = new CGCellRef(cellAddr, newVar);
        ComputeCell result = new ComputeCell(rExpr, newVar, cellAddr);
        // result.EvalCond = rCond;  // Don't save residual eval cond, we compute it accurately later...
        return result;
      }
    }

    public Typ Type {
      // For an output cell (only) the expr.Type() may be Number whereas
      // the enclosing cell naturally has type Value.  This makes a difference 
      // if that cell contains IF(..., A1, 17) because then A1 appears in a 
      // context that expects a Number.  But this is so only if A1 has type number, 
      // and hence no unwrapping is needed.  Hence it seems safe to always use 
      // var.Type instead of recomputing expr.Type(). 
      get { return var != null ? var.Type : Typ.Value; }
      // Alternatively: return expr.Type();
    }

    public override string ToString() {
      StringBuilder sb = new StringBuilder();
      if (evalCond != null)
        sb.AppendFormat("if ({0}) {\n", evalCond);
      sb.AppendFormat("{0} = {1}", (var != null ? var.Name : "<output>"), expr);
      if (numberVar != null)
        sb.AppendFormat("\n{0} = UnwrapToDoubleOrNan{1}", numberVar.Name, var.Name);
      if (evalCond != null)
        sb.AppendFormat("\n}");
      return sb.ToString();
    }
  }

  /// <summary>
  /// An UnwrapInputCell represents the action, in a program list, to 
  /// unwrap an input cell inputVar of type Value to a numberVar of type Number.
  /// </summary>
  public class UnwrapInputCell : CodeGenerate {
    public readonly Variable inputVar, numberVar;

    public UnwrapInputCell(Variable inputVar, Variable numberVar) {
      this.inputVar = inputVar;
      this.numberVar = numberVar;
    }

    public void Compile() {
      Debug.Assert(inputVar.Type == Typ.Value);
      Debug.Assert(numberVar.Type == Typ.Number);
      inputVar.EmitLoad(ilg);
      UnwrapToDoubleOrNan();
      numberVar.EmitStore(ilg);
    }

    public override string ToString() {
      return String.Format("{0} = UnwrapToDoubleOrNan({1})", numberVar.Name, inputVar.Name);
    }
  }
}
