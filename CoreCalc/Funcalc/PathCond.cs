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
using System.Text;
using System.Collections.Generic;

using CoreCalc.Types;

namespace Corecalc.Funcalc {
	/// <summary>
	/// A PathCond represents an evaluation condition.
	/// </summary>
	public abstract class PathCond : IEquatable<PathCond> {
		public static readonly PathCond FALSE = new Disj();
		public static readonly PathCond TRUE = new Conj();

		public abstract PathCond And(CachedAtom expr);

		public abstract PathCond AndNot(CachedAtom expr);

		public abstract PathCond Or(PathCond other);

		public abstract bool Is(bool b);

		public abstract CGExpr ToCGExpr();

		public abstract bool Equals(PathCond other);

		protected static PathCond[] AddItem(IEnumerable<PathCond> set, PathCond item) {
			HashList<PathCond> result = new HashList<PathCond>();
			result.AddAll(set);
			result.Add(item);
			return result.ToArray();
		}

		protected static String FormatInfix(String op, IEnumerable<PathCond> conds) {
			bool first = true;
			StringBuilder sb = new StringBuilder();
			sb.Append("(");
			foreach (PathCond p in conds) {
				if (!first) {
					sb.Append(op);
				}
				first = false;
				sb.Append(p);
			}
			return sb.Append(")").ToString();
		}
	}
}