using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

using Corecalc.Funcalc;

namespace CoreCalc.Values {
	/// <summary>
	/// A FunctionValue is a partially applied sheet-defined function, ie. a closure.
	/// </summary>
	public class FunctionValue : Value {
		// Depends on Funcalc.SdfInfo and so probably should be elsewhere.
		public readonly SdfInfo sdfInfo;
		public readonly Value[] args; // Invariant: args.Length==sdfInfo.Arity
		private readonly int arity; // Invariant: arity==number of #NA in args
		private readonly Delegate mergeAndCall;

		// Used by code generation for CGApply and CGClosure
		public new static readonly Type type = typeof (FunctionValue);

		public static readonly MethodInfo
			makeMethod = type.GetMethod("Make"),
			furtherApplyMethod = type.GetMethod("FurtherApply");

		private static readonly FieldInfo
			sdfInfoField = type.GetField("sdfInfo"),
			argsField = type.GetField("args");

		private static readonly Type[] mergeAndCallDelegateType
			= {
				  typeof (Func<FunctionValue, Value>),
				  typeof (Func<FunctionValue, Value, Value>),
				  typeof (Func<FunctionValue, Value, Value, Value>),
				  typeof (Func<FunctionValue, Value, Value, Value, Value>),
				  typeof (Func<FunctionValue, Value, Value, Value, Value, Value>),
				  typeof (Func<FunctionValue, Value, Value, Value, Value, Value, Value>),
				  typeof (Func<FunctionValue, Value, Value, Value, Value, Value, Value, Value>),
				  typeof (Func<FunctionValue, Value, Value, Value, Value, Value, Value, Value, Value>),
				  typeof (Func<FunctionValue, Value, Value, Value, Value, Value, Value, Value, Value, Value>),
				  typeof (Func<FunctionValue, Value, Value, Value, Value, Value, Value, Value, Value, Value, Value>),
				  typeof (Func<FunctionValue, Value, Value, Value, Value, Value, Value, Value, Value, Value, Value, Value>)
			  };

		private static readonly IDictionary<int, Delegate> mergeDelegateCache
			= new Dictionary<int, Delegate>();

		private static readonly Type[][] mergerArgTypes;

		public static readonly MethodInfo[] callMethods
			= {
				  type.GetMethod("Call0"),
				  type.GetMethod("Call1"),
				  type.GetMethod("Call2"),
				  type.GetMethod("Call3"),
				  type.GetMethod("Call4"),
				  type.GetMethod("Call5"),
				  type.GetMethod("Call6"),
				  type.GetMethod("Call7"),
				  type.GetMethod("Call8"),
				  type.GetMethod("Call9")
			  };

		static FunctionValue() {
			mergerArgTypes = new Type[mergeAndCallDelegateType.Length][];
			for (int a = 0; a < mergeAndCallDelegateType.Length; a++) {
				Type[] argTypes = mergerArgTypes[a] = new Type[a + 1];
				argTypes[0] = FunctionValue.type;
				for (int i = 0; i < a; i++) {
					argTypes[i + 1] = Value.type;
				}
			}
		}

		public FunctionValue(SdfInfo sdfInfo, Value[] args) {
			this.sdfInfo = sdfInfo;
			// A null or empty args array is equivalent to an array of all NA
			if (args == null || args.Length == 0) {
				args = new Value[sdfInfo.arity];
				for (int i = 0; i < args.Length; i++) {
					args[i] = ErrorValue.naError;
				}
			}
			// Requirement: There will be no further writes to the args array
			this.args = args;
			int k = 0;
			for (int i = 0; i < args.Length; i++) {
				if (args[i] == ErrorValue.naError) {
					k++;
				}
			}
			this.arity = k;
			this.mergeAndCall = MakeMergeAndCallMethod();
		}

		public override bool Equals(Object obj) { return Equals(obj as FunctionValue); }

		public override bool Equals(Value v) { return Equals(v as FunctionValue); }

		public bool Equals(FunctionValue that) {
			if (that == null
				|| this.sdfInfo.index != that.sdfInfo.index
				|| this.args.Length != that.args.Length) {
				return false;
			}
			for (int i = 0; i < args.Length; i++) {
				if (!this.args[i].Equals(that.args[i])) {
					return false;
				}
			}
			return true;
		}

