using System.Collections.Generic;

namespace Corecalc.Funcalc {
	/// <summary>
	/// A CachedAtom represents a (possibly negated) expression from a source spreadsheet 
	/// formula.  That expression is cached, in the sense that it will be 
	/// evaluated at most once and its value stored in a local variable.  
	/// For this to work, all copies and negations of a CachedAtom must have 
	/// the same enclosed cachedExpr.
	/// </summary>
	public class CachedAtom : PathCond {
		public readonly CGCachedExpr cachedExpr;
		public readonly bool negated; // True if represents NOT(cachedExpr)

		public CachedAtom(CGExpr cond, List<CGCachedExpr> caches) {
			this.cachedExpr = new CGCachedExpr(cond, this, caches);
			this.negated = false;
		}

		private CachedAtom(CachedAtom atom, bool negated) {
			this.cachedExpr = atom.cachedExpr;
			this.negated = negated;
		}

		public CachedAtom Negate() { return new CachedAtom(this, !negated); }

		public override PathCond And(CachedAtom cond) {
			if (this.EqualsNega(cond)) {
				return FALSE;
			}
			else {
				return Conj.Make(this, cond);
			}
		}

		public override PathCond AndNot(CachedAtom cond) {
			if (this.Equals(cond)) {
				return FALSE;
			}
			else {
				return Conj.Make(this, cond.Negate());
			}
		}

		public override PathCond Or(PathCond other) {
			if (this.EqualsNega(other)) {
				return TRUE;
			}
			else if (other is Conj || other is Disj) {
				// TODO: This doesn't preserve order of disjuncts:
				return other.Or(this);
			}
			else {
				return Disj.Make(this, other);
			}
		}

		public override bool Is(bool b) { return false; }

		public override CGExpr ToCGExpr() {
			cachedExpr.IncrementGenerateCount();
			if (negated) {
				return new CGNot(new CGExpr[] {cachedExpr});
			}
			else {
				return cachedExpr;
			}
		}

		public override bool Equals(PathCond other) {
			CachedAtom atom = other as CachedAtom;
			return atom != null && cachedExpr.Equals(atom.cachedExpr) && negated == atom.negated;
		}

		public override int GetHashCode() { return cachedExpr.GetHashCode()*2 + (negated ? 1 : 0); }

		public bool EqualsNega(PathCond other) {
			CachedAtom atom = other as CachedAtom;
			return atom != null && cachedExpr.Equals(atom.cachedExpr) && negated != atom.negated;
		}

		public override string ToString() {
			if (negated) {
				return "NOT(" + cachedExpr.ToString() + ")";
			}
			else {
				return cachedExpr.ToString();
			}
		}
	}
}