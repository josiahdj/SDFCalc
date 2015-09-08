using System;
using System.Reflection.Emit;

namespace Corecalc.Funcalc {
	/// <summary>
	/// A Gen object is a code generator that avoids generating the same code 
	/// multiple times, and also to some extent avoids generating jumps to jumps.  
	/// </summary>
	public class Gen {
		private readonly Action generate;
		private Label? label;
		private bool generated; // Invariant: generated implies label.HasValue

		public Gen(Action generate) {
			this.generate = generate;
			label = null;
			generated = false;
		}

		// A Generate object has a unique label
		public Label GetLabel(ILGenerator ilg) {
			if (!label.HasValue) {
				label = ilg.DefineLabel();
			}
			return label.Value;
		}

		public bool Generated {
			get { return generated; }
		}

		public void Generate(ILGenerator ilg) {
			if (generated) {
				ilg.Emit(OpCodes.Br, GetLabel(ilg));
			}
			else {
				ilg.MarkLabel(GetLabel(ilg));
				generated = true;
				generate();
			}
		}
	}
}