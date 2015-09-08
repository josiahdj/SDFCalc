using System.Collections.Generic;
using System;

namespace Corecalc {
	public class Parser {
		public const int _EOF = 0;
		public const int _name = 1;
		public const int _number = 2;
		public const int _datetime = 3;
		public const int _sheetref = 4;
		public const int _raref = 5;
		public const int _xmlssraref11 = 6;
		public const int _xmlssraref12 = 7;
		public const int _xmlssraref13 = 8;
		public const int _xmlssraref21 = 9;
		public const int _xmlssraref22 = 10;
		public const int _xmlssraref23 = 11;
		public const int _xmlssraref31 = 12;
		public const int _xmlssraref32 = 13;
		public const int _xmlssraref33 = 14;
		public const int _string = 15;
		public const int _quotecell = 16;
		public const int maxT = 34;

		private const bool T = true;
		private const bool x = false;
		private const int minErrDist = 2;

		public Scanner scanner;
		public Errors errors;

		public Token t; // last recognized token
		public Token la; // lookahead token
		private int errDist = minErrDist;

		private int col, row;
		private Workbook workbook;
		private Cell cell;
		private static System.Globalization.NumberFormatInfo numberFormat = null;

		static Parser() {
			// Set US/UK decimal point, regardless of culture
			System.Globalization.CultureInfo ci =
				System.Globalization.CultureInfo.InstalledUICulture;
			numberFormat = (System.Globalization.NumberFormatInfo)ci.NumberFormat.Clone();
			numberFormat.NumberDecimalSeparator = ".";
		}

		public Cell ParseCell(Workbook workbook, int col, int row) {
			this.workbook = workbook;
			this.col = col;
			this.row = row;
			Parse();
			return errors.count == 0 ? cell : null;
		}

/*--------------------------------------------------------------------------*/


		public Parser(Scanner scanner) {
			this.scanner = scanner;
			errors = new Errors();
		}

		private void SynErr(int n) {
			if (errDist >= minErrDist) {
				errors.SynErr(la.line, la.col, n);
			}
			errDist = 0;
		}

		public void SemErr(string msg) {
			if (errDist >= minErrDist) {
				errors.SemErr(t.line, t.col, msg);
			}
			errDist = 0;
		}

		private void Get() {
			for (;;) {
				t = la;
				la = scanner.Scan();
				if (la.kind <= maxT) {
					++errDist;
					break;
				}

				la = t;
			}
		}

		private void Expect(int n) {
			if (la.kind == n) {
				Get();
			}
			else {
				SynErr(n);
			}
		}

		private bool StartOf(int s) { return set[s, la.kind]; }

		private void ExpectWeak(int n, int follow) {
			if (la.kind == n) {
				Get();
			}
			else {
				SynErr(n);
				while (!StartOf(follow)) {
					Get();
				}
			}
		}


		private bool WeakSeparator(int n, int syFol, int repFol) {
			int kind = la.kind;
			if (kind == n) {
				Get();
				return true;
			}
			else if (StartOf(repFol)) {
				return false;
			}
			else {
				SynErr(n);
				while (!(set[syFol, kind] || set[repFol, kind] || set[0, kind])) {
					Get();
					kind = la.kind;
				}
				return StartOf(syFol);
			}
		}


		private void AddOp(out String op) {
			op = "+";
			if (la.kind == 17) {
				Get();
			}
			else if (la.kind == 18) {
				Get();
				op = "-";
			}
			else if (la.kind == 19) {
				Get();
				op = "&";
			}
			else {
				SynErr(35);
			}
		}

		private void LogicalOp(out String op) {
			op = "=";
			switch (la.kind) {
				case 20: {
					Get();
					break;
				}
				case 21: {
					Get();
					op = "<>";
					break;
				}
				case 22: {
					Get();
					op = "<";
					break;
				}
				case 23: {
					Get();
					op = "<=";
					break;
				}
				case 24: {
					Get();
					op = ">";
					break;
				}
				case 25: {
					Get();
					op = ">=";
					break;
				}
				default:
					SynErr(36);
					break;
			}
		}

		private void Expr(out Expr e) {
			Expr e2;
			String op;
			e = null;
			LogicalTerm(out e);
			while (StartOf(1)) {
				LogicalOp(out op);
				LogicalTerm(out e2);
				e = FunCall.Make(op, new Expr[] {e, e2});
			}
		}

		private void LogicalTerm(out Expr e) {
			Expr e2;
			String op;
			e = null;
			Term(out e);
			while (la.kind == 17 || la.kind == 18 || la.kind == 19) {
				AddOp(out op);
				Term(out e2);
				e = FunCall.Make(op, new Expr[] {e, e2});
			}
		}

