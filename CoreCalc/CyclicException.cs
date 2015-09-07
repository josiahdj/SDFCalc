// Funcalc, a spreadsheet core implementation 
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

// Delegate types, exception classes, formula formatting options, 
// and specialized collection classes

namespace Corecalc {
	/// <summary>
  /// A CyclicException signals that a cyclic dependency is discovered 
  /// during evaluation.
  /// </summary>
  public class CyclicException : Exception {
    public readonly FullCellAddr culprit;

    public CyclicException(String msg, FullCellAddr culprit) : base(msg) {
      this.culprit = culprit;
    }
  }

	// ----------------------------------------------------------------
  // Formula formatting options

	// ----------------------------------------------------------------
  // A hash bag, a replacement for C5.HashBag<T>

	// ----------------------------------------------------------------
  // An data structure that preserves insertion order of unique elements, 
  // and fast Contains, Add, AddAll, Intersection, Difference, and UnsequencedEquals 
}
