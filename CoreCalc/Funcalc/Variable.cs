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

namespace Corecalc.Funcalc {
	/// <summary>
	/// A Variable represents a cell of a sheet-defined function, whether 
	/// an argument (input cell) or a computed cell (intermediate or output cell).
	/// </summary>
	public abstract class Variable : IEquatable<Variable> {
		private readonly string name;
		private readonly Typ type;

		public Variable(String name, Typ type) {
			this.name = name;
			this.type = type;
		}

		public bool Equals(Variable that) { return this == that; }

		public override bool Equals(Object obj) { return Equals(obj as Variable); }

		public override int GetHashCode() { return base.GetHashCode(); }

		public abstract void EmitLoad(ILGenerator ilg);

		public abstract void EmitStore(ILGenerator ilg);

		public abstract Variable Fresh();

		public String Name {
			get { return name; }
		}

		public Typ Type {
			get { return type; }
		}
	}
}