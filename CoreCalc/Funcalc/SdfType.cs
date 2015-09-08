// Funcalc, spreadsheet with functions
// ----------------------------------------------------------------------
// Copyright (c) 2006-2014 Peter Sestoft

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
using System.Linq;

using CoreCalc.Values;

namespace Corecalc.Funcalc {
	/// <summary>
	/// An SdfType represents and parses signatures of sheet-defined functions
	/// as well as .NET Class Library (external) methods.
	/// </summary>
	public abstract class SdfType {
		public abstract Type GetDotNetType();

		/// <summary>
		/// A SigToken is a lexical used in the signature of an external function.
		/// </summary>
		private abstract class SigToken {
			public static readonly SigToken
				LPAR = new LPar(),
				RPAR = new RPar(),
				LBRK = new LBrk(),
				LBRE = new LBre(),
				EOF = new Eof();
		}

		private class LPar : SigToken {} // (

		private class RPar : SigToken {} // )

		private class LBrk : SigToken {} // [

		private class LBre : SigToken {} // {

		private class Eof : SigToken {}

		/// <summary>
		/// A TypenameToken represents a type name in a signature.
		/// </summary>
		private class TypenameToken : SigToken {
			public readonly String typename;

			public TypenameToken(String typename) { this.typename = typename; }

			public override string ToString() { return typename; }
		}

		/// <summary>
		/// An ErrorToken represents an error in lexical analysis of a signature.
		/// </summary>
		private class ErrorToken : SigToken {
			public readonly String message;

			public ErrorToken(String message) { this.message = message; }
		}

		// ParseType: from stream of signature tokens to a Type object.

		public static SdfType ParseType(String signature) {
			IEnumerator<SigToken> tokens = Scanner(signature);
			tokens.MoveNext(); // There's at least Eof in the stream
			SdfType res = ParseOneType(tokens);
			if (tokens.Current is Eof) {
				return res;
			}
			else {
				throw new SigParseException("Extraneous characters in signature");
			}
		}

		// Before the call, tokens.Current is the first token of the type 
		// to parse; and after the call it is the first token after that type.

		private static SdfType ParseOneType(IEnumerator<SigToken> tokens) {
			if (tokens.Current is LPar) {
				tokens.MoveNext();
				return ParseFunctionSignature(tokens);
			}
			else if (tokens.Current is LBrk) {
				tokens.MoveNext();
				return ParseArraySignature(tokens, 1);
			}
			else if (tokens.Current is LBre) {
				tokens.MoveNext();
				return ParseArraySignature(tokens, 2);
			}
			else if (tokens.Current is TypenameToken) {
				TypenameToken token = tokens.Current as TypenameToken;
				tokens.MoveNext();
				switch (token.typename) {
					case "Z":
						return new SimpleType(typeof (System.Boolean));
					case "C":
						return new SimpleType(typeof (System.Char));
					case "B":
						return new SimpleType(typeof (System.SByte));
					case "b":
						return new SimpleType(typeof (System.Byte));
					case "S":
						return new SimpleType(typeof (System.Int16));
					case "s":
						return new SimpleType(typeof (System.UInt16));
					case "I":
						return new SimpleType(typeof (System.Int32));
					case "i":
						return new SimpleType(typeof (System.UInt32));
					case "J":
						return new SimpleType(typeof (System.Int64));
					case "j":
						return new SimpleType(typeof (System.UInt64));
					case "F":
						return new SimpleType(typeof (System.Single));
					case "D":
					case "N":
						return new SimpleType(typeof (System.Double));
					case "M":
						return new SimpleType(typeof (System.Decimal));
					case "V":
						return new SimpleType(Value.type);
					case "W":
						return new SimpleType(typeof (void));
					case "T":
						return new SimpleType(typeof (System.String));
					case "O":
						return new SimpleType(typeof (System.Object));
					default:
						return new SimpleType(ExternalFunction.FindType(token.typename));
				}
			}
			else if (tokens.Current is Eof) {
				throw new SigParseException("Unexpected end of signature");
			}
			else {
				throw new SigParseException("Unexpected token " + tokens.Current);
			}
		}

		private static SdfType ParseFunctionSignature(IEnumerator<SigToken> tokens) {
			List<SdfType> arguments = new List<SdfType>();
			while (!(tokens.Current is Eof) && !(tokens.Current is RPar)) {
				arguments.Add(ParseOneType(tokens));
			}
			if (tokens.Current is RPar) {
				tokens.MoveNext();
			}
			else {
				throw new SigParseException("Unexpected end of function signature");
			}
			SdfType returntype = ParseOneType(tokens);
			return new FunctionType(arguments.ToArray(), returntype);
		}

		private static SdfType ParseArraySignature(IEnumerator<SigToken> tokens, int dim) {
			if (tokens.Current is Eof) {
				throw new SigParseException("Unexpected end of function signature");
			}
			SdfType elementtype = ParseOneType(tokens);
			return new ArrayType(elementtype, dim);
		}

		/// <summary>
		/// A SigParseException signals an error during parsing of a function signature.
		/// </summary>
		public class SigParseException : Exception {
			public SigParseException(String message) : base(message) { }
		}

		// Scanner: from signature string to stream of signature tokens

		private static IEnumerator<SigToken> Scanner(String signature) {
			int i = 0;
			while (i < signature.Length) {
				char ch = signature[i];
				switch (ch) {
					case 'Z':
					case 'C':
					case 'B':
					case 'b':
					case 'S':
					case 's':
					case 'I':
					case 'i':
					case 'J':
					case 'j':
					case 'D':
					case 'N':
					case 'F':
					case 'M':
					case 'V':
					case 'W':
					case 'T':
					case 'O':
						yield return new TypenameToken(signature.Substring(i, 1));
						break;
					case 'L': // For instance, LSystem.Text.StringBuilder;
						i++;
						int start = i;
						while (i < signature.Length && signature[i] != ';') {
							i++;
						}
						// Now signature[i]==';' or i == signature.Length
						if (i < signature.Length) {
							yield return new TypenameToken(signature.Substring(start, i - start));
						}
						else {
							yield return new ErrorToken("Unterminated class name");
						}
						break;
					case '(':
						yield return SigToken.LPAR;
						break;
					case ')':
						yield return SigToken.RPAR;
						break;
					case '[':
						yield return SigToken.LBRK;
						break;
					case '{':
						yield return SigToken.LBRE;
						break;
					default:
						yield return new ErrorToken("Illegal character '" + ch + "'");
						break;
				}
				i++;
			}
			yield return SigToken.EOF;
			yield break;
		}
	}
}