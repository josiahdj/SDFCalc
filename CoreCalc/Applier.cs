using CoreCalc.Expressions;
using CoreCalc.Values;

namespace Corecalc {
	/// <summary>
	/// Applier is the delegate type used to represent implementations of
	/// built-in functions and sheet-defined functions in the interpretive 
	/// implementation.
	/// </summary>
	/// <param name="sheet">The sheet containing the cell in which the function is called.</param>
	/// <param name="es">The function call's argument expressions.</param>
	/// <param name="col">The column containing the cell in which the function is called.</param>
	/// <param name="row">The row containing the cell in which the function is called.</param>
	/// <returns></returns>
	public delegate Value Applier(Sheet sheet, Expr[] es, int col, int row);
}