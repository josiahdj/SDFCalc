using System;
using System.Collections.Generic;
using System.IO;

//using System.Collections;

namespace CoreCalc.Coco {
//-----------------------------------------------------------------------------------
// Buffer
//-----------------------------------------------------------------------------------

//-----------------------------------------------------------------------------------
// UTF8Buffer
//-----------------------------------------------------------------------------------

//-----------------------------------------------------------------------------------
// Scanner
//-----------------------------------------------------------------------------------
	public class Scanner {
		private const char EOL = '\n';
		private const int eofSym = 0; /* pdt */
		private const int maxT = 34;
		private const int noSym = 34;


		public Buffer buffer; // scanner buffer

		private Token t; // current token
		private int ch; // current input character
		private int pos; // byte position of current character
		private int charPos; // position by unicode characters starting with 0
		private int col; // column number of current character
		private int line; // line number of current character
		private int oldEols; // EOLs that appeared in a comment;
		private static readonly Dictionary<int, int> start; // maps first token character to start state
//	static readonly Hashtable start; // maps first token character to start state

		private Token tokens; // list of tokens already peeked (first token is a dummy)
		private Token pt; // current peek token

		private char[] tval = new char[128]; // text of current token
		private int tlen; // length of current token

		static Scanner() {
			start = new Dictionary<int, int>(128);
//		start = new Hashtable(128);
			for (int i = 46; i <= 46; ++i) {
				start[i] = 1;
			}
			for (int i = 95; i <= 95; ++i) {
				start[i] = 1;
			}
			for (int i = 48; i <= 57; ++i) {
				start[i] = 48;
			}
			for (int i = 74; i <= 81; ++i) {
				start[i] = 49;
			}
			for (int i = 83; i <= 90; ++i) {
				start[i] = 49;
			}
			for (int i = 106; i <= 122; ++i) {
				start[i] = 49;
			}
			for (int i = 36; i <= 36; ++i) {
				start[i] = 50;
			}
			for (int i = 65; i <= 73; ++i) {
				start[i] = 51;
			}
			for (int i = 97; i <= 105; ++i) {
				start[i] = 51;
			}
			start[82] = 52;
			start[34] = 45;
			start[39] = 47;
			start[43] = 74;
			start[45] = 75;
			start[38] = 76;
			start[61] = 77;
			start[60] = 89;
			start[62] = 90;
			start[58] = 81;
			start[40] = 82;
			start[41] = 83;
			start[94] = 84;
			start[59] = 85;
			start[44] = 86;
			start[42] = 87;
			start[47] = 88;
			start[Buffer.EOF] = -1;
		}

		public Scanner(string fileName) {
			try {
				Stream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
				buffer = new Buffer(stream, false);
				Init();
			}
			catch (IOException) {
				throw new FatalError("Cannot open file " + fileName);
			}
		}

		public Scanner(Stream s) {
			buffer = new Buffer(s, true);
			Init();
		}

		private void Init() {
			pos = -1;
			line = 1;
			col = 0;
			charPos = -1;
			oldEols = 0;
			NextCh();
			if (ch == 0xEF) { // check optional byte order mark for UTF-8
				NextCh();
				int ch1 = ch;
				NextCh();
				int ch2 = ch;
				if (ch1 != 0xBB || ch2 != 0xBF) {
					throw new FatalError(String.Format("illegal byte order mark: EF {0,2:X} {1,2:X}", ch1, ch2));
				}
				buffer = new UTF8Buffer(buffer);
				col = 0;
				charPos = -1;
				NextCh();
			}
			pt = tokens = new Token(); // first token is a dummy
		}

		private void NextCh() {
			if (oldEols > 0) {
				ch = EOL;
				oldEols--;
			}
			else {
				pos = buffer.Pos;
				// buffer reads unicode chars, if UTF8 has been detected
				ch = buffer.Read();
				col++;
				charPos++;
				// replace isolated '\r' by '\n' in order to make
				// eol handling uniform across Windows, Unix and Mac
				if (ch == '\r' && buffer.Peek() != '\n') {
					ch = EOL;
				}
				if (ch == EOL) {
					line++;
					col = 0;
				}
			}
		}

