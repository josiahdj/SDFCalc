using System;

namespace Corecalc {
	/// <summary>
	/// An IDepend is an object such as Cell, Expr, CGExpr, ComputeCell 
	/// that can tell what full cell addresses it depends on.
	/// </summary>
	public interface IDepend {
		void DependsOn(FullCellAddr here, Action<FullCellAddr> dependsOn);
	}
}