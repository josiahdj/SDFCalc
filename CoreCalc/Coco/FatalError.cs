using System;

namespace CoreCalc.Coco {
	public class FatalError : Exception {
		public FatalError(string m) : base(m) { }
	}
}