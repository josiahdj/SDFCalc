using System;
using System.Collections.Generic;
using System.Reflection;

namespace Corecalc.Funcalc {
	/// <summary>
	/// A FunctionInfo is a table of built-in functions: each function's 
	/// signature, MethodInfo object, and other information.
	/// </summary>
	public class FunctionInfo {
		public readonly String name; // For lookup and display
		public readonly MethodInfo methodInfo; // For code generation
		public readonly Signature signature; // For arg. compilation
		public readonly bool isSerious; // Cache it or not?
		public readonly Applier applier; // For specialization

		private static readonly IDictionary<String, FunctionInfo>
			functions = new Dictionary<String, FunctionInfo>();

		// Some signatures for fixed-arity built-in functions; return type first
		private static readonly Signature
			numToNum = new Signature(Typ.Number, Typ.Number),
			numNumToNum = new Signature(Typ.Number, Typ.Number, Typ.Number),
			unitToNum = new Signature(Typ.Number),
			valuesToNum = new Signature(Typ.Number, null),
			// Variable arity
			valuesToValue = new Signature(Typ.Value, null),
			// Variable arity
			valueToNum = new Signature(Typ.Number, Typ.Value),
			valueNumNumToValue = new Signature(Typ.Value, Typ.Value, Typ.Number, Typ.Number),
			valueValueToNum = new Signature(Typ.Number, Typ.Value, Typ.Value),
			valueToValue = new Signature(Typ.Value, Typ.Value),
			valueValueToValue = new Signature(Typ.Value, Typ.Value, Typ.Value),
			valueValueValueToValue = new Signature(Typ.Value, Typ.Value, Typ.Value, Typ.Value),
			valueNumNumNumNumToValue = new Signature(Typ.Value, Typ.Value, Typ.Number, Typ.Number, Typ.Number, Typ.Number);

		// Fixed-arity built-in functions that do not require special treatment.
		// Those that require special treatment are in CGComposite.Make.
		static FunctionInfo() {
			MakeMath1("Abs");
			MakeMath1("Acos");
			MakeMath1("Asin");
			MakeMath1("Atan");
			MakeFunction("ATAN2", "ExcelAtan2", numNumToNum, false);
			MakeFunction("AVERAGE", "Average", valuesToNum, true);
			MakeFunction("BENCHMARK", "Benchmark", valueValueToValue, true);
			MakeFunction("CEILING", "ExcelCeiling", numNumToNum, false);
			MakeFunction("COLMAP", "ColMap", valueValueToValue, true);
			MakeFunction("COLUMNS", "Columns", valueToNum, false);
			MakeFunction("&", "ExcelConcat", valueValueToValue, false);
			MakeFunction("CONSTARRAY", "ConstArray", valueValueValueToValue, true);
			MakeMath1("Cos");
			MakeFunction("COUNTIF", "CountIf", valueValueToValue, true);
			MakeFunction("EQUAL", "Equal", valueValueToNum, false);
			MakeMath1("Exp");
			MakeFunction("FLOOR", "ExcelFloor", numNumToNum, false);
			MakeFunction("HARRAY", "HArray", valuesToValue, true);
			MakeFunction("HCAT", "HCat", valuesToValue, true);
			MakeFunction("HSCAN", "HScan", valueValueValueToValue, true);
			MakeFunction("INDEX", "Index", valueNumNumToValue, false);
			MakeFunction("ISARRAY", "IsArray", valueToNum, false);
			MakeFunction("ISERROR", "IsError", valueToNum, false);
			MakeMath1("LN", "Log");
			MakeMath1("LOG", "Log10");
			MakeMath1("LOG10", "Log10");
			MakeFunction("MAP", "Map", valuesToValue, true);
			MakeFunction("MAX", "Max", valuesToNum, true);
			MakeFunction("MIN", "Min", valuesToNum, true);
			MakeFunction("MOD", "ExcelMod", numNumToNum, false);
			MakeFunction("NOW", "ExcelNow", unitToNum, false);
			MakeFunction("^", "ExcelPow", numNumToNum, false);
			MakeFunction("RAND", "ExcelRand", unitToNum, false);
			MakeFunction("REDUCE", "Reduce", valueValueValueToValue, true);
			MakeFunction("ROUND", "ExcelRound", numNumToNum, false);
			MakeFunction("ROWMAP", "RowMap", valueValueToValue, true);
			MakeFunction("ROWS", "Rows", valueToNum, false);
			MakeFunction("SIGN", "Sign", numToNum, false);
			MakeMath1("Sin");
			MakeFunction("SLICE", "Slice", valueNumNumNumNumToValue, false);
			MakeFunction("SPECIALIZE", "Specialize", valueToValue, true);
			MakeMath1("Sqrt");
			MakeFunction("SUM", "Sum", valuesToNum, true);
			MakeFunction("SUMIF", "SumIf", valueValueToValue, true);
			MakeFunction("TABULATE", "Tabulate", valueValueValueToValue, true);
			MakeMath1("Tan");
			MakeFunction("TRANSPOSE", "Transpose", valueToValue, true);
			MakeFunction("VARRAY", "VArray", valuesToValue, true);
			MakeFunction("VCAT", "VCat", valuesToValue, true);
			MakeFunction("VSCAN", "VScan", valueValueValueToValue, true);
		}

		public FunctionInfo(String name, MethodInfo methodInfo, Applier applier, Signature signature, bool isSerious) {
			this.name = name.ToUpper();
			this.methodInfo = methodInfo;
			this.applier = applier;
			this.signature = signature;
			functions[this.name] = this; // For Funsheet
			this.isSerious = isSerious;
		}

		private static FunctionInfo MakeMath1(String funcalcName, String mathName) {
			return new FunctionInfo(funcalcName.ToUpper(),
									typeof (Math).GetMethod(mathName, new Type[] {typeof (double)}),
									Function.Get(funcalcName.ToUpper()).Applier,
									numToNum,
									isSerious: false);
		}

		private static FunctionInfo MakeMath1(String name) { return MakeMath1(name, name); }

		private static FunctionInfo MakeFunction(String ssName,
												 MethodInfo method,
												 Signature signature,
												 bool isSerious) {
			return new FunctionInfo(ssName,
									method,
									Function.Get(ssName.ToUpper()).Applier,
									signature,
									isSerious);
		}

		private static FunctionInfo MakeFunction(String ssName,
												 String implementationName,
												 Signature signature,
												 bool isSerious) {
			return new FunctionInfo(ssName,
									Function.type.GetMethod(implementationName),
									Function.Get(ssName.ToUpper()).Applier,
									signature,
									isSerious);
		}

		public static bool Find(String name, out FunctionInfo functionInfo) { return functions.TryGetValue(name, out functionInfo); }
	}
}