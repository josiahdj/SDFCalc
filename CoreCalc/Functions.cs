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
using System.Collections.Generic;
using System.Text;
using Corecalc.Funcalc;    // ExternalFunction, SdfInfo, SdfManager ... 
using System.Diagnostics;
using System.Reflection;   // TargetInvocationException
using System.Linq;

namespace Corecalc {
  /// <summary>
  /// A Function represents a built-in function or operator, or a sheet-defined function.
  /// </summary>
  public class Function {
    public readonly String name;
    public Applier Applier { get; private set; }
    public readonly int fixity;                      // If non-zero: operator, precedence 
    public bool IsPlaceHolder { get; private set; }  // May be overwritten by an SDF
    private bool isVolatile;                         // True for RAND, NOW, some SDFs
    private static readonly IDictionary<String, Function> table;

    public static readonly Type type = typeof(Function);  // Used in IL code generation

    private static readonly Random random = new Random();

    // Used by the SDF machinery to update a placeholder function
    public void UpdateApplier(Applier applier, bool isVolatile) {
      Debug.Assert(IsPlaceHolder);
      this.Applier = applier;
      this.IsPlaceHolder = false;
      this.isVolatile = isVolatile;
    }

    // The following methods are used also from compiled SDFs:

    public static double Average(Value[] vs) {
      // May consider whether empty cells and texts should just
      // be ignored instead of given ArgTypeError.
      // We're using Kahan's accurate sum algorithm; see Goldberg 1991.
      double S = 0.0, C = 0.0;
      int count = 0;
      foreach (Value outerV in vs)
        outerV.Apply(delegate(Value v) {
          double Y = NumberValue.ToDoubleOrNan(v) - C, T = S + Y;
          C = (T - S) - Y;
          S = T;
          count++;
        });
      return S / count;
    }

