using System;
using System.Reflection.Emit;

namespace Corecalc.Funcalc {
	/// <summary>
	/// A CGComparison is a comparison operation that takes two operands, 
	/// both numeric for now, and produces
	/// the outcome true (1.0) or false (0.0), or in case either operand 
	/// evaluates to NaN or +/-infinity, it produces that NaN or a plain NaN.
	/// </summary>
	public abstract class CGComparison : CGStrictOperation {
		public CGComparison(CGExpr[] es, Applier applier) : base(es, applier) { }

		// These are used in a template method pattern
		// Emit instruction to compare doubles, leaving 1 on stack if true:
		protected abstract void GenCompareDouble();

		// Emit instructions to compare doubles, jump to target if false:
		protected abstract void GenDoubleFalseJump(Label target);

		protected abstract String Name { get; }

		public override void Compile() {
			CompileToDoubleProper(new Gen(delegate { WrapDoubleToNumberValue(); }),
								  GenLoadTestDoubleErrorValue());
		}

		// A comparison always evaluates to a double; if it cannot evaluate to
		// a proper double, it evaluates to an infinity or NaN.
		// This code seems a bit redundant: It leaves the double on the stack top,
		// whether or not it is proper?  On the other hand, it can be improved only
		// by basically duplicating the CompileToDoubleProper method's body.  
		// Doing so might avoid a jump to a jump or similar, though.
		public override void CompileToDoubleOrNan() {
			CompileToDoubleProper(
								  new Gen(delegate { }),
								  new Gen(delegate { ilg.Emit(OpCodes.Ldloc, testDouble); }));
		}

		// A comparison evaluates to a proper double only if both operands do
		public override void CompileToDoubleProper(Gen ifProper, Gen ifOther) {
			es[0].CompileToDoubleProper(
									    new Gen(delegate {
													es[1].CompileToDoubleProper(
																			    new Gen(delegate {
																							GenCompareDouble();
																							ilg.Emit(OpCodes.Conv_R8);
																							ifProper.Generate(ilg);
																						}),
																				new Gen(delegate {
																							ilg.Emit(OpCodes.Pop);
																							ifOther.Generate(ilg);
																						}));
												}),
										ifOther);
		}

		// This override combines the ordering predicate and the conditional jump
		public override void CompileCondition(Gen ifTrue, Gen ifFalse, Gen ifOther) {
			es[0].CompileToDoubleProper(
									    new Gen(delegate {
													es[1].CompileToDoubleProper(
																			    new Gen(delegate {
																							GenDoubleFalseJump(ifFalse.GetLabel(ilg));
																							ifTrue.Generate(ilg);
																							if (!ifFalse.Generated) {
																								Label endLabel = ilg.DefineLabel();
																								ilg.Emit(OpCodes.Br, endLabel);
																								ifFalse.Generate(ilg);
																								ilg.MarkLabel(endLabel);
																							}
																						}),
																				new Gen(delegate {
																							ilg.Emit(OpCodes.Pop);
																							ifOther.Generate(ilg);
																						}));
												}),
										ifOther);
		}

		public override string ToString() { return es[0] + Name + es[1]; }

		public override Typ Type() { return Typ.Number; }

		protected override Typ GetInputTypWithoutLengthCheck(int pos) { return Typ.Number; }

		public override int Arity {
			get { return 2; }
		}
	}
}