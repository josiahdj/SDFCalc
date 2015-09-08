using System;
using System.Linq;
using System.Text;

namespace Corecalc {
	/// <summary>
	/// A FunCall expression is an operator application such as 1+$A$4 or a function
	/// call such as RAND() or SIN(4*A$7) or SUM(B4:B52; 3) or IF(A1; A2; 1/A1).
	/// </summary>
	internal class FunCall : Expr {
		public readonly Function function; // Non-null
		public readonly Expr[] es; // Non-null, elements non-null

		private FunCall(String name, params Expr[] es)
			: this(Function.Get(name), es) {}

		private FunCall(Function function, Expr[] es) {
			// Assert: function != null, all es[i] != null
			this.function = function;
			this.es = es;
		}

		public static Expr Make(String name, Expr[] es) {
			Function function = Function.Get(name);
			if (function == null) {
				function = Function.MakeUnknown(name);
			}
			for (int i = 0; i < es.Length; i++) {
				if (es[i] == null) {
					es[i] = new Error("#SYNTAX");
				}
			}
			if (name == "SPECIALIZE" && es.Length > 1) {
				return new FunCall("SPECIALIZE", Make("CLOSURE", es));
			}
			else {
				return new FunCall(function, es);
			}
		}

		// Arguments are passed unevaluated to cater for non-strict IF 

		public override Value Eval(Sheet sheet, int col, int row) { return function.Applier(sheet, es, col, row); }

		public override Expr Move(int deltaCol, int deltaRow) {
			Expr[] newEs = new Expr[es.Length];
			for (int i = 0; i < es.Length; i++) {
				newEs[i] = es[i].Move(deltaCol, deltaRow);
			}
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

		public override Adjusted<Expr> InsertRowCols(Sheet modSheet,
													 bool thisSheet,
													 int R,
													 int N,
													 int r,
													 bool doRows) {
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
					if (i > 0) {
						sb.Append(", ");
					}
					sb.Append(es[i].Show(col, row, 0, fo));
				}
				sb.Append(")");
			}
			else { // Operator.  Assume es.Length is 1 or 2 
				if (es.Length == 2) {
					// If precedence lower than context, add parens
					if (pre < ctxpre) {
						sb.Append("(");
					}
					sb.Append(es[0].Show(col, row, pre, fo));
					sb.Append(function.name);
					// Only higher precedence right operands avoid parentheses
					sb.Append(es[1].Show(col, row, pre + 1, fo));
					if (pre < ctxpre) {
						sb.Append(")");
					}
				}
				else if (es.Length == 1) {
					sb.Append(function.name == "NEG" ? "-" : function.name);
					sb.Append(es[0].Show(col, row, pre, fo));
				}
				else {
					throw new ImpossibleException("Operator not unary or binary");
				}
			}
			return sb.ToString();
		}

		internal override void VisitRefs(RefSet refSet, Action<CellRef> refAct, Action<CellArea> areaAct) {
			foreach (Expr e in es) {
				e.VisitRefs(refSet, refAct, areaAct);
			}
		}

		public override void DependsOn(FullCellAddr here, Action<FullCellAddr> dependsOn) {
			foreach (Expr e in es) {
				e.DependsOn(here, dependsOn);
			}
		}

		public override bool IsVolatile {
			get {
				if (function.IsVolatile(es)) {
					return true;
				}
				return es.Any(e => e.IsVolatile);
			}
		}

		internal override void VisitorCall(IExpressionVisitor visitor) { visitor.CallVisitor(this); }
	}
}