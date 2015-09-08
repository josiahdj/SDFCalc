using System.Linq;

using CoreCalc.Types;

namespace Corecalc.Funcalc {
	/// <summary>
	/// A Conj is conjunction of ordered path conditions.
	/// </summary>
	public class Conj : PathCond {
		internal readonly HashList<PathCond> conds;

		public Conj(params PathCond[] conds) {
			this.conds = new HashList<PathCond>();
			this.conds.AddAll(conds);
		}

		public static PathCond Make(params PathCond[] conjs) {
			HashList<PathCond> result = new HashList<PathCond>();
			foreach (PathCond conj in conjs) {
				if (conj.Is(false)) {
					return FALSE;
				}
				else if (!conj.Is(true)) {
					result.Add(conj);
				}
			}
			if (result.Count == 0) {
				return TRUE;
			}
			else if (result.Count == 1) {
				return result.Single();
			}
			else {
				return new Conj(result.ToArray());
			}
		}

		public override PathCond And(CachedAtom cond) {
			if (conds.Contains(cond.Negate())) {
				return FALSE;
			}
			else {
				return Make(AddItem(this.conds, cond));
			}
		}

		public override PathCond AndNot(CachedAtom cond) {
			if (conds.Contains(cond)) {
				return FALSE;
			}
			else {
				return Make(AddItem(this.conds, cond.Negate()));
			}
		}

		public override PathCond Or(PathCond other) {
			if (this.Is(true) || other.Is(true)) {
				return TRUE;
			}
			else if (this.Is(false)) {
				return other;
			}
			else if (other.Is(false)) {
				return this;
			}
			else if (conds.Contains(other)) {
				// Reduce ((p1 & ... & pn)) | pi to pi
				return other;
			}
			else if (other is Disj) {
				// TODO: This doesn't preserve order of disjuncts:
				return other.Or(this);
			}
			else if (other is Conj) {
				if ((other as Conj).conds.Contains(this)) {
					// Reduce (pi | (p1 & ... & pn)) to pi
					return this;
				}
				else {
					HashList<PathCond> intersect = HashList<PathCond>.Intersection(this.conds, (other as Conj).conds);
					if (intersect.Count > 0) {
						// Reduce (p1 & ... & pn & q1 & ... & qm) | (p1 & ... & pn & r1 & ... & rk) 
						// to (p1 & ... & pn & (q1 & ... & qm | r1 & ... & rk).
						// The pi go in intersect, qi in thisRest, and ri in otherRest.
						HashList<PathCond> thisRest = HashList<PathCond>.Difference(this.conds, intersect);
						HashList<PathCond> otherRest = HashList<PathCond>.Difference((other as Conj).conds, intersect);
						// This recursion terminates because thisRest is smaller than this.conds
						intersect.Add(Conj.Make(thisRest.ToArray()).Or(Conj.Make(otherRest.ToArray())));
						return Conj.Make(intersect.ToArray());
					}
					else {
						return Disj.Make(AddItem(this.conds, other));
					}
				}
			}
			else {
				return Disj.Make(this, other);
			}
		}

		public override bool Is(bool b) { // Conj() is TRUE, and Conj(b) is b
			return b && conds.Count == 0 || conds.Count == 1 && conds.Single().Is(b);
		}

		public override CGExpr ToCGExpr() {
			if (conds.Count == 1) {
				return conds.Single().ToCGExpr();
			}
			else {
				return new CGAnd(conds.Select(cond => cond.ToCGExpr()).ToArray());
			}
		}

		public override bool Equals(PathCond other) { return other is Conj && conds.UnsequencedEquals((other as Conj).conds); }

		public override int GetHashCode() {
			int result = 0;
			foreach (PathCond cond in conds) {
				result = 37*result + cond.GetHashCode();
			}
			return result;
		}

		public override string ToString() {
			if (conds.Count == 0) {
				return "TRUE";
			}
			else {
				return FormatInfix(" && ", conds);
			}
		}
	}
}