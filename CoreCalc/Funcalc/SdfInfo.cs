using System;
using System.Collections.Generic;
using System.Reflection;

namespace Corecalc.Funcalc {
	/// <summary>
	/// An SdfInfo instance represents a compiled sheet-defined function (SDF).
	/// </summary>
	public class SdfInfo {
		public readonly FullCellAddr outputCell;
		public readonly FullCellAddr[] inputCells;
		public readonly string name; // Always upper case
		public readonly int index; // Index into SdfManager.sdfDelegates
		public readonly int arity;
		public ProgramLines Program { get; set; }
		public bool IsVolatile { get; private set; }

		private static readonly Type[] sdfDelegateType
			= {
				  typeof (Func<Value>),
				  typeof (Func<Value, Value>),
				  typeof (Func<Value, Value, Value>),
				  typeof (Func<Value, Value, Value, Value>),
				  typeof (Func<Value, Value, Value, Value, Value>),
				  typeof (Func<Value, Value, Value, Value, Value, Value>),
				  typeof (Func<Value, Value, Value, Value, Value, Value, Value>),
				  typeof (Func<Value, Value, Value, Value, Value, Value, Value, Value>),
				  typeof (Func<Value, Value, Value, Value, Value, Value, Value, Value, Value>),
				  typeof (Func<Value, Value, Value, Value, Value, Value, Value, Value, Value, Value>),
				  typeof (Func<Value, Value, Value, Value, Value, Value, Value, Value, Value, Value, Value>)
			  };

		private static readonly MethodInfo[] sdfDelegateInvokeMethods;
		private static readonly Type[][] argumentTypes;

		public static readonly FieldInfo indexField = typeof (SdfInfo).GetField("index");

		static SdfInfo() {
			sdfDelegateInvokeMethods = new MethodInfo[sdfDelegateType.Length];
			for (int i = 0; i < sdfDelegateType.Length; i++) {
				sdfDelegateInvokeMethods[i] = sdfDelegateType[i].GetMethod("Invoke");
			}
			argumentTypes = new Type[sdfDelegateType.Length][];
			for (int i = 0; i < argumentTypes.Length; i++) {
				argumentTypes[i] = new Type[i];
				for (int j = 0; j < i; j++) {
					argumentTypes[i][j] = Value.type;
				}
			}
		}

		internal SdfInfo(FullCellAddr outputCell,
						 FullCellAddr[] inputCells,
						 string name,
						 int index) {
			this.outputCell = outputCell;
			this.inputCells = inputCells;
			this.name = name.ToUpper();
			this.index = index;
			this.arity = inputCells.Length;
		}

		public Type[] MyArgumentTypes {
			get { return argumentTypes[arity]; }
		}

		public Type MyType {
			get { return sdfDelegateType[arity]; }
		}

		public static Type SdfDelegateType(int n) { return sdfDelegateType[n]; }

		public MethodInfo MyInvoke {
			get { return sdfDelegateInvokeMethods[arity]; }
		}

		/// <summary>
		/// Determine whether the sheet-defined function involves any volatile cells.  
		/// Does not track volatility of normal cells or normal cell areas referred to.
		/// </summary>
		/// <param name="fcas">The set of function-sheet cells making up the function.</param>
		public void SetVolatility(IEnumerable<FullCellAddr> fcas) {
			bool isVolatile = false;
			foreach (FullCellAddr fca in fcas) {
				Cell cell = fca.sheet[fca.ca];
				if (cell != null && cell.IsVolatile) {
					isVolatile = true;
					break;
				}
			}
			this.IsVolatile = isVolatile;
		}

		public Value Apply(Value[] aa) {
			switch (arity) {
				case 0:
					return Call0();
				case 1:
					return Call1(aa[0]);
				case 2:
					return Call2(aa[0], aa[1]);
				case 3:
					return Call3(aa[0], aa[1], aa[2]);
				case 4:
					return Call4(aa[0], aa[1], aa[2], aa[3]);
				case 5:
					return Call5(aa[0], aa[1], aa[2], aa[3], aa[4]);
				case 6:
					return Call6(aa[0], aa[1], aa[2], aa[3], aa[4], aa[5]);
				case 7:
					return Call7(aa[0], aa[1], aa[2], aa[3], aa[4], aa[5], aa[6]);
				case 8:
					return Call8(aa[0], aa[1], aa[2], aa[3], aa[4], aa[5], aa[6], aa[7]);
				case 9:
					return Call9(aa[0], aa[1], aa[2], aa[3], aa[4], aa[5], aa[6], aa[7], aa[8]);
				default:
					return ErrorValue.tooManyArgsError;
			}
		}

		// These don't seem to be used reflexively (for code generation)

		public Value Call0() {
			if (arity == 0) {
				return ((Func<Value>)(SdfManager.sdfDelegates[index]))();
			}
			else {
				return ErrorValue.argCountError;
			}
		}

		public Value Call1(Value v0) {
			if (arity == 1) {
				return ((Func<Value, Value>)(SdfManager.sdfDelegates[index]))(v0);
			}
			else {
				return ErrorValue.argCountError;
			}
		}

		public Value Call2(Value v0, Value v1) {
			if (arity == 2) {
				return ((Func<Value, Value, Value>)(SdfManager.sdfDelegates[index]))(v0, v1);
			}
			else {
				return ErrorValue.argCountError;
			}
		}