		private void AddCh() {
			if (tlen >= tval.Length) {
				char[] newBuf = new char[2*tval.Length];
				Array.Copy(tval, 0, newBuf, 0, tval.Length);
				tval = newBuf;
			}
			if (ch != Buffer.EOF) {
				tval[tlen++] = (char)ch;
				NextCh();
			}
		}


		private bool Comment0() {
			int level = 1, pos0 = pos, line0 = line, col0 = col, charPos0 = charPos;
			NextCh();
			if (ch == '/') {
				NextCh();
				for (;;) {
					if (ch == 13) {
						NextCh();
						if (ch == 10) {
							level--;
							if (level == 0) {
								oldEols = line - line0;
								NextCh();
								return true;
							}
							NextCh();
						}
					}
					else if (ch == Buffer.EOF) {
						return false;
					}
					else {
						NextCh();
					}
				}
			}
			else {
				buffer.Pos = pos0;
				NextCh();
				line = line0;
				col = col0;
				charPos = charPos0;
			}
			return false;
		}

		private bool Comment1() {
			int level = 1, pos0 = pos, line0 = line, col0 = col, charPos0 = charPos;
			NextCh();
			if (ch == '*') {
				NextCh();
				for (;;) {
					if (ch == '*') {
						NextCh();
						if (ch == '/') {
							level--;
							if (level == 0) {
								oldEols = line - line0;
								NextCh();
								return true;
							}
							NextCh();
						}
					}
					else if (ch == '/') {
						NextCh();
						if (ch == '*') {
							level++;
							NextCh();
						}
					}
					else if (ch == Buffer.EOF) {
						return false;
					}
					else {
						NextCh();
					}
				}
			}
			else {
				buffer.Pos = pos0;
				NextCh();
				line = line0;
				col = col0;
				charPos = charPos0;
			}
			return false;
		}


		private void CheckLiteral() {
			switch (t.val) {
				default:
					break;
			}
		}