		private void Term(out Expr e) {
			Expr e2;
			String op;
			PowFactor(out e);
			while (la.kind == 32 || la.kind == 33) {
				MulOp(out op);
				PowFactor(out e2);
				e = FunCall.Make(op, new Expr[] {e, e2});
			}
		}

		private void Factor(out Expr e) {
			RARef r1, r2;
			Sheet s1 = null;
			double d;
			bool sheetError = false;
			e = null;
			switch (la.kind) {
				case 1: {
					Application(out e);
					break;
				}
				case 4:
				case 5:
				case 6:
				case 7:
				case 8:
				case 9:
				case 10:
				case 11:
				case 12:
				case 13:
				case 14: {
					if (StartOf(2)) {}
					else {
						Get();
						s1 = workbook[t.val.Substring(0, t.val.Length - 1)];
						if (s1 == null) {
							sheetError = true;
						}
					}
					Raref(out r1);
					if (StartOf(3)) {
						if (sheetError) {
							e = new Error(ErrorValue.refError);
						}
						else {
							e = new CellRef(s1, r1);
						}
					}
					else if (la.kind == 26) {
						Get();
						Raref(out r2);
						if (sheetError) {
							e = new Error(ErrorValue.refError);
						}
						else {
							e = new CellArea(s1, r1, r2);
						}
					}
					else {
						SynErr(37);
					}
					break;
				}
				case 2: {
					Number(out d);
					e = new NumberConst(d);
					break;
				}
				case 18: {
					Get();
					Factor(out e);
					if (e is NumberConst) {
						e = new NumberConst(-((NumberConst)e).value.value);
					}
					else {
						e = FunCall.Make("NEG", new Expr[] {e});
					}

					break;
				}
				case 15: {
					Get();
					e = new TextConst(t.val.Substring(1, t.val.Length - 2));
					break;
				}
				case 27: {
					Get();
					Expr(out e);
					Expect(28);
					break;
				}
				default:
					SynErr(38);
					break;
			}
		}

		private void Application(out Expr e) {
			String s;
			Expr[] es;
			e = null;
			Name(out s);
			Expect(27);
			if (la.kind == 28) {
				Get();
				e = FunCall.Make(s.ToUpper(), new Expr[0]);
			}
			else if (StartOf(4)) {
				Exprs1(out es);
				Expect(28);
				e = FunCall.Make(s.ToUpper(), es);
			}
			else {
				SynErr(39);
			}
		}

		private void Raref(out RARef raref) {
			raref = null;
			switch (la.kind) {
				case 5: {
					Get();
					raref = new RARef(t.val, col, row);
					break;
				}
				case 6: {
					Get();
					raref = new RARef(t.val);
					break;
				}
				case 7: {
					Get();
					raref = new RARef(t.val);
					break;
				}
				case 8: {
					Get();
					raref = new RARef(t.val);
					break;
				}
				case 9: {
					Get();
					raref = new RARef(t.val);
					break;
				}
				case 10: {
					Get();
					raref = new RARef(t.val);
					break;
				}
				case 11: {
					Get();
					raref = new RARef(t.val);
					break;
				}
				case 12: {
					Get();
					raref = new RARef(t.val);
					break;
				}
				case 13: {
					Get();
					raref = new RARef(t.val);
					break;
				}
				case 14: {
					Get();
					raref = new RARef(t.val);
					break;
				}
				default:
					SynErr(40);
					break;
			}
		}

		private void Number(out double d) {
			d = 0.0;
			Expect(2);
			d = double.Parse(t.val, numberFormat);
		}

		private void PowFactor(out Expr e) {
			Expr e2;
			Factor(out e);
			while (la.kind == 29) {
				Get();
				Factor(out e2);
				e = FunCall.Make("^", new Expr[] {e, e2});
			}
		}

		private void Name(out String s) {
			Expect(1);
			s = t.val;
		}

		private void Exprs1(out Expr[] es) {
			Expr e1, e2;
			List<Expr> elist = new List<Expr>();

			Expr(out e1);
			elist.Add(e1);
			while (la.kind == 30 || la.kind == 31) {
				if (la.kind == 30) {
					Get();
				}
				else {
					Get();
				}
				Expr(out e2);
				elist.Add(e2);
			}
			es = elist.ToArray();
		}

		private void MulOp(out String op) {
			op = "*";
			if (la.kind == 32) {
				Get();
			}
			else if (la.kind == 33) {
				Get();
				op = "/";
			}
			else {
				SynErr(41);
			}
		}

