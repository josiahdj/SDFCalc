using System;
using System.Reflection;

namespace Corecalc {
	/// <summary>
	/// An ErrorValue is a value indicating failure of evaluation.
	/// </summary>
	public class ErrorValue : Value {
		public readonly String message;
		public readonly int index;

		// Standard ErrorValue objects and static fields, shared between all functions:
		public new static readonly Type type = typeof(ErrorValue);

		public static readonly MethodInfo
			fromNanMethod = type.GetMethod("FromNan"),
			fromIndexMethod = type.GetMethod("FromIndex");

		// Caching ErrorValues by message string, and for access by integer index
		private static readonly ValueCache<String, ErrorValue> errorTable
			= new ValueCache<string, ErrorValue>((index, message) => new ErrorValue(message, index));

		public static readonly ErrorValue
			// The numError is first so it gets indedunx zero; necessary because
			// System.Math functions produce NaN with error code zero:
			numError =         Make("#NUM!"),
			argCountError =    Make("#ERR: ArgCount"),
			argTypeError =     Make("#ERR: ArgType"),
			nameError =        Make("#NAME?"),
			refError =         Make("#REF!"),
			valueError =       Make("#VALUE!"),
			naError =          Make("#NA"),
			tooManyArgsError = Make("#ERR: Too many arguments");

		private ErrorValue(String message, int errorIndex) {
			this.message = message;
			this.index = errorIndex;
		}

		public static int GetIndex(String message) {
			return errorTable.GetIndex(message);
		}

		public static ErrorValue Make(String message) {
			return errorTable[errorTable.GetIndex(message)];
		}

		public double ErrorNan {
			get { return MakeNan(index); }
		}

		// These two are also called from compiled code, through reflection:

		public static ErrorValue FromNan(double d) {
			return errorTable[ErrorCode(d)];
		}

		public static ErrorValue FromIndex(int errorIndex) {
			return errorTable[errorIndex];
		}

		public override bool Equals(Value v) {
			return v is ErrorValue && (v as ErrorValue).index == index;
		}

		public override int GetHashCode() {
			return index;
		}

		public override Object ToObject() {
			return (Object)this;
		}

		public override String ToString() {
			return message;
		}

		// From error code index (int) to NaN (double) and back

		public static double MakeNan(int errorIndex) {
			long nanbits = System.BitConverter.DoubleToInt64Bits(Double.NaN);
			return System.BitConverter.Int64BitsToDouble(nanbits | (uint)errorIndex);
		}

		public static int ErrorCode(double d) {
			return (int)System.BitConverter.DoubleToInt64Bits(d);
		}
	}
}