		public override int GetHashCode() {
			int result = sdfInfo.index*37 + args.Length;
			for (int i = 0; i < args.Length; i++) {
				result = result*37 + args[i].GetHashCode();
			}
			return result;
		}

		public override Object ToObject() { return (Object)this; }

		// This array statically allocated and reusedto avoid allocation
		private static readonly Value[] allArgs = new Value[10];

		public Value Apply(Value[] vs) {
			if (arity != vs.Length) {
				return ErrorValue.argCountError;
			}
			else {
				MergeArgs(vs, allArgs);
				return sdfInfo.Apply(allArgs);
			}
		}

		// Replace #NA from args with late arguments in output.
		// There is a special version of this loop in CGSdfCall.PEval too
		private void MergeArgs(Value[] late, Value[] output) {
			int j = 0;
			for (int i = 0; i < args.Length; i++) {
				if (args[i] != ErrorValue.naError) {
					output[i] = args[i];
				}
				else {
					output[i] = late[j++];
				}
			}
		}

		// These methods are called both directly and by generated code/reflection:

		public static FunctionValue Make(int sdfIndex, Value[] vs) { return new FunctionValue(SdfManager.GetInfo(sdfIndex), vs); }

		public Value FurtherApply(Value[] vs) {
			if (vs.Length == 0) {
				return this;
			}
			else if (vs.Length != arity) {
				return ErrorValue.argCountError;
			}
			else {
				Value[] newArgs = new Value[args.Length];
				MergeArgs(vs, newArgs);
				return new FunctionValue(sdfInfo, newArgs);
			}
		}

		public Value Call0() { return sdfInfo.Apply(args); // Shortcut when no late arguments
		}

		public Value Call1(Value v1) {
			if (arity != 1) {
				return ErrorValue.argCountError;
			}
			else {
				var caller = (Func<FunctionValue, Value, Value>)mergeAndCall;
				return caller(this, v1);
			}
		}

		public Value Call2(Value v1, Value v2) {
			if (arity != 2) {
				return ErrorValue.argCountError;
			}
			else {
				var caller = (Func<FunctionValue, Value, Value, Value>)mergeAndCall;
				return caller(this, v1, v2);
			}
		}

		public Value Call3(Value v1, Value v2, Value v3) {
			if (arity != 3) {
				return ErrorValue.argCountError;
			}
			else {
				var caller = (Func<FunctionValue, Value, Value, Value, Value>)mergeAndCall;
				return caller(this, v1, v2, v3);
			}
		}

		public Value Call4(Value v1, Value v2, Value v3, Value v4) {
			if (arity != 4) {
				return ErrorValue.argCountError;
			}
			else {
				var caller = (Func<FunctionValue, Value, Value, Value, Value, Value>)mergeAndCall;
				return caller(this, v1, v2, v3, v4);
			}
		}

		public Value Call5(Value v1, Value v2, Value v3, Value v4, Value v5) {
			if (arity != 5) {
				return ErrorValue.argCountError;
			}
			else {
				var caller = (Func<FunctionValue, Value, Value, Value, Value, Value, Value>)mergeAndCall;
				return caller(this, v1, v2, v3, v4, v5);
			}
		}

		public Value Call6(Value v1, Value v2, Value v3, Value v4, Value v5, Value v6) {
			if (arity != 6) {
				return ErrorValue.argCountError;
			}
			else {
				var caller = (Func<FunctionValue, Value, Value, Value, Value, Value, Value, Value>)mergeAndCall;
				return caller(this, v1, v2, v3, v4, v5, v6);
			}
		}

		public Value Call7(Value v1, Value v2, Value v3, Value v4, Value v5, Value v6, Value v7) {
			if (arity != 7) {
				return ErrorValue.argCountError;
			}
			else {
				var caller = (Func<FunctionValue, Value, Value, Value, Value, Value, Value, Value, Value>)mergeAndCall;
				return caller(this, v1, v2, v3, v4, v5, v6, v7);
			}
		}

		public Value Call8(Value v1, Value v2, Value v3, Value v4, Value v5, Value v6, Value v7, Value v8) {
			if (arity != 8) {
				return ErrorValue.argCountError;
			}
			else {
				var caller = (Func<FunctionValue, Value, Value, Value, Value, Value, Value, Value, Value, Value>)mergeAndCall;
				return caller(this, v1, v2, v3, v4, v5, v6, v7, v8);
			}
		}

