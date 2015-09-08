using System.Collections.Generic;

namespace Corecalc.Funcalc {
	/// <summary>
	/// A PEnv is an environment for partial evaluation.
	/// </summary>
	public class PEnv : Dictionary<FullCellAddr, CGExpr> {}
}