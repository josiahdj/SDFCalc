// Corecalc, a spreadsheet core implementation
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
using System.Reflection;
// LocalBuilder etc

// For SdfInfo

namespace Corecalc {
  /// <summary>
  /// A Value is the result of evaluating an expression in the
  /// interpretive spreadsheet implementation.
  /// </summary>
  public abstract class Value : IEquatable<Value> {
    public static readonly Type type = typeof(Value);
    public static readonly MethodInfo toDoubleOrNanMethod
      = type.GetMethod("ToDoubleOrNan", new Type[] { type });

    public virtual void Apply(Action<Value> act) {
      act(this);
    }

    public abstract bool Equals(Value v);

    // Called from interpreted EXTERN and from generated bytecode
    public static Object ToObject(Value v) {
      return v.ToObject();
    }

    public static double ToDoubleOrNan(Value v) {
      if (v is NumberValue)
        return (v as NumberValue).value;
      else if (v is ErrorValue)
        return (v as ErrorValue).ErrorNan;
      else
        return ErrorValue.argTypeError.ErrorNan;
    }

    // For external methods that do not return anything -- or make it null?
    public static Value MakeVoid(Object o /* ignored */) {
      return TextValue.VOID;
    }

    public abstract Object ToObject();
  }
}
