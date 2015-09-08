using System;

namespace CoreCalc.Coco {
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
	}
}