		private void CellContents() {
			Expr e;
			double d;
			switch (la.kind) {
				case 20: {
					Get();
					Expr(out e);
					this.cell = Formula.Make(workbook, e);
					break;
				}
				case 16: {
					Get();
					this.cell = new QuoteCell(t.val.Substring(1));
					break;
				}
				case 15: {
					Get();
					this.cell = new TextCell(t.val.Substring(1, t.val.Length - 2));
					break;
				}
				case 3: {
					Get();
					long ticks = DateTime.Parse(t.val).Ticks;
					double time = NumberValue.DoubleFromDateTimeTicks(ticks);
					this.cell = new NumberCell(time);

					break;
				}
				case 2: {
					Number(out d);
					this.cell = new NumberCell(d);
					break;
				}
				case 18: {
					Get();
					Number(out d);
					this.cell = new NumberCell(-d);
					break;
				}
				default:
					SynErr(42);
					break;
			}
		}


		public void Parse() {
			la = new Token();
			la.val = "";
			Get();
			CellContents();
			Expect(0);
		}

		private static readonly bool[,] set = {
												  {T, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x},
												  {x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, T, T, T, T, T, T, x, x, x, x, x, x, x, x, x, x},
												  {x, x, x, x, x, T, T, T, T, T, T, T, T, T, T, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x},
												  {T, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, x, T, T, T, T, T, T, T, T, T, x, x, T, T, T, T, T, T, x, x},
												  {x, T, T, x, T, T, T, T, T, T, T, T, T, T, T, T, x, x, T, x, x, x, x, x, x, x, x, T, x, x, x, x, x, x, x, x}
											  };
	} // end Parser


	public class Errors {
		public int count = 0; // number of errors detected
		public System.IO.TextWriter errorStream = Console.Out; // error messages go to this stream
		public string errMsgFormat = "-- line {0} col {1}: {2}"; // 0=line, 1=column, 2=text

		public virtual void SynErr(int line, int col, int n) {
			string s;
			switch (n) {
				case 0:
					s = "EOF expected";
					break;
				case 1:
					s = "name expected";
					break;
				case 2:
					s = "number expected";
					break;
				case 3:
					s = "datetime expected";
					break;
				case 4:
					s = "sheetref expected";
					break;
				case 5:
					s = "raref expected";
					break;
				case 6:
					s = "xmlssraref11 expected";
					break;
				case 7:
					s = "xmlssraref12 expected";
					break;
				case 8:
					s = "xmlssraref13 expected";
					break;
				case 9:
					s = "xmlssraref21 expected";
					break;
				case 10:
					s = "xmlssraref22 expected";
					break;
				case 11:
					s = "xmlssraref23 expected";
					break;
				case 12:
					s = "xmlssraref31 expected";
					break;
				case 13:
					s = "xmlssraref32 expected";
					break;
				case 14:
					s = "xmlssraref33 expected";
					break;
				case 15:
					s = "string expected";
					break;
				case 16:
					s = "quotecell expected";
					break;
				case 17:
					s = "\"+\" expected";
					break;
				case 18:
					s = "\"-\" expected";
					break;
				case 19:
					s = "\"&\" expected";
					break;
				case 20:
					s = "\"=\" expected";
					break;
				case 21:
					s = "\"<>\" expected";
					break;
				case 22:
					s = "\"<\" expected";
					break;
				case 23:
					s = "\"<=\" expected";
					break;
				case 24:
					s = "\">\" expected";
					break;
				case 25:
					s = "\">=\" expected";
					break;
				case 26:
					s = "\":\" expected";
					break;
				case 27:
					s = "\"(\" expected";
					break;
				case 28:
					s = "\")\" expected";
					break;
				case 29:
					s = "\"^\" expected";
					break;
				case 30:
					s = "\";\" expected";
					break;
				case 31:
					s = "\",\" expected";
					break;
				case 32:
					s = "\"*\" expected";
					break;
				case 33:
					s = "\"/\" expected";
					break;
				case 34:
					s = "??? expected";
					break;
				case 35:
					s = "invalid AddOp";
					break;
				case 36:
					s = "invalid LogicalOp";
					break;
				case 37:
					s = "invalid Factor";
					break;
				case 38:
					s = "invalid Factor";
					break;
				case 39:
					s = "invalid Application";
					break;
				case 40:
					s = "invalid Raref";
					break;
				case 41:
					s = "invalid MulOp";
					break;
				case 42:
					s = "invalid CellContents";
					break;

				default:
					s = "error " + n;
					break;
			}
			errorStream.WriteLine(errMsgFormat, line, col, s);
			count++;
		}

		public virtual void SemErr(int line, int col, string s) {
			errorStream.WriteLine(errMsgFormat, line, col, s);
			count++;
		}

		public virtual void SemErr(string s) {
			errorStream.WriteLine(s);
			count++;
		}

		public virtual void Warning(int line, int col, string s) { errorStream.WriteLine(errMsgFormat, line, col, s); }

		public virtual void Warning(string s) { errorStream.WriteLine(s); }
	} // Errors


	public class FatalError : Exception {
		public FatalError(string m) : base(m) { }
	}
}