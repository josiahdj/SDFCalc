using System;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Text;

using CoreCalc.CellAddressing;
using CoreCalc.Types;

namespace Corecalc.Funcalc {
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
		private CGExpr evalCond; // If non-null, then conditional evaluation
		private Variable numberVar; // If non-null, unwrap to this Number variable
		// If var==null then numberVar==null

		public ComputeCell(CGExpr expr, Variable var, FullCellAddr cellAddr) {
			this.expr = expr;
			this.var = var;
			// The output cell's expression is in tail position:
			if (var == null) {
				this.expr.NoteTailPosition();
			}
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
								if (var != null && var.Type == Typ.Number) {
									expr.CompileToDoubleOrNan();
								}
								else {
									expr.Compile();
								}
								if (var != null) {
									var.EmitStore(ilg);
								}
								if (numberVar != null) {
									Debug.Assert(var.Type == Typ.Value);
									var.EmitLoad(ilg);
									UnwrapToDoubleOrNan();
									numberVar.EmitStore(ilg);
								}
							});
		}

		protected void EvalCondCompile(Action compile) {
			if (evalCond != null) {
				evalCond.CompileToDoubleProper(
											   new Gen(delegate {
														   Label endLabel = ilg.DefineLabel();
														   ilg.Emit(OpCodes.Ldc_R8, 0.0);
														   ilg.Emit(OpCodes.Beq, endLabel);
														   compile();
														   ilg.MarkLabel(endLabel);
													   }),
											   new Gen(delegate { }));
			}
			else {
				compile();
			}
		}

		public virtual void DependsOn(FullCellAddr here, Action<FullCellAddr> dependsOn) {
			expr.DependsOn(here, dependsOn);
			if (evalCond != null) {
				evalCond.DependsOn(here, dependsOn);
			}
		}

		public void CountUses(Typ typ, HashBag<FullCellAddr> numberUses) {
			expr.CountUses(typ, numberUses);
			if (evalCond != null) {
				evalCond.CountUses(Typ.Number, numberUses);
			}
		}

		// Returns residual ComputeCell or null if no cell needed
		public ComputeCell PEval(PEnv pEnv) {
			CGExpr rCond = null;
			if (evalCond != null) // Never the case for an output cell
			{
				rCond = evalCond.PEval(pEnv, false /* not dynamic control */);
			}
			if (rCond is CGNumberConst) {
				if ((rCond as CGNumberConst).number.value != 0.0) {
					rCond = null; // eval cond constant TRUE, discard eval cond
				}
				else {
					return null; // eval cond constant FALSE, discard entire compute cell
				}
			}
			// If residual eval cond is not TRUE then expr has dynamic control
			CGExpr rExpr = expr.PEval(pEnv, rCond != null);
			if (rExpr is CGConst && var != null) {
				// If cell's value is constant and it is not an output cell just put in PEnv
				pEnv[cellAddr] = rExpr;
				return null;
			}
			else {
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
			if (evalCond != null) {
				sb.AppendFormat("if ({0}) {\n", evalCond);
			}
			sb.AppendFormat("{0} = {1}", (var != null ? var.Name : "<output>"), expr);
			if (numberVar != null) {
				sb.AppendFormat("\n{0} = UnwrapToDoubleOrNan{1}", numberVar.Name, var.Name);
			}
			if (evalCond != null) {
				sb.AppendFormat("\n}");
			}
			return sb.ToString();
		}
	}
}