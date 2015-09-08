namespace Corecalc.Funcalc {
	/// <summary>
	/// A CGArithmetic1 is an application of a one-argument numeric-valued 
	/// built-in function.
	/// </summary>
	public abstract class CGArithmetic1 : CGStrictOperation {
		public CGArithmetic1(CGExpr[] es, Applier applier) : base(es, applier) { }

		public override void Compile() {
			CompileToDoubleOrNan();
			WrapDoubleToNumberValue();
		}

		public override Typ Type() { return Typ.Number; }

		protected override Typ GetInputTypWithoutLengthCheck(int pos) { return Typ.Number; }

		public override int Arity {
			get { return 1; }
		}
	}
}