    public static Value Benchmark(Value v0, Value v1) {
      if (v0 is ErrorValue) return v0;
      if (v1 is ErrorValue) return v1;
      FunctionValue fv = v0 as FunctionValue;
      NumberValue n1 = v1 as NumberValue;
      if (fv == null || n1 == null)
        return ErrorValue.argTypeError;
      int count = (int)(n1.value);
      if (count <= 0)
        return ErrorValue.numError;
      // The following replicates some of fun.Call0(), to lift 
      // array unwrapping (although not arity checks and a cast) 
      // out of the timing loops
      if (fv.Arity != 0)
        return ErrorValue.argCountError;
      SdfInfo sdfInfo = fv.sdfInfo;
      Stopwatch stopwatch = new Stopwatch();
      stopwatch.Reset();
      stopwatch.Start();
      switch (sdfInfo.arity) {
        case 0:
          for (int i = count; i > 0; i--)
            sdfInfo.Call0();
          break;
        case 1: {
            Value arg0 = fv.args[0];
            for (int i = count; i > 0; i--)
              sdfInfo.Call1(arg0);
            break;
          }
        case 2: {
            Value arg0 = fv.args[0], arg1 = fv.args[1];
            for (int i = count; i > 0; i--)
              sdfInfo.Call2(arg0, arg1);
            break;
          }
        case 3: {
            Value arg0 = fv.args[0], arg1 = fv.args[1], arg2 = fv.args[2];
            for (int i = count; i > 0; i--)
              sdfInfo.Call3(arg0, arg1, arg2);
            break;
          }
        case 4: {
            Value arg0 = fv.args[0], arg1 = fv.args[1],
              arg2 = fv.args[2], arg3 = fv.args[3];
            for (int i = count; i > 0; i--)
              sdfInfo.Call4(arg0, arg1, arg2, arg3);
            break;
          }
        case 5: {
            Value arg0 = fv.args[0], arg1 = fv.args[1], arg2 = fv.args[2],
              arg3 = fv.args[3], arg4 = fv.args[4];
            for (int i = count; i > 0; i--)
              sdfInfo.Call5(arg0, arg1, arg2, arg3, arg4);
            break;
          }
        case 6: {
            Value arg0 = fv.args[0], arg1 = fv.args[1], arg2 = fv.args[2],
              arg3 = fv.args[3], arg4 = fv.args[4], arg5 = fv.args[5];
            for (int i = count; i > 0; i--)
              sdfInfo.Call6(arg0, arg1, arg2, arg3, arg4, arg5);
            break;
          }
        case 7: {
            Value arg0 = fv.args[0], arg1 = fv.args[1], arg2 = fv.args[2],
              arg3 = fv.args[3], arg4 = fv.args[4], arg5 = fv.args[5],
              arg6 = fv.args[6];
            for (int i = count; i > 0; i--)
              sdfInfo.Call7(arg0, arg1, arg2, arg3, arg4, arg5, arg6);
            break;
          }
        case 8: {
            Value arg0 = fv.args[0], arg1 = fv.args[1], arg2 = fv.args[2],
              arg3 = fv.args[3], arg4 = fv.args[4], arg5 = fv.args[5],
              arg6 = fv.args[6], arg7 = fv.args[7];
            for (int i = count; i > 0; i--)
              sdfInfo.Call8(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            break;
          }
        case 9: {
            Value arg0 = fv.args[0], arg1 = fv.args[1], arg2 = fv.args[2],
              arg3 = fv.args[3], arg4 = fv.args[4], arg5 = fv.args[5],
              arg6 = fv.args[6], arg7 = fv.args[7], arg8 = fv.args[8];
            for (int i = count; i > 0; i--)
              sdfInfo.Call9(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
            break;
          }
        default:
          return ErrorValue.tooManyArgsError;
      }
      stopwatch.Stop();
      return NumberValue.Make((1E6 * stopwatch.ElapsedMilliseconds) / count);
    }

    public static Value ColMap(Value v0, Value v1) {
      if (v0 is ErrorValue) return v0;
      if (v1 is ErrorValue) return v1;
      if (v0 is FunctionValue && v1 is ArrayValue) {
        FunctionValue fv = v0 as FunctionValue;
        ArrayValue arr = v1 as ArrayValue;
        if (fv.Arity != arr.Rows)
          return ErrorValue.argCountError;
        Value[,] result = new Value[arr.Cols, 1];
        Value[] arguments = new Value[arr.Rows];
        for (int c = 0; c < arr.Cols; c++) {
          for (int r = 0; r < arr.Rows; r++)
            arguments[r] = arr[c, r];
          result[c, 0] = fv.Apply(arguments);
        }
        return new ArrayExplicit(result);
      } else
        return ErrorValue.argTypeError;
    }

    public static double Columns(Value v0) {
      if (v0 is ErrorValue) return (v0 as ErrorValue).ErrorNan;
      ArrayValue v0arr = v0 as ArrayValue;
      if (v0arr != null)
        return v0arr.Cols;
      else
        return ErrorValue.argTypeError.ErrorNan;
    }

    public static Value ConstArray(Value v0, Value v1, Value v2) {
      if (v0 is ErrorValue) return v0;
      if (v1 is ErrorValue) return v1;
      if (v2 is ErrorValue) return v2;
      if (v1 is NumberValue && v2 is NumberValue) {
        int rows = (int)(v1 as NumberValue).value,
            cols = (int)(v2 as NumberValue).value;
        if (0 <= rows && 0 <= cols) {
          Value[,] result = new Value[cols, rows];
          for (int c = 0; c < cols; c++)
            for (int r = 0; r < rows; r++)
              result[c, r] = v0;
          return new ArrayExplicit(result);
        } else
          return ErrorValue.Make("#ERR: Size");
      } else
        return ErrorValue.argTypeError;
    }

    public static Value CountIf(Value v0, Value v1) {
      if (v0 is ErrorValue) return v0;
      if (v1 is ErrorValue) return v1;
      FunctionValue fv = v0 as FunctionValue;
      if (fv == null)
        return ErrorValue.argTypeError;
      if (fv.Arity != 1)
        return ErrorValue.argCountError;
      double count = 0.0;
      v1.Apply(delegate(Value v) {
        if (!Double.IsNaN(count)) {
          double condition = NumberValue.ToDoubleOrNan(fv.Call1(v));
          if (Double.IsNaN(condition))
            count = condition; // Hack: Error propagation from predicate
          else if (condition != 0)
            count++;
        }
      });
      return NumberValue.Make(count);
    }

    public static double Equal(Value e1, Value e2) {
      if (e1 is ErrorValue) return (e1 as ErrorValue).ErrorNan;
      if (e2 is ErrorValue) return (e2 as ErrorValue).ErrorNan;
      return e1 == e2 || e1 != null && e1.Equals(e2) ? 1.0 : 0.0;
    }

    public static double ExcelAtan2(double x, double y) {
      return Math.Atan2(y, x);      // Note swapped arguments
    }

    public static double ExcelCeiling(double d, double signif) {
      return signif * Math.Ceiling(d / signif);
    }

    public static Value ExcelConcat(Value v0, Value v1) {
      if (v0 is ErrorValue) return v0;
      if (v1 is ErrorValue) return v1;
      if ((v0 is TextValue || v0 is NumberValue) && (v1 is TextValue || v1 is NumberValue)) {
        String s0 = v0.ToString(), s1 = v1.ToString();
        // Avoid creating new String or TextValue objects when possible
        if (s0 == "")
          return v1 as TextValue ?? TextValue.Make(s1);
        else if (s1 == "")
          return v0 as TextValue ?? TextValue.Make(s0);
        else
          return TextValue.Make(s0 + s1);
      } else
        return ErrorValue.argTypeError;
    }

    public static double ExcelFloor(double d, double signif) {
      return signif * Math.Floor(d / signif);
    }

    public static double ExcelMod(double x, double y) {
      return x - y * Math.Floor(x / y);
    }

    public static double ExcelNow() {
      return NumberValue.DoubleFromDateTimeTicks(DateTime.Now.Ticks);
    }

    public static double ExcelPow(double x, double y) { 
      // Necessary because MS .NET and Mono Math.Pow is wrong
      return double.IsNaN(x) ? x : double.IsNaN(y) ? y : Math.Pow(x, y);
    }

    public static double ExcelRand() {
      return random.NextDouble();
    }

    public static double ExcelRound(double d, double digits) {
      if (Double.IsNaN(digits)) return digits;
      int idigits = (int)digits; // Truncation towards zero is correct
      if (idigits >= 0)
        return Math.Round(d, idigits, MidpointRounding.AwayFromZero);
      else {
        double scale = Math.Pow(10, -idigits);
        return scale * Math.Round(d / scale, 0);
      }
    }

    public static Value HArray(Value[] vs) {
      Value[,] result = new Value[vs.Length, 1];
      for (int c = 0; c < vs.Length; c++)
        if (vs[c] is ErrorValue)
          return vs[c];
        else
          result[c, 0] = vs[c];
      return new ArrayExplicit(result);
    }

    // Make array as horizontal (side-by-side) concatenation of arguments' columns
    public static Value HCat(Value[] vs) {
      int rows = 0, cols = 0;
      foreach (Value v in vs)
        if (v is ErrorValue)
          return v;
        else if (v is ArrayValue) {
          rows = Math.Max(rows, (v as ArrayValue).Rows);
          cols += (v as ArrayValue).Cols;
        } else {
          rows = Math.Max(rows, 1);
          cols += 1;
        }
      foreach (Value v in vs)
        if (v is ArrayValue && (v as ArrayValue).Rows != rows)
          return ErrorValue.Make("#ERR: Row counts differ");
      Value[,] result = new Value[cols, rows];
      int nextCol = 0;
      foreach (Value v in vs)
        if (v is ArrayValue) {
          ArrayValue arr = v as ArrayValue;
          for (int c = 0; c < arr.Cols; c++) {
            for (int r = 0; r < rows; r++)
              result[nextCol, r] = arr[c, r];
            nextCol++;
          }
        } else {
          for (int r = 0; r < rows; r++)
            result[nextCol, r] = v;
          nextCol++;
        }
      return new ArrayExplicit(result);
    }

    // Return horizontal array with nv+1 columns cv, fv(cv), fv(fv(cv)), ...
    public static Value HScan(Value v0, Value v1, Value v2) {
      if (v0 is ErrorValue) return v0;
      if (v1 is ErrorValue) return v1;
      if (v2 is ErrorValue) return v2;
      FunctionValue fv = v0 as FunctionValue;
      ArrayValue cv = v1 as ArrayValue;
      NumberValue nv = v2 as NumberValue;
      if (fv == null || cv == null || nv == null)
        return ErrorValue.argTypeError;
      else {
        int n = (int)nv.value;
        int rows = cv.Rows;
        if (n < 0 || rows == 0 || cv.Cols != 1)
          return ErrorValue.Make("#ERR: Argument value in HSCAN");
        else {
          Value[,] result = new Value[n+1, rows];
          for (int r=0; r<rows; r++)
            result[0, r] = cv[0, r];
          for (int c=1; c<=n; c++) {
            cv = fv.Call1(cv) as ArrayValue;
            if (cv == null)
              return ErrorValue.argTypeError;
            if (cv.Cols != 1 || cv.Rows != rows)
              return ErrorValue.Make("#ERR: Result shape in HSCAN");
            for (int r=0; r<rows; r++)
              result[c, r] = cv[0, r];
          }
          return new ArrayExplicit(result);
        }
      }
    }

    public static Value Index(Value v0, double r, double c) {
      if (v0 is ErrorValue) return v0;
      if (Double.IsNaN(r)) return NumberValue.Make(r);
      if (Double.IsNaN(c)) return NumberValue.Make(c);
      ArrayValue arr = v0 as ArrayValue;
      if (arr != null)
        return arr.Index(r, c);
      else
        return ErrorValue.argTypeError;
    }

    public static double IsArray(Value v0) {
      if (v0 is ErrorValue) return (v0 as ErrorValue).ErrorNan;
      if (v0 == null)
        return ErrorValue.argTypeError.ErrorNan;
      else
        return v0 is ArrayValue ? 1.0 : 0.0;
    }

    // This is Excel ISERROR; Excel ISERR does not consider #N/A an error
    public static double IsError(Value v0) {
      return v0 as ErrorValue != null ? 1.0 : 0.0;
    }

    // Generalized n-argument map
    public static Value Map(Value[] vs) {
      int n = vs.Length - 1;
      if (n < 1)
        return ErrorValue.argCountError;
      if (vs[0] is ErrorValue) return vs[0];
      FunctionValue fv = vs[0] as FunctionValue;
      if (fv == null)
        return ErrorValue.argTypeError;
      if (fv.Arity != n)
        return ErrorValue.argCountError;
      ArrayValue[] arrs = new ArrayValue[n];
      for (int i = 0; i < n; i++) {
        Value vi = vs[i + 1];
        if (vi is ArrayValue)
          arrs[i] = vi as ArrayValue;
        else if (vi is ErrorValue)
          return vi;
        else
          return ErrorValue.argTypeError;
      }
      int cols = arrs[0].Cols, rows = arrs[0].Rows;
      for (int i=1; i<n; i++)
        if (arrs[i].Cols != cols || arrs[i].Rows != rows)
          return ErrorValue.Make("#ERR: Array shapes differ");
      Value[] args = new Value[n];
      Value[,] result = new Value[cols, rows];
      for (int c = 0; c < cols; c++)
        for (int r = 0; r < rows; r++) {
          for (int i = 0; i < n; i++)
            args[i] = arrs[i][c, r];
          result[c, r] = fv.Apply(args);
        }
      return new ArrayExplicit(result);
    }

    public static double Max(Value[] vs) {
      double result = Double.NegativeInfinity;
      foreach (Value outerV in vs)
        outerV.Apply(delegate(Value v) {
          result = Math.Max(result, NumberValue.ToDoubleOrNan(v));
        });
      return result;
    }

    public static double Min(Value[] vs) {
      double result = Double.PositiveInfinity;
      foreach (Value outerV in vs)
        outerV.Apply(delegate(Value v) {
          result = Math.Min(result, NumberValue.ToDoubleOrNan(v));
        });
      return result;
    }

    public static Value Reduce(Value v0, Value v1, Value v2) {
      if (v0 is ErrorValue) return v0;
      if (v1 is ErrorValue) return v1;
      if (v2 is ErrorValue) return v2;
      if (v0 is FunctionValue && v2 is ArrayValue) {
        FunctionValue fv = v0 as FunctionValue;
        ArrayValue arr = v2 as ArrayValue;
        if (fv.Arity != 2)
          return ErrorValue.argCountError;
        Value result = v1;
        for (int r = 0; r < arr.Rows; r++)
          for (int c = 0; c < arr.Cols; c++)
            result = fv.Call2(result, arr[c, r]);
        return result;
      } else
        return ErrorValue.argTypeError;
    }

    public static Value RowMap(Value v0, Value v1) {
      if (v0 is ErrorValue) return v0;
      if (v1 is ErrorValue) return v1;
      if (v1 is ArrayValue && v0 is FunctionValue) {
        ArrayValue arr = v1 as ArrayValue;
        FunctionValue fv = v0 as FunctionValue;
        if (fv.Arity != arr.Cols)
          return ErrorValue.argCountError;
        Value[,] result = new Value[1, arr.Rows];
        Value[] arguments = new Value[arr.Cols];
        for (int r = 0; r < arr.Rows; r++) {
          for (int c = 0; c < arr.Cols; c++)
            arguments[c] = arr[c, r];
          result[0, r] = fv.Apply(arguments);
        }
        return new ArrayExplicit(result);
      } else
        return ErrorValue.argTypeError;
    }

    public static double Rows(Value v0) {
      if (v0 is ErrorValue) return (v0 as ErrorValue).ErrorNan;
      ArrayValue v0arr = v0 as ArrayValue;
      if (v0arr != null)
        return v0arr.Rows;
      else
        return ErrorValue.argTypeError.ErrorNan;
    }

    public static double Sign(double x) {
      return Double.IsNaN(x) ? x : (double)Math.Sign(x);
    }

    public static Value Slice(Value v0, double r1, double c1, double r2, double c2) {
      if (v0 is ErrorValue) return v0;
      if (Double.IsNaN(r1)) return NumberValue.Make(r1);
      if (Double.IsNaN(c1)) return NumberValue.Make(c1);
      if (Double.IsNaN(r2)) return NumberValue.Make(r2);
      if (Double.IsNaN(c2)) return NumberValue.Make(c2);
      ArrayValue arr = v0 as ArrayValue;
      if (arr != null) 
        return arr.Slice(r1, c1, r2, c2);
      else
        return ErrorValue.argTypeError;
    }

    public static Value Specialize(Value v0) {
      if (v0 is ErrorValue) return v0;
      FunctionValue fv = v0 as FunctionValue;
      if (fv != null)
        if (fv.args.All(v => v == ErrorValue.naError))
          return fv;
        else
          return new FunctionValue(SdfManager.SpecializeAndCompile(fv), null);
      else
        return ErrorValue.argTypeError;
    }

    public static double Sum(Value[] vs) {
      // May consider whether empty cells and texts should just
      // be ignored instead of giving ArgTypeError.
      // We're using Kahan's accurate sum algorithm; see Goldberg 1991.
      double S = 0.0, C = 0.0;
      foreach (Value outerV in vs)
        outerV.Apply(delegate(Value v) {
          double Y = NumberValue.ToDoubleOrNan(v) - C, T = S + Y;
          C = (T - S) - Y;
          S = T;
        });
      return S;
    }

    public static double SumNew(Value[] vs) {
      // Lower-functionality slightly-higher performance SUM.
      // May consider whether empty cells and texts should just
      // be ignored instead of giving ArgTypeError.
      // We're using Kahan's accurate sum algorithm; see Goldberg 1991.
      double S = 0.0, C = 0.0;
      foreach (Value outerV in vs)
        if (outerV is ArrayValue) {
          ArrayValue arr = outerV as ArrayValue;
          int cols = arr.Cols, rows = arr.Rows;
          for (int c = 0; c < cols; c++)
            for (int r = 0; r < rows; r++) {
              Value v = arr[c, r];
              if (v != null) { // Only non-blank cells contribute
                double Y = NumberValue.ToDoubleOrNan(v) - C, T = S + Y;
                C = (T - S) - Y;
                S = T;
              }
            }
        }
        else if (outerV != null) {
          double Y = NumberValue.ToDoubleOrNan(outerV) - C, T = S + Y;
          C = (T - S) - Y;
          S = T;
        }
      return S;
    }

    public static Value SumIf(Value v0, Value v1) {
      if (v0 is ErrorValue) return v0;
      if (v1 is ErrorValue) return v1;
      FunctionValue fv = v0 as FunctionValue;
      if (fv == null)
        return ErrorValue.argTypeError;
      if (fv.Arity != 1)
        return ErrorValue.argCountError;
      // We're using Kahan's accurate sum algorithm; see Goldberg 1991.
      double S = 0.0, C = 0.0;
      v1.Apply(delegate(Value v) {
          if (!Double.IsNaN(S)) {
            double condition = NumberValue.ToDoubleOrNan(fv.Call1(v));
            if (Double.IsNaN(condition))
              S = condition; // Error propagation from predicate
            else if (condition != 0) {
              double Y = NumberValue.ToDoubleOrNan(v) - C, T = S + Y;
              C = (T - S) - Y;
              S = T;
            }
          }
        });
      return NumberValue.Make(S);
    }

    public static Value Tabulate(Value v0, Value v1, Value v2) {
      if (v0 is ErrorValue) return v0;
      if (v1 is ErrorValue) return v1;
      if (v2 is ErrorValue) return v2;
      if (v0 is FunctionValue && v1 is NumberValue && v2 is NumberValue) {
        FunctionValue fv = v0 as FunctionValue;
        if (fv.Arity != 2)
          return ErrorValue.argCountError;
        int rows = (int)(v1 as NumberValue).value,
          cols = (int)(v2 as NumberValue).value;
        if (0 <= rows && 0 <= cols) {
          Value[,] result = new Value[cols, rows];
          for (int c = 0; c < cols; c++)
            for (int r = 0; r < rows; r++)
              result[c, r] = fv.Call2(NumberValue.Make(r + 1), NumberValue.Make(c + 1));
          return new ArrayExplicit(result);
        } else
          return ErrorValue.Make("#ERR: Size");
      } else
        return ErrorValue.argTypeError;
    }

    public static Value Transpose(Value v0) {
      if (v0 is ErrorValue) return v0;
      ArrayValue v0arr = v0 as ArrayValue;
      if (v0arr != null) {
        int cols = v0arr.Rows, rows = v0arr.Cols;
        Value[,] result = new Value[cols, rows];
        for (int c = 0; c < cols; c++)
          for (int r = 0; r < rows; r++)
            result[c, r] = v0arr[r, c];
        return new ArrayExplicit(result);
      } else
        return ErrorValue.argTypeError;
    }

    public static Value VArray(Value[] vs) {
      Value[,] result = new Value[1, vs.Length];
      for (int r = 0; r < vs.Length; r++)
        if (vs[r] is ErrorValue)
          return vs[r];
        else
          result[0, r] = vs[r];
      return new ArrayExplicit(result);
    }

    // Make array as vertical concatenation (stack) of the arguments' rows
    public static Value VCat(Value[] vs) {
      int rows = 0, cols = 0;
      foreach (Value v in vs)
        if (v is ErrorValue)
          return v;
        else if (v is ArrayValue) {
          cols = Math.Max(cols, (v as ArrayValue).Cols);
          rows += (v as ArrayValue).Rows;
        } else {
          cols = Math.Max(cols, 1);
          rows += 1;
        }
      foreach (Value v in vs)
        if (v is ArrayValue && (v as ArrayValue).Cols != cols)
          return ErrorValue.Make("#ERR: Column counts differ");
      Value[,] result = new Value[cols, rows];
      int nextRow = 0;
      foreach (Value v in vs)
        if (v is ArrayValue) {
          ArrayValue arr = v as ArrayValue;
          for (int r = 0; r < arr.Rows; r++) {
            for (int c = 0; c < cols; c++)
              result[c, nextRow] = arr[c, r];
            nextRow++;
          }
        } else {
          for (int c = 0; c < cols; c++)
            result[c, nextRow] = v;
          nextRow++;
        }
      return new ArrayExplicit(result);
    }

    // Return vertical array with nv+1 rows rv, fv(rv), fv(fv(rv)), ...
    public static Value VScan(Value v0, Value v1, Value v2) {
      if (v0 is ErrorValue) return v0;
      if (v1 is ErrorValue) return v1;
      if (v2 is ErrorValue) return v2;
      FunctionValue fv = v0 as FunctionValue;
      ArrayValue rv = v1 as ArrayValue;
      NumberValue nv = v2 as NumberValue;
      if (fv == null || rv == null || nv == null)
        return ErrorValue.argTypeError;
      else {
        int n = (int)nv.value;
        int cols = rv.Cols;
        if (n < 0 || cols == 0 || rv.Rows != 1)
          return ErrorValue.Make("#ERR: Argument value in VSCAN");
        else {
          Value[,] result = new Value[cols,n+1];
          for (int c=0; c<cols; c++)
            result[c, 0] = rv[c, 0];
          for (int r=1; r<=n; r++) {
            rv = fv.Call1(rv) as ArrayValue;
            if (rv == null)
              return ErrorValue.argTypeError;
            if (rv.Rows != 1 || rv.Cols != cols)
              return ErrorValue.Make("#ERR: Result shape in VSCAN");
            for (int c=0; c<cols; c++)
              result[c, r] = rv[c, 0];
          }
          return new ArrayExplicit(result);
        }
      }
    }

    // ----- End of SDF-callable built-in functions -----

    public bool IsVolatile(Expr[] es) {
      // A partial application is volatile if the underlying function is
      if (name == "CLOSURE") {
        if (es.Length > 0 && es[0] is TextConst) {
          String sdfName = (es[0] as TextConst).value.value;
          SdfInfo sdfInfo = SdfManager.GetInfo(sdfName);
          return sdfInfo != null && sdfInfo.IsVolatile;
        } else
          return false;
      } else
        return isVolatile;
    }

    private static Value Closure(Sheet sheet, Expr[] es, int col, int row) {
      // First argument may be a (constant) function name or a FunctionValue
      if (es.Length < 1)
        return ErrorValue.argCountError;
      int argCount = es.Length - 1;
      Value[] arguments = new Value[argCount];
      for (int i = 1; i < es.Length; i++) {
        Value vi = es[i].Eval(sheet, col, row);
        if (vi == null)
          return ErrorValue.argTypeError;
        arguments[i - 1] = vi;
      }
      if (es[0] is TextConst) {
        String name = (es[0] as TextConst).value.value;
        SdfInfo sdfInfo = SdfManager.GetInfo(name);
        if (sdfInfo == null)
          return ErrorValue.nameError;
        if (argCount != 0 && argCount != sdfInfo.arity)
          return ErrorValue.argCountError;
        return new FunctionValue(sdfInfo, arguments);
      } else {
        Value v0 = es[0].Eval(sheet, col, row);
        if (v0 is FunctionValue)  // Further application of a partial application 
          return (v0 as FunctionValue).FurtherApply(arguments);
        else if (v0 is ErrorValue)
          return v0;
        else
          return ErrorValue.argTypeError;
      }
    }

    // Auxiliary for EXTERN and EXTERNVOLATILE
    private static Value CallExtern(Sheet sheet, Expr[] es, int col, int row) {
      if (es.Length < 1)
        return ErrorValue.argCountError;
      TextConst nameAndSignatureConst = es[0] as TextConst;
      if (nameAndSignatureConst == null)
        return ErrorValue.argTypeError;
      try {
        // This retrieves the method from cache, or creates it:
        ExternalFunction ef = ExternalFunction.Make(nameAndSignatureConst.value.value);
        Value[] values = new Value[es.Length - 1];
        for (int i = 0; i < values.Length; i++)
          values[i] = es[i + 1].Eval(sheet, col, row);
        return ef.Call(values);
      } catch (TargetInvocationException exn)  // From external method
      {
        return ErrorValue.Make(exn.InnerException.Message);
      } catch (Exception exn)  // Covers a multitude of sins
      {
        return ErrorValue.Make("#EXTERN: " + exn.Message);
      }
    }

    // Get a Function by name
    public static Function Get(String name) {
      Function result;
      if (table.TryGetValue(name.ToUpper(), out result))
        return result;
      else
        return null;
    }

    internal static bool Exists(string name) {
      return table.ContainsKey(name.ToUpper());
    }

    internal static void Remove(string name) {
      table.Remove(name);
    }

    // Populate table of functions.  Corresponding data for sheet-defined 
    // functions are in FunctionInfo.functions and in a CGComposite switch.
    static Function() {
      table = new Dictionary<String, Function>();
      // <fun> : unit -> number
      new Function("NOW", MakeNumberFunction(ExcelNow),
                   isVolatile: true);
      new Function("PI", MakeConstant(NumberValue.PI));
      new Function("RAND", MakeNumberFunction(ExcelRand),
                   isVolatile: true);
      // <fun> : unit -> #NA error
      new Function("NA", MakeConstant(ErrorValue.naError));
      // <fun> : number -> number
      new Function("ABS", MakeNumberFunction(Math.Abs));
      new Function("ASIN", MakeNumberFunction(Math.Asin));
      new Function("ACOS", MakeNumberFunction(Math.Acos));
      new Function("ATAN", MakeNumberFunction(Math.Atan));
      new Function("COS", MakeNumberFunction(Math.Cos));
      new Function("EXP", MakeNumberFunction(Math.Exp));
      new Function("LN", MakeNumberFunction((Func<double, double>)Math.Log));
      new Function("LOG", MakeNumberFunction(Math.Log10));
      new Function("LOG10", MakeNumberFunction(Math.Log10));
      new Function("NEG", 9 /* print neatly */, MakeNumberFunction(x => -x));
      new Function("SIN", MakeNumberFunction(Math.Sin));
      new Function("SQRT", MakeNumberFunction(Math.Sqrt));
      new Function("TAN", MakeNumberFunction(Math.Tan));
      // <fun> : number * number -> number
      new Function("ATAN2", MakeNumberFunction(ExcelAtan2));
      new Function("CEILING", MakeNumberFunction(ExcelCeiling));
      new Function("FLOOR", MakeNumberFunction(ExcelFloor));
      new Function("MOD", MakeNumberFunction(ExcelMod));
      new Function("ROUND", MakeNumberFunction(ExcelRound));
      new Function("^", 8, MakeNumberFunction(ExcelPow));
      new Function("*", 7, MakeNumberFunction((x, y) => x * y));
      new Function("/", 7, MakeNumberFunction((x, y) => x / y));
      new Function("+", 6, MakeNumberFunction((x, y) => x + y ));
      new Function("-", 6, MakeNumberFunction((x, y) => x - y));
      new Function("&", 6, MakeFunction(ExcelConcat));
      new Function("<", 5, MakePredicate((x, y) => x < y));
      new Function("<=", 5, MakePredicate((x, y) => x <= y));
      new Function(">=", 5, MakePredicate((x, y) => x >= y));
      new Function(">", 5, MakePredicate((x, y) => x > y));
      new Function("=", 4, MakePredicate((x, y) => x == y));
      new Function("<>", 4, MakePredicate((x, y) => x != y));
      // AND : number* -> number, 
      new Function("AND",   // Variadic, and non-strict in args 2, 3, ...
          delegate(Sheet sheet, Expr[] es, int col, int row) {
            for (int i = 0; i < es.Length; i++) {
              Value vi = es[i].Eval(sheet, col, row);
              NumberValue ni = vi as NumberValue;
              if (ni != null) {
                if (ni.value == 0)
                  return NumberValue.ZERO;
              } else if (vi is ErrorValue)
                return vi;
              else
                return ErrorValue.argTypeError;
            }
            return NumberValue.ONE;
          });
      // APPLY(fv,a1...an) applies closure fv to arguments a1...an, n>=0
      new Function("APPLY",  // Variadic
        MakeFunction(delegate(Value[] vs) {
        if (vs.Length < 1)
          return ErrorValue.argCountError;
        if (vs[0] is ErrorValue)
          return vs[0];
        FunctionValue fv = vs[0] as FunctionValue;
        if (fv == null)
          return ErrorValue.argTypeError;
        int argCount = vs.Length - 1;
        Value[] arguments = new Value[argCount];
        for (int i = 1; i < vs.Length; i++)
          arguments[i - 1] = vs[i];
        return fv.Apply(arguments);
      }));
      //  AVERAGE : (number | array)*  -> number
      new Function("AVERAGE", MakeNumberFunction(Average));
      // BENCHMARK(fv, n) evaluates fv to zero-arity FunctionValue,
      // then calls it n times and returns the number of nanoseconds per call
      new Function("BENCHMARK", MakeFunction(Benchmark));
      // CLOSURE(sdfname/fv,arguments...) creates a FunctionValue closure by partial
      // application of a sheet-defined function or an given FunctionValue
      new Function("CLOSURE", Closure);
      //  CONSTARRAY : value * number * number -> array
      new Function("CONSTARRAY", MakeFunction(ConstArray));
      // CHOOSE: number * any* -> any
      new Function("CHOOSE",  // Variadic, and non-strict in arg 2...
          delegate(Sheet sheet, Expr[] es, int col, int row) {
            if (es.Length >= 1) {
              Value v0 = es[0].Eval(sheet, col, row);
              NumberValue n0 = v0 as NumberValue;
              if (n0 != null) {
                int index = (int)n0.value;
                if (1 <= index && index < es.Length)
                  return es[index].Eval(sheet, col, row);
                else
                  return ErrorValue.valueError;
              } else if (v0 is ErrorValue)
                return v0;
              else
                return ErrorValue.argTypeError;
            } else
              return ErrorValue.argCountError;
          });
      //  COLMAP : function * array -> array
      new Function("COLMAP", MakeFunction(ColMap));
      // COLS : array -> number 
      new Function("COLUMNS", MakeNumberFunction(Columns));
      //  COUNTIF : area * fExpr -> number
      new Function("COUNTIF", MakeFunction(CountIf));
      // DEFINE(sdfname,outputcell,inputcells...) defines a sheet-defined function
      new Function("DEFINE",
      delegate(Sheet sheet, Expr[] es, int col, int row) {
        if (!sheet.IsFunctionSheet)
          return ErrorValue.Make("#FUNERR: Non-function sheet");
        if (es.Length < 2)
          return ErrorValue.argCountError;
        TextConst nameConst = es[0] as TextConst;
        if (nameConst == null)
          return ErrorValue.argTypeError;
        String name = nameConst.value.value.ToUpper();
        CellRef outputCell = es[1] as CellRef;
        if (outputCell == null)
          return ErrorValue.argTypeError;
        if (outputCell.sheet != null && outputCell.sheet != sheet)
          return ErrorValue.Make("#FUNERR: Output on another sheet");
        FullCellAddr output = outputCell.GetAbsoluteAddr(sheet, col, row);
        List<FullCellAddr> inputCells = new List<FullCellAddr>();
        for (int i = 2; i < es.Length; i++) {
          CellRef inputCell = es[i] as CellRef;
          if (inputCell == null)
            return ErrorValue.argTypeError;
          else if (inputCell.sheet != null && inputCell.sheet != sheet)
            return ErrorValue.Make("#FUNERR: Input on another sheet");
          else
            inputCells.Add(inputCell.GetAbsoluteAddr(sheet, col, row));
        }
        try {
          SdfManager.CreateFunction(name, output, inputCells);
        } catch (CyclicException) {
          return ErrorValue.Make("#FUNERR: Cyclic dependency in function");
        }
        return TextValue.MakeInterned(SdfManager.GetInfo(name).ToString());
      });
      // EQUAL(e1, e2) returns 1 if values e1 and e2 are non-errors and equal
      new Function("EQUAL", MakeNumberFunction(Equal));
      // ERR("message") produces an ErrorValue with the given constant message
      new Function("ERR",
      delegate(Sheet sheet, Expr[] es, int col, int row) {
        if (es.Length != 1)
          return ErrorValue.argCountError;
        TextConst messageConst = es[0] as TextConst;
        if (messageConst == null)
          return ErrorValue.argTypeError;
        return ErrorValue.Make("#ERR: " + messageConst.value.value);
      });
      // EXTERN("nameAndSignature", e1, ..., en) calls a .NET function "name"
      // having the given "signature", passing it converted values of e1...en 
      new Function("EXTERN", CallExtern);
      // HARRAY: value* -> array
      new Function("HARRAY", MakeFunction(HArray));
      // HCAT: value* -> array
      new Function("HCAT", MakeFunction(HCat));
      // HSCAN: function * array * number -> array
      new Function("HSCAN", MakeFunction(HScan));
      // IF : number any any -> any, 
      new Function("IF",                  // Note: non-strict in arg 2 and 3
          delegate(Sheet sheet, Expr[] es, int col, int row) {
            if (es.Length == 3) {
              Value v0 = es[0].Eval(sheet, col, row);
              NumberValue n0 = v0 as NumberValue;
              if (n0 != null && !Double.IsInfinity(n0.value) && !Double.IsNaN(n0.value))
                if (n0.value != 0)
                  return es[1].Eval(sheet, col, row);
                else
                  return es[2].Eval(sheet, col, row);
              else if (v0 is ErrorValue)
                return v0;
              else
                return ErrorValue.argTypeError;
            } else
              return ErrorValue.argCountError;
          });
      // INDEX: any* * number * number  -> any     // (row, col) indexing, offset base 1
      new Function("INDEX", MakeFunction(Index));
      // ISARRAY : value -> number
      new Function("ISARRAY", MakeNumberFunction(IsArray));
      // ISERROR : value -> number -- NOT ErrorValue-strict
      new Function("ISERROR", MakeNumberFunction(IsError));
      // MAP: array * function -> array
      new Function("MAP", MakeFunction(Map));
      // MAX: value * -> number
      new Function("MAX", MakeNumberFunction(Max));
      // MIN: value * -> number
      new Function("MIN", MakeNumberFunction(Min));
      // NOT : number -> number, 
      new Function("NOT",
        MakeNumberFunction(delegate(double n0) {
          return Double.IsNaN(n0) ? n0 : n0 == 0 ? 1.0 : 0.0;
        }));
      // OR : number number -> number, 
      new Function("OR",  // Variadic, and non-strict in args 2, 3, ...
          delegate(Sheet sheet, Expr[] es, int col, int row) {
            for (int i = 0; i < es.Length; i++) {
              Value vi = es[i].Eval(sheet, col, row);
              NumberValue ni = vi as NumberValue;
              if (ni != null) {
                if (ni.value != 0)
                  return NumberValue.ONE;
              } else if (vi is ErrorValue)
                return vi;
              else
                return ErrorValue.argTypeError;
            }
            return NumberValue.ZERO;
          });
      // REDUCE: function * value * array -> value
      new Function("REDUCE", MakeFunction(Reduce));
      // ROWMAP : function * array -> array
      new Function("ROWMAP", MakeFunction(RowMap));
      // ROWS : array -> number 
      new Function("ROWS", MakeNumberFunction(Rows));
      // SIGN : number -> number
      new Function("SIGN", MakeNumberFunction(Sign));
      // SLICE : array * number * number * number * number -> value
      new Function("SLICE", MakeFunction(Slice));
      // SPECIALIZE(fv) creates a specialized SDF from closure fv
      new Function("SPECIALIZE", MakeFunction(Specialize));
      // SUM : { number, array } * -> number
      new Function("SUM", MakeNumberFunction(SumNew));
      // SUMIF : function * array -> number
      new Function("SUMIF", MakeFunction(SumIf));
      // TABULATE : function * number * number -> array
      new Function("TABULATE", MakeFunction(Tabulate));
      // TRANSPOSE : array -> array
      new Function("TRANSPOSE", MakeFunction(Transpose));
      // VARRAY: value* -> array
      new Function("VARRAY", MakeFunction(VArray));
      // VCAT: value* -> array
      new Function("VCAT", MakeFunction(VCat));
      // VOLATILIZE(e1) is exactly as e1, but is volatile
      new Function("VOLATILIZE", MakeFunction(v => v), isVolatile: true);
      // VSCAN: function * array * number -> array
      new Function("VSCAN", MakeFunction(VScan));
    }

    private Function(String name, int fixity, Applier applier)
      : this(name, applier, fixity: fixity) { }

    internal Function(String name, Applier applier, int fixity = 0,
      bool placeHolder = false, bool isVolatile = false) {
      this.name = name;
      this.Applier = applier;
      this.fixity = fixity;
      this.IsPlaceHolder = placeHolder;
      this.isVolatile = isVolatile;
      table[name] = this;  // For Funsheet
    }

    public static Function MakeUnknown(String name) {
      return new Function(name.ToUpper(),
                          applier: delegate { return ErrorValue.nameError; },
                          placeHolder: true);
    }

    // Number-valued nullary function, eg RAND()
    private static Applier MakeNumberFunction(Func<double> dlg) {
      return
        delegate(Sheet sheet, Expr[] es, int col, int row) {
          if (es.Length == 0)
            return NumberValue.Make(dlg());
          else
            return ErrorValue.argCountError;
        };
    }

    // Number-valued constant nullary function, eg PI()
    private static Applier MakeConstant(Value value) {
      return
        delegate(Sheet sheet, Expr[] es, int col, int row) {
          if (es.Length == 0)
            return value;
          else
            return ErrorValue.argCountError;
        };
    }

    // Number-valued strict unary function, eg SIN(x)
    private static Applier MakeNumberFunction(Func<double, double> dlg) {
      return
        delegate(Sheet sheet, Expr[] es, int col, int row) {
          if (es.Length == 1) {
            Value v0 = es[0].Eval(sheet, col, row);
            return NumberValue.Make(dlg(Value.ToDoubleOrNan(v0)));
          } else
            return ErrorValue.argCountError;
        };
    }

    // Number-valued strict unary function, eg ISARRAY, ISERROR
    private static Applier MakeNumberFunction(Func<Value, double> dlg) {
      return
        delegate(Sheet sheet, Expr[] es, int col, int row) {
          if (es.Length == 1) {
            Value v0 = es[0].Eval(sheet, col, row);
            return NumberValue.Make(dlg(v0));
          } else
            return ErrorValue.argCountError;
        };
    }

    // Number-valued strict binary function, eg +
    private static Applier MakeNumberFunction(Func<double, double, double> dlg) {
      return
        delegate(Sheet sheet, Expr[] es, int col, int row) {
          if (es.Length == 2) {
            Value v0 = es[0].Eval(sheet, col, row),
                  v1 = es[1].Eval(sheet, col, row);
            return NumberValue.Make(dlg(Value.ToDoubleOrNan(v0), Value.ToDoubleOrNan(v1)));
          } else
            return ErrorValue.argCountError;
        };
    }

    // Number-valued strict binary function, eg EQUAL(e1,e2)
    private static Applier MakeNumberFunction(Func<Value, Value, double> dlg) {
      return
        delegate(Sheet sheet, Expr[] es, int col, int row) {
          if (es.Length == 2) {
            Value v0 = es[0].Eval(sheet, col, row),
                  v1 = es[1].Eval(sheet, col, row);
            return NumberValue.Make(dlg(v0, v1));
          } else
            return ErrorValue.argCountError;
        };
    }

    // Boolean-valued strict binary function, eg ==
    private static Applier MakePredicate(Func<double, double, bool> dlg) {
      return
        MakeNumberFunction(delegate(double x, double y) {
          return Double.IsNaN(x) ? x : Double.IsNaN(y) ? y : dlg(x, y) ? 1.0 : 0.0;
      });
    }

    // Number-valued strict variadic function, eg SUM, AVERAGE
    private static Applier MakeNumberFunction(Func<Value[], double> dlg) {
      return
        delegate(Sheet sheet, Expr[] es, int col, int row) {
          try {
            return NumberValue.Make(dlg(Eval(es, sheet, col, row)));
          } catch (ArgumentException) {
            return ErrorValue.argTypeError;
          }
        };
    }

    // Strict unary function, eg SPECIALIZE, TRANSPOSE
    private static Applier MakeFunction(Func<Value, Value> dlg) {
      return
        delegate(Sheet sheet, Expr[] es, int col, int row) {
          if (es.Length == 1) 
            return dlg(es[0].Eval(sheet, col, row));
          else
            return ErrorValue.argCountError;
        };
    }

    // Strict binary function, eg &, COLMAP, SUMIF
    private static Applier MakeFunction(Func<Value, Value, Value> dlg) {
      return
        delegate(Sheet sheet, Expr[] es, int col, int row) {
          if (es.Length == 2) {
            Value v0 = es[0].Eval(sheet, col, row),
                  v1 = es[1].Eval(sheet, col, row);
            return dlg(v0, v1);
          } else
            return ErrorValue.argCountError;
        };
    }

    // Strict ternary function, eg REDUCE, TABULATE
    private static Applier MakeFunction(Func<Value, Value, Value, Value> dlg) {
      return
        delegate(Sheet sheet, Expr[] es, int col, int row) {
          if (es.Length == 3) {
            Value v0 = es[0].Eval(sheet, col, row),
                  v1 = es[1].Eval(sheet, col, row),
                  v2 = es[2].Eval(sheet, col, row);
            return dlg(v0, v1, v2);
          } else
            return ErrorValue.argCountError;
        };
    }

    // Strict ternary function, eg INDEX
    private static Applier MakeFunction(Func<Value, double, double, Value> dlg) {
      return
        delegate(Sheet sheet, Expr[] es, int col, int row) {
          if (es.Length == 3) {
            Value v0 = es[0].Eval(sheet, col, row),
                  v1 = es[1].Eval(sheet, col, row),
                  v2 = es[2].Eval(sheet, col, row);
            return dlg(v0, Value.ToDoubleOrNan(v1), Value.ToDoubleOrNan(v2));
          } else
            return ErrorValue.argCountError;
        };
    }

    // Strict quinternary function, eg SLICE
    private static Applier MakeFunction(Func<Value, double, double, double, double, Value> dlg) {
      return
        delegate(Sheet sheet, Expr[] es, int col, int row) {
          if (es.Length == 5) {
            Value v0 = es[0].Eval(sheet, col, row);
            double n1 = Value.ToDoubleOrNan(es[1].Eval(sheet, col, row)),
                   n2 = Value.ToDoubleOrNan(es[2].Eval(sheet, col, row)),
                   n3 = Value.ToDoubleOrNan(es[3].Eval(sheet, col, row)),
                   n4 = Value.ToDoubleOrNan(es[4].Eval(sheet, col, row));
            return dlg(v0, n1, n2, n3, n4);
          } else
            return ErrorValue.argCountError;
        };
    }

    // Strict variadic function, eg MAP
    private static Applier MakeFunction(Func<Value[], Value> dlg) {
      return
        delegate(Sheet sheet, Expr[] es, int col, int row) {
          Value[] vs = Eval(es, sheet, col, row);
          return dlg(vs);
        };
    }

    // Evaluate expression array
    private static Value[] Eval(Expr[] es, Sheet sheet, int col, int row) {
      Value[] vs = new Value[es.Length];
      for (int i = 0; i < es.Length; i++)
        vs[i] = es[i].Eval(sheet, col, row);
      return vs;
    }
  }
}
