using System;
using System.Diagnostics;
using System.Reflection;

namespace Corecalc {
	/// <summary>
	/// A TextValue holds a string resulting from evaluation.
	/// </summary>
	public class TextValue : Value {
		public readonly String value;  // Non-null

		public new static readonly Type type = typeof(TextValue);
		public static readonly FieldInfo
			valueField = type.GetField("value"),
			emptyField = type.GetField("EMPTY"),
			voidField = type.GetField("VOID");
		public static readonly MethodInfo
			fromIndexMethod = type.GetMethod("FromIndex"),
			fromNakedCharMethod = type.GetMethod("FromNakedChar"),
			toNakedCharMethod = type.GetMethod("ToNakedChar");

		// Caching TextValues by string contents, and for access by integer index
		private static ValueCache<String, TextValue> textValueCache
			= new ValueCache<String, TextValue>((index, s) => new TextValue(s));

		public static readonly TextValue
			EMPTY = MakeInterned(String.Empty),
			VOID = MakeInterned("<void>");

		private TextValue(String value) {
			this.value = value;
		}

		public static int GetIndex(String s) {
			return textValueCache.GetIndex(s);
		}

		public static TextValue MakeInterned(String s) {
			return textValueCache[textValueCache.GetIndex(s)];
		}

		public static TextValue Make(string s) {
			Debug.Assert(s != null);  // Else use the defensive FromString
			if (s == "")
				return TextValue.EMPTY;
			else
				return new TextValue(s);  // On purpose NOT interned!
		}

		// These five are called also from generated code:

		public static TextValue FromIndex(int index) {
			return textValueCache[index];
		}

		public static Value FromString(Object o) {
			if (o is String)
				return Make(o as String);
			else
				return ErrorValue.argTypeError;
		}

		public static String ToString(Value v) {
			TextValue tv = v as TextValue;
			return tv != null ? tv.value : null;
		}

		public static Value FromNakedChar(char c) {
			return Make(c.ToString());
		}

		public static char ToNakedChar(TextValue v) {
			return v.value.Length >= 1 ? v.value[0] : '\0';
		}

		public static Value FromChar(Object o) {
			if (o is char)
				return Make(((char)o).ToString());
			else
				return ErrorValue.argTypeError;
		}

		public static Object ToChar(Value v) {
			TextValue tv = v as TextValue;
			return tv != null && tv.value.Length >= 1 ? (Object)tv.value[0] : null; // causes boxing
		}

		public override bool Equals(Value v) {
			return v is TextValue && (v as TextValue).value.Equals(value);
		}

		public override int GetHashCode() {
			return value.GetHashCode();
		}

		public override Object ToObject() {
			return (Object)value;
		}

		public override String ToString() {
			return value;
		}
	}
}