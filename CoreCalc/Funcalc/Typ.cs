namespace Corecalc.Funcalc {
	/// <summary>
	/// A Typ is the type of a spreadsheet value, used for return type 
	/// and argument types of built-in functions.
	/// </summary>
	public enum Typ {
		Error,
		Number,
		Text,
		Array,
		Function,
		Value
	};
}