		private Token NextToken() {
			while (ch == ' ' ||
				   ch >= 9 && ch <= 10 || ch == 13
				) {
				NextCh();
			}
			if (ch == '/' && Comment0() || ch == '/' && Comment1()) {
				return NextToken();
			}
			int apx = 0;
			int recKind = noSym;
			int recEnd = pos;
			t = new Token();
			t.pos = pos;
			t.col = col;
			t.line = line;
			t.charPos = charPos;
			int state;
			if (start.ContainsKey(ch)) {
				state = start[ch];
			}
			else {
				state = 0;
			}
			tlen = 0;
			AddCh();

			switch (state) {
				case -1: {
					t.kind = eofSym;
					break;
				} // NextCh already done
				case 0: {
					if (recKind != noSym) {
						tlen = recEnd - t.pos;
						SetScannerBehindT();
					}
					t.kind = recKind;
					break;
				} // NextCh already done
				case 1:
					if (ch == '.' || ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'Z' || ch == '_' || ch >= 'a' && ch <= 'z') {
						AddCh();
						goto case 1;
					}
					else if (ch == '(') {
						apx++;
						AddCh();
						goto case 2;
					}
					else {
						goto case 0;
					}
				case 2: {
					tlen -= apx;
					SetScannerBehindT();
					t.kind = 1;
					break;
				}
				case 3:
					if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 5;
					}
					else if (ch == '+' || ch == '-') {
						AddCh();
						goto case 4;
					}
					else {
						goto case 0;
					}
				case 4:
					if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 5;
					}
					else {
						goto case 0;
					}
				case 5:
					recEnd = pos;
					recKind = 2;
					if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 5;
					}
					else {
						t.kind = 2;
						break;
					}
				case 6:
					if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 7;
					}
					else {
						goto case 0;
					}
				case 7:
					recEnd = pos;
					recKind = 2;
					if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 7;
					}
					else if (ch == 'E' || ch == 'e') {
						AddCh();
						goto case 3;
					}
					else {
						t.kind = 2;
						break;
					}
				case 8:
					if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 9;
					}
					else {
						goto case 0;
					}
				case 9:
					if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 10;
					}
					else {
						goto case 0;
					}
				case 10:
					if (ch == '-') {
						AddCh();
						goto case 11;
					}
					else {
						goto case 0;
					}
				case 11:
					if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 12;
					}
					else {
						goto case 0;
					}
				case 12:
					if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 13;
					}
					else {
						goto case 0;
					}
				case 13:
					recEnd = pos;
					recKind = 3;
					if (ch == 'T') {
						AddCh();
						goto case 14;
					}
					else {
						t.kind = 3;
						break;
					}
				case 14:
					if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 15;
					}
					else {
						goto case 0;
					}
				case 15:
					if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 16;
					}
					else {
						goto case 0;
					}
				case 16:
					if (ch == ':') {
						AddCh();
						goto case 17;
					}
					else {
						goto case 0;
					}
				case 17:
					if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 18;
					}
					else {
						goto case 0;
					}
				case 18:
					if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 19;
					}
					else {
						goto case 0;
					}
				case 19:
					if (ch == ':') {
						AddCh();
						goto case 20;
					}
					else {
						goto case 0;
					}
				case 20:
					if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 21;
					}
					else {
						goto case 0;
					}
				case 21:
					if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 22;
					}
					else {
						goto case 0;
					}
				case 22:
					recEnd = pos;
					recKind = 3;
					if (ch == '.') {
						AddCh();
						goto case 23;
					}
					else {
						t.kind = 3;
						break;
					}
				case 23:
					recEnd = pos;
					recKind = 3;
					if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 23;
					}
					else {
						t.kind = 3;
						break;
					}
				case 24:
					if (ch == '!') {
						AddCh();
						goto case 25;
					}
					else if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'Z' || ch >= 'a' && ch <= 'z') {
						AddCh();
						goto case 24;
					}
					else {
						goto case 0;
					}
				case 25: {
					t.kind = 4;
					break;
				}
				case 26:
					if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 28;
					}
					else if (ch == '$') {
						AddCh();
						goto case 27;
					}
					else {
						goto case 0;
					}
				case 27:
					if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 28;
					}
					else {
						goto case 0;
					}
				case 28:
					recEnd = pos;
					recKind = 5;
					if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 28;
					}
					else {
						t.kind = 5;
						break;
					}
				case 29:
					if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 31;
					}
					else if (ch == '$') {
						AddCh();
						goto case 30;
					}
					else {
						goto case 0;
					}
				case 30:
					if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 31;
					}
					else {
						goto case 0;
					}
				case 31:
					recEnd = pos;
					recKind = 5;
					if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 31;
					}
					else {
						t.kind = 5;
						break;
					}
				case 32:
					if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 34;
					}
					else if (ch == '+' || ch == '-') {
						AddCh();
						goto case 33;
					}
					else {
						goto case 0;
					}
				case 33:
					if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 34;
					}
					else {
						goto case 0;
					}
				case 34:
					if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 34;
					}
					else if (ch == ']') {
						AddCh();
						goto case 35;
					}
					else {
						goto case 0;
					}
				case 35: {
					t.kind = 8;
					break;
				}
				case 36:
					if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 38;
					}
					else if (ch == '+' || ch == '-') {
						AddCh();
						goto case 37;
					}
					else {
						goto case 0;
					}
				case 37:
					if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 38;
					}
					else {
						goto case 0;
					}
				case 38:
					if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 38;
					}
					else if (ch == ']') {
						AddCh();
						goto case 39;
					}
					else {
						goto case 0;
					}
				case 39: {
					t.kind = 11;
					break;
				}
				case 40:
					recEnd = pos;
					recKind = 13;
					if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 40;
					}
					else {
						t.kind = 13;
						break;
					}
				case 41:
					if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 43;
					}
					else if (ch == '+' || ch == '-') {
						AddCh();
						goto case 42;
					}
					else {
						goto case 0;
					}
				case 42:
					if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 43;
					}
					else {
						goto case 0;
					}
				case 43:
					if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 43;
					}
					else if (ch == ']') {
						AddCh();
						goto case 44;
					}
					else {
						goto case 0;
					}
				case 44: {
					t.kind = 14;
					break;
				}
				case 45:
					if (ch <= 9 || ch >= 11 && ch <= 12 || ch >= 14 && ch <= '!' || ch >= '#' && ch <= '[' || ch >= ']' && ch <= 65535) {
						AddCh();
						goto case 45;
					}
					else if (ch == '"') {
						AddCh();
						goto case 46;
					}
					else {
						goto case 0;
					}
				case 46: {
					t.kind = 15;
					break;
				}
				case 47:
					recEnd = pos;
					recKind = 16;
					if (ch <= '[' || ch >= ']' && ch <= 65535) {
						AddCh();
						goto case 47;
					}
					else {
						t.kind = 16;
						break;
					}
				case 48:
					recEnd = pos;
					recKind = 2;
					if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 53;
					}
					else if (ch == '!') {
						AddCh();
						goto case 25;
					}
					else if (ch >= 'A' && ch <= 'D' || ch >= 'F' && ch <= 'Z' || ch >= 'a' && ch <= 'd' || ch >= 'f' && ch <= 'z') {
						AddCh();
						goto case 24;
					}
					else if (ch == 'E' || ch == 'e') {
						AddCh();
						goto case 54;
					}
					else if (ch == '.') {
						AddCh();
						goto case 6;
					}
					else {
						t.kind = 2;
						break;
					}
				case 49:
					if (ch == '.' || ch == '_') {
						AddCh();
						goto case 1;
					}
					else if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 55;
					}
					else if (ch == '!') {
						AddCh();
						goto case 25;
					}
					else if (ch == '$') {
						AddCh();
						goto case 27;
					}
					else if (ch >= 'A' && ch <= 'Z' || ch >= 'a' && ch <= 'z') {
						AddCh();
						goto case 56;
					}
					else if (ch == '(') {
						apx++;
						AddCh();
						goto case 2;
					}
					else {
						goto case 0;
					}
				case 50:
					if (ch >= 'J' && ch <= 'Z' || ch >= 'j' && ch <= 'z') {
						AddCh();
						goto case 26;
					}
					else if (ch >= 'A' && ch <= 'I' || ch >= 'a' && ch <= 'i') {
						AddCh();
						goto case 57;
					}
					else {
						goto case 0;
					}
				case 51:
					if (ch == '.' || ch == '_') {
						AddCh();
						goto case 1;
					}
					else if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 55;
					}
					else if (ch == '!') {
						AddCh();
						goto case 25;
					}
					else if (ch == '$') {
						AddCh();
						goto case 27;
					}
					else if (ch >= 'A' && ch <= 'Z' || ch >= 'a' && ch <= 'z') {
						AddCh();
						goto case 58;
					}
					else if (ch == '(') {
						apx++;
						AddCh();
						goto case 2;
					}
					else {
						goto case 0;
					}
				case 52:
					if (ch == '.' || ch == '_') {
						AddCh();
						goto case 1;
					}
					else if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 59;
					}
					else if (ch == '!') {
						AddCh();
						goto case 25;
					}
					else if (ch == '$') {
						AddCh();
						goto case 27;
					}
					else if (ch >= 'A' && ch <= 'B' || ch >= 'D' && ch <= 'Z' || ch >= 'a' && ch <= 'z') {
						AddCh();
						goto case 56;
					}
					else if (ch == '(') {
						apx++;
						AddCh();
						goto case 2;
					}
					else if (ch == 'C') {
						AddCh();
						goto case 60;
					}
					else if (ch == '[') {
						AddCh();
						goto case 61;
					}
					else {
						goto case 0;
					}
				case 53:
					recEnd = pos;
					recKind = 2;
					if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 62;
					}
					else if (ch == '!') {
						AddCh();
						goto case 25;
					}
					else if (ch >= 'A' && ch <= 'D' || ch >= 'F' && ch <= 'Z' || ch >= 'a' && ch <= 'd' || ch >= 'f' && ch <= 'z') {
						AddCh();
						goto case 24;
					}
					else if (ch == 'E' || ch == 'e') {
						AddCh();
						goto case 54;
					}
					else if (ch == '.') {
						AddCh();
						goto case 6;
					}
					else {
						t.kind = 2;
						break;
					}
				case 54:
					if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 63;
					}
					else if (ch == '!') {
						AddCh();
						goto case 25;
					}
					else if (ch >= 'A' && ch <= 'Z' || ch >= 'a' && ch <= 'z') {
						AddCh();
						goto case 24;
					}
					else if (ch == '+' || ch == '-') {
						AddCh();
						goto case 4;
					}
					else {
						goto case 0;
					}
				case 55:
					recEnd = pos;
					recKind = 5;
					if (ch == '.' || ch == '_') {
						AddCh();
						goto case 1;
					}
					else if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 55;
					}
					else if (ch == '!') {
						AddCh();
						goto case 25;
					}
					else if (ch >= 'A' && ch <= 'Z' || ch >= 'a' && ch <= 'z') {
						AddCh();
						goto case 56;
					}
					else if (ch == '(') {
						apx++;
						AddCh();
						goto case 2;
					}
					else {
						t.kind = 5;
						break;
					}
				case 56:
					if (ch == '.' || ch == '_') {
						AddCh();
						goto case 1;
					}
					else if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'Z' || ch >= 'a' && ch <= 'z') {
						AddCh();
						goto case 56;
					}
					else if (ch == '!') {
						AddCh();
						goto case 25;
					}
					else if (ch == '(') {
						apx++;
						AddCh();
						goto case 2;
					}
					else {
						goto case 0;
					}
				case 57:
					if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 28;
					}
					else if (ch == '$') {
						AddCh();
						goto case 27;
					}
					else if (ch >= 'A' && ch <= 'Z' || ch >= 'a' && ch <= 'z') {
						AddCh();
						goto case 29;
					}
					else {
						goto case 0;
					}
				case 58:
					if (ch == '.' || ch == '_') {
						AddCh();
						goto case 1;
					}
					else if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 64;
					}
					else if (ch == '!') {
						AddCh();
						goto case 25;
					}
					else if (ch == '$') {
						AddCh();
						goto case 30;
					}
					else if (ch >= 'A' && ch <= 'Z' || ch >= 'a' && ch <= 'z') {
						AddCh();
						goto case 56;
					}
					else if (ch == '(') {
						apx++;
						AddCh();
						goto case 2;
					}
					else {
						goto case 0;
					}
				case 59:
					recEnd = pos;
					recKind = 5;
					if (ch == '.' || ch == '_') {
						AddCh();
						goto case 1;
					}
					else if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 59;
					}
					else if (ch == '!') {
						AddCh();
						goto case 25;
					}
					else if (ch >= 'A' && ch <= 'B' || ch >= 'D' && ch <= 'Z' || ch >= 'a' && ch <= 'z') {
						AddCh();
						goto case 56;
					}
					else if (ch == '(') {
						apx++;
						AddCh();
						goto case 2;
					}
					else if (ch == 'C') {
						AddCh();
						goto case 65;
					}
					else {
						t.kind = 5;
						break;
					}
				case 60:
					recEnd = pos;
					recKind = 6;
					if (ch == '.' || ch == '_') {
						AddCh();
						goto case 1;
					}
					else if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 66;
					}
					else if (ch == '!') {
						AddCh();
						goto case 25;
					}
					else if (ch >= 'A' && ch <= 'Z' || ch >= 'a' && ch <= 'z') {
						AddCh();
						goto case 56;
					}
					else if (ch == '(') {
						apx++;
						AddCh();
						goto case 2;
					}
					else if (ch == '[') {
						AddCh();
						goto case 32;
					}
					else {
						t.kind = 6;
						break;
					}
				case 61:
					if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 67;
					}
					else if (ch == '+' || ch == '-') {
						AddCh();
						goto case 68;
					}
					else {
						goto case 0;
					}
				case 62:
					recEnd = pos;
					recKind = 2;
					if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 69;
					}
					else if (ch == '!') {
						AddCh();
						goto case 25;
					}
					else if (ch >= 'A' && ch <= 'D' || ch >= 'F' && ch <= 'Z' || ch >= 'a' && ch <= 'd' || ch >= 'f' && ch <= 'z') {
						AddCh();
						goto case 24;
					}
					else if (ch == 'E' || ch == 'e') {
						AddCh();
						goto case 54;
					}
					else if (ch == '.') {
						AddCh();
						goto case 6;
					}
					else {
						t.kind = 2;
						break;
					}
				case 63:
					recEnd = pos;
					recKind = 2;
					if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 63;
					}
					else if (ch == '!') {
						AddCh();
						goto case 25;
					}
					else if (ch >= 'A' && ch <= 'Z' || ch >= 'a' && ch <= 'z') {
						AddCh();
						goto case 24;
					}
					else {
						t.kind = 2;
						break;
					}
				case 64:
					recEnd = pos;
					recKind = 5;
					if (ch == '.' || ch == '_') {
						AddCh();
						goto case 1;
					}
					else if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 64;
					}
					else if (ch == '!') {
						AddCh();
						goto case 25;
					}
					else if (ch >= 'A' && ch <= 'Z' || ch >= 'a' && ch <= 'z') {
						AddCh();
						goto case 56;
					}
					else if (ch == '(') {
						apx++;
						AddCh();
						goto case 2;
					}
					else {
						t.kind = 5;
						break;
					}
				case 65:
					recEnd = pos;
					recKind = 9;
					if (ch == '.' || ch == '_') {
						AddCh();
						goto case 1;
					}
					else if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 70;
					}
					else if (ch == '!') {
						AddCh();
						goto case 25;
					}
					else if (ch >= 'A' && ch <= 'Z' || ch >= 'a' && ch <= 'z') {
						AddCh();
						goto case 56;
					}
					else if (ch == '(') {
						apx++;
						AddCh();
						goto case 2;
					}
					else if (ch == '[') {
						AddCh();
						goto case 36;
					}
					else {
						t.kind = 9;
						break;
					}
				case 66:
					recEnd = pos;
					recKind = 7;
					if (ch == '.' || ch == '_') {
						AddCh();
						goto case 1;
					}
					else if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 66;
					}
					else if (ch == '!') {
						AddCh();
						goto case 25;
					}
					else if (ch >= 'A' && ch <= 'Z' || ch >= 'a' && ch <= 'z') {
						AddCh();
						goto case 56;
					}
					else if (ch == '(') {
						apx++;
						AddCh();
						goto case 2;
					}
					else {
						t.kind = 7;
						break;
					}
				case 67:
					if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 67;
					}
					else if (ch == ']') {
						AddCh();
						goto case 71;
					}
					else {
						goto case 0;
					}
				case 68:
					if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 67;
					}
					else {
						goto case 0;
					}
				case 69:
					recEnd = pos;
					recKind = 2;
					if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 72;
					}
					else if (ch == '!') {
						AddCh();
						goto case 25;
					}
					else if (ch >= 'A' && ch <= 'D' || ch >= 'F' && ch <= 'Z' || ch >= 'a' && ch <= 'd' || ch >= 'f' && ch <= 'z') {
						AddCh();
						goto case 24;
					}
					else if (ch == 'E' || ch == 'e') {
						AddCh();
						goto case 54;
					}
					else if (ch == '.') {
						AddCh();
						goto case 6;
					}
					else if (ch == '-') {
						AddCh();
						goto case 8;
					}
					else {
						t.kind = 2;
						break;
					}
				case 70:
					recEnd = pos;
					recKind = 10;
					if (ch == '.' || ch == '_') {
						AddCh();
						goto case 1;
					}
					else if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 70;
					}
					else if (ch == '!') {
						AddCh();
						goto case 25;
					}
					else if (ch >= 'A' && ch <= 'Z' || ch >= 'a' && ch <= 'z') {
						AddCh();
						goto case 56;
					}
					else if (ch == '(') {
						apx++;
						AddCh();
						goto case 2;
					}
					else {
						t.kind = 10;
						break;
					}
				case 71:
					if (ch == 'C') {
						AddCh();
						goto case 73;
					}
					else {
						goto case 0;
					}
				case 72:
					recEnd = pos;
					recKind = 2;
					if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 72;
					}
					else if (ch == '!') {
						AddCh();
						goto case 25;
					}
					else if (ch >= 'A' && ch <= 'D' || ch >= 'F' && ch <= 'Z' || ch >= 'a' && ch <= 'd' || ch >= 'f' && ch <= 'z') {
						AddCh();
						goto case 24;
					}
					else if (ch == 'E' || ch == 'e') {
						AddCh();
						goto case 54;
					}
					else if (ch == '.') {
						AddCh();
						goto case 6;
					}
					else {
						t.kind = 2;
						break;
					}
				case 73:
					recEnd = pos;
					recKind = 12;
					if (ch >= '0' && ch <= '9') {
						AddCh();
						goto case 40;
					}
					else if (ch == '[') {
						AddCh();
						goto case 41;
					}
					else {
						t.kind = 12;
						break;
					}
				case 74: {
					t.kind = 17;
					break;
				}
				case 75: {
					t.kind = 18;
					break;
				}
				case 76: {
					t.kind = 19;
					break;
				}
				case 77: {
					t.kind = 20;
					break;
				}
				case 78: {
					t.kind = 21;
					break;
				}
				case 79: {
					t.kind = 23;
					break;
				}
				case 80: {
					t.kind = 25;
					break;
				}
				case 81: {
					t.kind = 26;
					break;
				}
				case 82: {
					t.kind = 27;
					break;
				}
				case 83: {
					t.kind = 28;
					break;
				}
				case 84: {
					t.kind = 29;
					break;
				}
				case 85: {
					t.kind = 30;
					break;
				}
				case 86: {
					t.kind = 31;
					break;
				}
				case 87: {
					t.kind = 32;
					break;
				}
				case 88: {
					t.kind = 33;
					break;
				}
				case 89:
					recEnd = pos;
					recKind = 22;
					if (ch == '>') {
						AddCh();
						goto case 78;
					}
					else if (ch == '=') {
						AddCh();
						goto case 79;
					}
					else {
						t.kind = 22;
						break;
					}
				case 90:
					recEnd = pos;
					recKind = 24;
					if (ch == '=') {
						AddCh();
						goto case 80;
					}
					else {
						t.kind = 24;
						break;
					}
			}
			t.val = new String(tval, 0, tlen);
			return t;
		}

		private void SetScannerBehindT() {
			buffer.Pos = t.pos;
			NextCh();
			line = t.line;
			col = t.col;
			charPos = t.charPos;
			for (int i = 0; i < tlen; i++) {
				NextCh();
			}
		}

		// get the next token (possibly a token already seen during peeking)
		public Token Scan() {
			if (tokens.next == null) {
				return NextToken();
			}
			else {
				pt = tokens = tokens.next;
				return tokens;
			}
		}

		// peek for the next token, ignore pragmas
		public Token Peek() {
			do {
				if (pt.next == null) {
					pt.next = NextToken();
				}
				pt = pt.next;
			} while (pt.kind > maxT); // skip pragmas

			return pt;
		}

		// make sure that peeking starts at the current scan position
		public void ResetPeek() { pt = tokens; }
	} // end Scanner
}