		public Value Call9(Value v1, Value v2, Value v3, Value v4, Value v5, Value v6, Value v7, Value v8, Value v9) {
			if (arity != 9) {
				return ErrorValue.argCountError;
			}
			else {
				var caller = (Func<FunctionValue, Value, Value, Value, Value, Value, Value, Value, Value, Value, Value>)mergeAndCall;
				return caller(this, v1, v2, v3, v4, v5, v6, v7, v8, v9);
			}
		}

		public int Arity {
			get { return arity; }
		}

		// Generate a method EntryN(fv,v1,...,vN) to merge early arguments from 
		// fv.args with late arguments v1...vN and call fv.sdfInfo delegate.
		// Assumptions: The call-time fv.args array has the same length as the 
		// generation-time args array, and call-time fv.arity equals generation-time arity. 

		private Delegate MakeMergeAndCallMethod() {
			Delegate result;
			int pattern = NaPattern(args);
			if (!mergeDelegateCache.TryGetValue(pattern, out result)) {
				// Console.WriteLine("Created function value merger pattern {0}", pattern);
				DynamicMethod method = new DynamicMethod("EntryN", Value.type, mergerArgTypes[Arity], true);
				ILGenerator ilg = method.GetILGenerator();
				// Load and cast the SDF delegate to call
				ilg.Emit(OpCodes.Ldsfld, SdfManager.sdfDelegatesField); // sdfDelegates
				ilg.Emit(OpCodes.Ldarg_0); // sdfDelegates, fv 
				ilg.Emit(OpCodes.Ldfld, sdfInfoField); // sdfDelegates, fv.sdfInfo
				ilg.Emit(OpCodes.Ldfld, SdfInfo.indexField); // sdfDelegates, fv.sdfInfo.index
				ilg.Emit(OpCodes.Ldelem_Ref); // sdfDelegates[fv.sdfInfo.index] 
				ilg.Emit(OpCodes.Castclass, sdfInfo.MyType); // sdf delegate of appropriate type
				// Save the early argument array to a local variable
				LocalBuilder argsArray = ilg.DeclareLocal(typeof (Value[]));
				ilg.Emit(OpCodes.Ldarg_0); // sdf, fv
				ilg.Emit(OpCodes.Ldfld, argsField); // sdf, fv.args
				ilg.Emit(OpCodes.Stloc, argsArray); // sdf
				// Do like MergeArgs(new Value[] { v1...vn }, ...) but without array overhead:
				int j = 1; // The first late argument is arg1
				for (int i = 0; i < args.Length; i++) {
					if (args[i] != ErrorValue.naError) { // Load early arguments from fv.args
						ilg.Emit(OpCodes.Ldloc, argsArray);
						ilg.Emit(OpCodes.Ldc_I4, i);
						ilg.Emit(OpCodes.Ldelem_Ref);
					}
					else // Load late arguments from the EntryN method's parameters
					{
						ilg.Emit(OpCodes.Ldarg, j++);
					}
				}
				ilg.Emit(OpCodes.Call, sdfInfo.MyInvoke);
				ilg.Emit(OpCodes.Ret);
				result = method.CreateDelegate(mergeAndCallDelegateType[arity]);
				mergeDelegateCache[pattern] = result;
			}
			return result;
		}

		// Convert a #NA-pattern to an index for caching purposes; think 
		// of the #NA-pattern as a dyadic number with #NA=1 and non-#NA=2
		private static int NaPattern(Value[] args) {
			int index = 0;
			for (int i = 0; i < args.Length; i++) {
				index = 2*index + (args[i] == ErrorValue.naError ? 1 : 2);
			}
			return index;
		}

		public static String FormatAsCall(String name, params Object[] args) {
			StringBuilder sb = new StringBuilder();
			sb.Append(name).Append("(");
			if (args.Length > 0) {
				sb.Append(args[0]);
			}
			for (int i = 1; i < args.Length; i++) {
				sb.Append(",").Append(args[i]);
			}
			return sb.Append(")").ToString();
		}

		public override String ToString() { // Don't show args if all are #NA
			return Arity < args.Length ? FormatAsCall(sdfInfo.name, args) : sdfInfo.name;
		}
	}
}