		public Value Call3(Value v0, Value v1, Value v2) {
			if (arity == 3) {
				return ((Func<Value, Value, Value, Value>)(SdfManager.sdfDelegates[index]))(v0, v1, v2);
			}
			else {
				return ErrorValue.argCountError;
			}
		}

		public Value Call4(Value v0, Value v1, Value v2, Value v3) {
			if (arity == 4) {
				return ((Func<Value, Value, Value, Value, Value>)(SdfManager.sdfDelegates[index]))(v0, v1, v2, v3);
			}
			else {
				return ErrorValue.argCountError;
			}
		}

		public Value Call5(Value v0, Value v1, Value v2, Value v3, Value v4) {
			if (arity == 5) {
				return ((Func<Value, Value, Value, Value, Value, Value>)(SdfManager.sdfDelegates[index]))(v0, v1, v2, v3, v4);
			}
			else {
				return ErrorValue.argCountError;
			}
		}

		public Value Call6(Value v0, Value v1, Value v2, Value v3, Value v4, Value v5) {
			if (arity == 6) {
				return ((Func<Value, Value, Value, Value, Value, Value, Value>)(SdfManager.sdfDelegates[index]))(v0, v1, v2, v3, v4, v5);
			}
			else {
				return ErrorValue.argCountError;
			}
		}

		public Value Call7(Value v0, Value v1, Value v2, Value v3, Value v4, Value v5, Value v6) {
			if (arity == 7) {
				return ((Func<Value, Value, Value, Value, Value, Value, Value, Value>)(SdfManager.sdfDelegates[index]))(v0, v1, v2, v3, v4, v5, v6);
			}
			else {
				return ErrorValue.argCountError;
			}
		}

		public Value Call8(Value v0, Value v1, Value v2, Value v3, Value v4, Value v5, Value v6, Value v7) {
			if (arity == 8) {
				return ((Func<Value, Value, Value, Value, Value, Value, Value, Value, Value>)(SdfManager.sdfDelegates[index]))(v0, v1, v2, v3, v4, v5, v6, v7);
			}
			else {
				return ErrorValue.argCountError;
			}
		}

		public Value Call9(Value v0, Value v1, Value v2, Value v3, Value v4, Value v5, Value v6, Value v7, Value v8) {
			if (arity == 9) {
				return ((Func<Value, Value, Value, Value, Value, Value, Value, Value, Value, Value>)(SdfManager.sdfDelegates[index]))(v0, v1, v2, v3, v4, v5, v6, v7, v8);
			}
			else {
				return ErrorValue.argCountError;
			}
		}

		public Value Apply(Sheet sheet, Expr[] es, int col, int row) {
			// Arity is checked in the SdfInfo.CallN methods
			switch (es.Length) {
				case 0:
					return Call0();
				case 1:
					return Call1(es[0].Eval(sheet, col, row));
				case 2:
					return Call2(es[0].Eval(sheet, col, row), es[1].Eval(sheet, col, row));
				case 3:
					return Call3(es[0].Eval(sheet, col, row),
								 es[1].Eval(sheet, col, row),
								 es[2].Eval(sheet, col, row));
				case 4:
					return Call4(es[0].Eval(sheet, col, row),
								 es[1].Eval(sheet, col, row),
								 es[2].Eval(sheet, col, row),
								 es[3].Eval(sheet, col, row));
				case 5:
					return Call5(es[0].Eval(sheet, col, row),
								 es[1].Eval(sheet, col, row),
								 es[2].Eval(sheet, col, row),
								 es[3].Eval(sheet, col, row),
								 es[4].Eval(sheet, col, row));
				case 6:
					return Call6(es[0].Eval(sheet, col, row),
								 es[1].Eval(sheet, col, row),
								 es[2].Eval(sheet, col, row),
								 es[3].Eval(sheet, col, row),
								 es[4].Eval(sheet, col, row),
								 es[5].Eval(sheet, col, row));
				case 7:
					return Call7(es[0].Eval(sheet, col, row),
								 es[1].Eval(sheet, col, row),
								 es[2].Eval(sheet, col, row),
								 es[3].Eval(sheet, col, row),
								 es[4].Eval(sheet, col, row),
								 es[5].Eval(sheet, col, row),
								 es[6].Eval(sheet, col, row));
				case 8:
					return Call8(es[0].Eval(sheet, col, row),
								 es[1].Eval(sheet, col, row),
								 es[2].Eval(sheet, col, row),
								 es[3].Eval(sheet, col, row),
								 es[4].Eval(sheet, col, row),
								 es[5].Eval(sheet, col, row),
								 es[6].Eval(sheet, col, row),
								 es[7].Eval(sheet, col, row));
				case 9:
					return Call9(es[0].Eval(sheet, col, row),
								 es[1].Eval(sheet, col, row),
								 es[2].Eval(sheet, col, row),
								 es[3].Eval(sheet, col, row),
								 es[4].Eval(sheet, col, row),
								 es[5].Eval(sheet, col, row),
								 es[6].Eval(sheet, col, row),
								 es[7].Eval(sheet, col, row),
								 es[8].Eval(sheet, col, row));
				default:
					return ErrorValue.Make("#FUNERR: Too many arguments");
			}
		}

		public override string ToString() { return String.Format("FUN {0} AT #{1}", name, index); }
	}
}