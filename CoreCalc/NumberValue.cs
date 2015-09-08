using System;
using System.Diagnostics;
using System.Reflection;

namespace Corecalc {
	/// <summary>
	/// A NumberValue holds a floating-point number resulting from evaluation.
	/// </summary>
	public class NumberValue : Value {
		public readonly double value;

		public static readonly NumberValue
			ZERO = new NumberValue(0.0),
			ONE = new NumberValue(1.0),
			PI = new NumberValue(Math.PI);

		public new static readonly Type type = typeof (NumberValue);

		public static readonly FieldInfo
			zeroField = type.GetField("ZERO"),
			oneField = type.GetField("ONE"),
			piField = type.GetField("PI");

		public static readonly MethodInfo
			makeMethod = type.GetMethod("Make", new Type[] {typeof (double)});

		private NumberValue(double value) {
			Debug.Assert(!Double.IsInfinity(value) && !Double.IsNaN(value));
			this.value = value;
		}

		public static Value Make(double d) {
			if (double.IsInfinity(d)) {
				return ErrorValue.numError;
			}
			else if (double.IsNaN(d)) {
				return ErrorValue.FromNan(d);
			}
			else if (d == 0) {
				return ZERO;
			}
			else if (d == 1) {
				return ONE;
			}
			else {
				return new NumberValue(d);
			}
		}

		public override bool Equals(Value v) { return v is NumberValue && (v as NumberValue).value == value; }

		public override int GetHashCode() { return value.GetHashCode(); }

		public override Object ToObject() { return (Object)value; }

		public static Object ToDouble(Value v) {
			NumberValue nv = v as NumberValue;
			return nv != null ? (Object)nv.value : null; // Causes boxing
		}

		public static Value FromDouble(Object o) {
			if (o is double) {
				return Make((double)o);
			}
			else {
				return ErrorValue.numError;
			}
		}

		public static Object ToSingle(Value v) {
			NumberValue nv = v as NumberValue;
			return nv != null ? (Object)(float)nv.value : null; // Causes boxing
		}

		public static Value FromSingle(Object o) {
			if (o is float) {
				return Make((float)o);
			}
			else {
				return ErrorValue.numError;
			}
		}

		public static Object ToInt64(Value v) {
			NumberValue nv = v as NumberValue;
			return nv != null ? (Object)(long)nv.value : null; // Causes boxing
		}

		public static Value FromInt64(Object o) {
			if (o is long) {
				return Make((long)o);
			}
			else {
				return ErrorValue.numError;
			}
		}

		public static Object ToInt32(Value v) {
			NumberValue nv = v as NumberValue;
			return nv != null ? (Object)(int)nv.value : null; // Causes boxing
		}

		public static Value FromInt32(Object o) {
			if (o is int) {
				return Make((int)o);
			}
			else {
				return ErrorValue.numError;
			}
		}

		public static Object ToInt16(Value v) {
			NumberValue nv = v as NumberValue;
			return nv != null ? (Object)(short)nv.value : null; // Causes boxing
		}

		public static Value FromInt16(Object o) {
			if (o is short) {
				return Make((short)o);
			}
			else {
				return ErrorValue.numError;
			}
		}

		public static Object ToSByte(Value v) {
			NumberValue nv = v as NumberValue;
			return nv != null ? (Object)(sbyte)nv.value : null; // Causes boxing
		}

		public static Value FromSByte(Object o) {
			if (o is sbyte) {
				return Make((sbyte)o);
			}
			else {
				return ErrorValue.numError;
			}
		}

		public static Object ToUInt64(Value v) {
			NumberValue nv = v as NumberValue;
			return nv != null ? (Object)(ulong)nv.value : null; // Causes boxing
		}

		public static Value FromUInt64(Object o) {
			if (o is ulong) {
				return Make((ulong)o);
			}
			else {
				return ErrorValue.numError;
			}
		}

		public static Object ToUInt32(Value v) {
			NumberValue nv = v as NumberValue;
			return nv != null ? (Object)(uint)nv.value : null; // Causes boxing
		}

		public static Value FromUInt32(Object o) {
			if (o is uint) {
				return Make((uint)o);
			}
			else {
				return ErrorValue.numError;
			}
		}

		public static Object ToUInt16(Value v) {
			NumberValue nv = v as NumberValue;
			return nv != null ? (Object)(ushort)nv.value : null; // Causes boxing
		}

		public static Value FromUInt16(Object o) {
			if (o is ushort) {
				return Make((ushort)o);
			}
			else {
				return ErrorValue.numError;
			}
		}

		public static Object ToByte(Value v) {
			NumberValue nv = v as NumberValue;
			return nv != null ? (Object)(byte)nv.value : null; // Causes boxing
		}

		public static Value FromByte(Object o) {
			if (o is byte) {
				return Make((byte)o);
			}
			else {
				return ErrorValue.numError;
			}
		}

		public static Object ToBoolean(Value v) {
			NumberValue nv = v as NumberValue;
			return nv != null ? (Object)(nv.value != 0) : null; // Causes boxing
		}

		public static Value FromBoolean(Object o) {
			if (o is bool) {
				return (bool)o ? ONE : ZERO;
			}
			else {
				return ErrorValue.numError;
			}
		}

		// Conversion between System.DateTime ticks and Excel-style date numbers
		private static readonly long basedate = new DateTime(1899, 12, 30).Ticks;
		private static readonly double daysPerTick = 100E-9/60/60/24;

		public static double DoubleFromDateTimeTicks(long ticks) { return (ticks - basedate)*daysPerTick; }

		public override String ToString() { return value.ToString(); }
	}
}