using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Corecalc.Funcalc {
	/// <summary>
	/// An ExternalFunction represents an external .NET function, conversion 
	/// of its arguments from spreadsheet Values to .NET values, conversion of 
	/// its result value in the opposite direction, its .NET MethodInfo for 
	/// invoking it, its .NET signature, and more.
	/// </summary>
	internal class ExternalFunction {
		private readonly Func<Value, object>[] argConverters;
		private readonly Func<Object, Value> resConverter;
		private readonly MethodBase mcInfo; // MethodInfo or ConstructorInfo
		private readonly Type[] argTypes; // Does not include receiver type
		private readonly Object[] argValues; // Reused from call to call
		private readonly Type resType; // Result type
		private readonly bool isStatic;
		private readonly Type recType; // Receiver type
		private readonly Func<Value, Object> recConverter; // Receiver converter 
		public readonly int arity; // Includes receiver if !isStatic

		// Invariant: argTypes.Length == argConverters.Length == argValues.Length
		// Invariant: if isStatic then arity==argTypes.Length else arity==argTypes.Length+1
		// Invariant: if !isStatic then recType != null and recConverter != null
		// Invariant: argConverters[i] == toObjectConverter[argTypes[i]]
		// Invariant: recConverter == null || recConverter = toObjectConverter[recType]
		// Invariant: resConverter == fromObjectConverter[resType]

		// Mapping .NET types to argument and result converters: Value <-> Object
		private static readonly IDictionary<Type, Func<Value, Object>> toObjectConverter
			= new Dictionary<Type, Func<Value, Object>>();

		private static IDictionary<Type, Func<Object, Value>> fromObjectConverter
			= new Dictionary<Type, Func<Object, Value>>();

		static ExternalFunction() {
			// Initialize tables of conversions
			// From Funcalc type to .NET type, for argument converters
			toObjectConverter.Add(typeof (System.Int64), NumberValue.ToInt64);
			toObjectConverter.Add(typeof (System.Int32), NumberValue.ToInt32);
			toObjectConverter.Add(typeof (System.Int16), NumberValue.ToInt16);
			toObjectConverter.Add(typeof (System.SByte), NumberValue.ToSByte);
			toObjectConverter.Add(typeof (System.UInt64), NumberValue.ToUInt64);
			toObjectConverter.Add(typeof (System.UInt32), NumberValue.ToUInt32);
			toObjectConverter.Add(typeof (System.UInt16), NumberValue.ToUInt16);
			toObjectConverter.Add(typeof (System.Byte), NumberValue.ToByte);
			toObjectConverter.Add(typeof (System.Double), NumberValue.ToDouble);
			toObjectConverter.Add(typeof (System.Single), NumberValue.ToSingle);
			toObjectConverter.Add(typeof (System.Boolean), NumberValue.ToBoolean);
			toObjectConverter.Add(typeof (System.String), TextValue.ToString);
			toObjectConverter.Add(typeof (System.Char), TextValue.ToChar);
			toObjectConverter.Add(typeof (System.Object), Value.ToObject);
			toObjectConverter.Add(typeof (System.Double[]), ArrayValue.ToDoubleArray1D);
			toObjectConverter.Add(typeof (System.Double[,]), ArrayValue.ToDoubleArray2D);
			toObjectConverter.Add(typeof (System.String[]), ArrayValue.ToStringArray1D);

			// From .NET type to Funcalc type, for result converters
			fromObjectConverter.Add(typeof (System.Int64), NumberValue.FromInt64);
			fromObjectConverter.Add(typeof (System.Int32), NumberValue.FromInt32);
			fromObjectConverter.Add(typeof (System.Int16), NumberValue.FromInt16);
			fromObjectConverter.Add(typeof (System.SByte), NumberValue.FromSByte);
			fromObjectConverter.Add(typeof (System.UInt64), NumberValue.FromUInt64);
			fromObjectConverter.Add(typeof (System.UInt32), NumberValue.FromUInt32);
			fromObjectConverter.Add(typeof (System.UInt16), NumberValue.FromUInt16);
			fromObjectConverter.Add(typeof (System.Byte), NumberValue.FromByte);
			fromObjectConverter.Add(typeof (System.Double), NumberValue.FromDouble);
			fromObjectConverter.Add(typeof (System.Single), NumberValue.FromSingle);
			fromObjectConverter.Add(typeof (System.Boolean), NumberValue.FromBoolean);
			fromObjectConverter.Add(typeof (System.String), TextValue.FromString);
			fromObjectConverter.Add(typeof (System.Char), TextValue.FromChar);
			fromObjectConverter.Add(typeof (System.Object), ObjectValue.Make);
			fromObjectConverter.Add(typeof (System.String[]), ArrayValue.FromStringArray1D);
			fromObjectConverter.Add(typeof (System.Double[]), ArrayValue.FromDoubleArray1D);
			fromObjectConverter.Add(typeof (System.Double[,]), ArrayValue.FromDoubleArray2D);
			fromObjectConverter.Add(typeof (void), Value.MakeVoid);
		}

		// For caching the result of nameAndSignature lookups
		private static readonly IDictionary<String, ExternalFunction> cache
			= new Dictionary<String, ExternalFunction>();

		public static ExternalFunction Make(String nameAndSignature) {
			ExternalFunction res;
			if (!cache.TryGetValue(nameAndSignature, out res)) {
				res = new ExternalFunction(nameAndSignature);
				cache.Add(nameAndSignature, res);
			}
			return res;
		}

		private ExternalFunction(String nameAndSignature) {
			int firstParen = nameAndSignature.IndexOf('(');
			if (firstParen <= 0 || firstParen == nameAndSignature.Length - 1) {
				throw new Exception("#ERR: Ill-formed name and signature");
			}
			isStatic = nameAndSignature[firstParen - 1] == '$';
			String name = nameAndSignature.Substring(0, isStatic ? firstParen - 1 : firstParen);
			String signature = nameAndSignature.Substring(firstParen);
			int lastDot = name.LastIndexOf('.');
			if (lastDot <= 0 || lastDot == name.Length - 1) {
				throw new Exception("#ERR: Ill-formed .NET method name");
			}
			String typeName = name.Substring(0, lastDot);
			String methodName = name.Substring(lastDot + 1);
			// Experimental: Search appdomain's assemblies
			Type declaringType = FindType(typeName);
			if (declaringType == null) {
				throw new Exception("#ERR: Unknown .NET type " + typeName);
			}
			FunctionType ft = SdfType.ParseType(signature) as FunctionType;
			if (ft == null) {
				throw new Exception("#ERR: Ill-formed .NET method signature");
			}
			argTypes = ft.ArgumentDotNetTypes();
			resType = ft.returntype.GetDotNetType();
			if (methodName == "new" && isStatic) {
				mcInfo = declaringType.GetConstructor(argTypes);
			}
			else {
				mcInfo = declaringType.GetMethod(methodName, argTypes);
			}
			if (mcInfo == null) {
				throw new Exception("#ERR: Unknown .NET method");
			}
			argConverters = new Func<Value, Object>[argTypes.Length];
			for (int i = 0; i < argTypes.Length; i++) {
				argConverters[i] = GetToObjectConverter(argTypes[i]);
			}
			resConverter = GetFromObjectConverter(ft.returntype.GetDotNetType());
			if (isStatic) {
				arity = argTypes.Length;
			}
			else {
				arity = argTypes.Length + 1;
				recType = declaringType;
				recConverter = GetToObjectConverter(recType);
			}
			argValues = new Object[argTypes.Length]; // Allocate once, reuse at calls
		}

		private static Func<Value, Object> GetToObjectConverter(Type typ) {
			if (toObjectConverter.ContainsKey(typ)) {
				return toObjectConverter[typ];
			}
			else {
				return ObjectValue.ToObject;
			}
			//return delegate(Value v) {
			//  ObjectValue o = v as ObjectValue;
			//  return o != null && typ.IsInstanceOfType(o.value) ? o.value : null;
			//};
		}

		private static Func<Object, Value> GetFromObjectConverter(Type typ) {
			if (fromObjectConverter.ContainsKey(typ)) {
				return fromObjectConverter[typ];
			}
			else {
				return ObjectValue.Make;
			}
			//return delegate(Object o) {
			//  return typ.IsInstanceOfType(o) ? ObjectValue.Make(o) : ErrorValue.argTypeError;
			//};
		}

		// Experimental: Search for type by name in some known assemblies

		private static readonly String[] assmNames = new String[] {
																	  "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
																	  "System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
																	  // This is for testing EXTERN functions:
																	  "Externals, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"
																  };

		public static Type FindType(String typeName) {
			Type declaringType = null;
			foreach (String assmName in assmNames) {
				Assembly assm = Assembly.Load(assmName);
				// Console.WriteLine(assm);
				declaringType = assm.GetType(typeName);
				if (declaringType != null) {
					break;
				}
			}
			return declaringType;
		}

		// Called from the EXTERN function applier in interpreted sheets.

		public Value Call(Value[] vs) {
			if (vs.Length != arity) {
				return ErrorValue.argCountError;
			}
			Object receiver;
			if (isStatic) {
				receiver = null;
				for (int i = 0; i < vs.Length; i++) {
					argValues[i] = argConverters[i](vs[i]);
				}
			}
			else {
				receiver = recConverter(vs[0]);
				for (int i = 1; i < vs.Length; i++) {
					argValues[i - 1] = argConverters[i - 1](vs[i]);
				}
			}
			if (mcInfo is ConstructorInfo) {
				return resConverter((mcInfo as ConstructorInfo).Invoke(argValues));
			}
			else {
				return resConverter(mcInfo.Invoke(receiver, argValues));
			}
		}

		// These are used by the SDF code generator in CGExtern

		public void EmitCall(ILGenerator ilg) {
			if (mcInfo is ConstructorInfo) {
				ilg.Emit(OpCodes.Newobj, mcInfo as ConstructorInfo);
			}
			else if (isStatic) {
				ilg.Emit(OpCodes.Call, mcInfo as MethodInfo);
			}
			else {
				ilg.Emit(OpCodes.Call, mcInfo as MethodInfo);
			}
		}

		public Type ArgType(int i) { return isStatic ? argTypes[i] : i > 0 ? argTypes[i - 1] : recType; }

		public Func<Value, Object> ArgConverter(int i) { return isStatic ? argConverters[i] : i > 0 ? argConverters[i - 1] : recConverter; }

		public Type ResType {
			get { return resType; }
		}

		public Func<Object, Value> ResConverter {
			get { return resConverter; }
		}
	}
}