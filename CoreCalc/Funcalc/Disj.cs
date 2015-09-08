using System.Linq;

using CoreCalc.Types;

namespace Corecalc.Funcalc {
	/// <summary>
	/// A Disj is a disjunction of ordered path conditions.
	/// </summary>
	public class Disj : PathCond {
		private readonly HashList<PathCond> conds;

		public Disj(params PathCond[] conds) {
			this.conds = new HashList<PathCond>();
			this.conds.AddAll(conds);
		}

		public static PathCond Make(params PathCond[] disjs) {
			HashList<PathCond> result = new HashList<PathCond>();
			foreach (PathCond disj in disjs) {
				if (disj.Is(true)) {
					return TRUE;
				}
				else if (!disj.Is(false)) {
					result.Add(disj);
				}
			}
			if (result.Count == 0) {
				return FALSE;
			}
			else if (result.Count == 1) {
				return result.Single();
			}
			else {
				return new Disj(result.ToArray());
			}
		}

		public override PathCond And(CachedAtom cond) {
			return Conj.Make(this, cond);
			// Alternatively, weed out disjuncts inconsistent with the condition, 
			// using an order-preserving version of this code:
			// HashSet<PathCond> result = new HashSet<PathCond>();
			// result.AddAll(conds);
			// result.Filter(disj => !(disj is NegAtom && (disj as NegAtom).cond.Equals(cond) 
			// || disj is Conj && (disj as Conj).conds.Contains(new NegAtom(cond))));
			// return Conj.Make(Make(result.ToArray(), new Atom(cond));
		}

		public override PathCond AndNot(CachedAtom cond) { return Conj.Make(this, cond.Negate()); }

		public override PathCond Or(PathCond other) {
			if (other is CachedAtom && conds.Contains(((CachedAtom)other).Negate())) {
				// Reduce Or(OR(...,e,...), NOT(e)) and Or(OR(...,NOT(e),...), e) to TRUE
				return TRUE;
			}
			else if (other is Disj) {
				HashList<PathCond> result = new HashList<PathCond>();
				result.AddAll(conds);
				foreach (PathCond cond in ((Disj)other).conds) {
					if (cond is CachedAtom && conds.Contains((cond as CachedAtom).Negate())) {
						// Reduce Or(OR(...,e,...),OR(...,NOT(e),...)) to TRUE
						// and    Or(OR(...,NOT(e),...),OR(...,e,...)) to TRUE
						return TRUE;
					}
					result.Add(cond);
				}
				return Disj.Make(result.ToArray());
			}
			else if (other is Conj) {
				if (((Conj)other).conds.Contains(this)) {
					// Reduce (pi | (p1 & ... & pn)) to pi
					return this;
				}
				else {
					if (((Conj)other).conds.Any(cond => conds.Contains(cond))) {
						return this;
					}
				}
				return Disj.Make(AddItem(this.conds, other));
			}
			else {
				return Disj.Make(AddItem(this.conds, other));
			}
		}

		public override bool Is(bool b) { // Disj() is FALSE, and Disj(b) is b
			return !b && conds.Count == 0 || conds.Count == 1 && conds.Single().Is(b);
		}

		public override CGExpr ToCGExpr() {
			if (conds.Count == 1) {
				return conds.Single().ToCGExpr();
			}
			else {
				return new CGOr(conds.Select(cond => cond.ToCGExpr()).ToArray());
			}
		}

		public override bool Equals(PathCond other) { return other is Disj && conds.UnsequencedEquals(((Disj)other).conds); }

		public override int GetHashCode() {
			return conds.Aggregate(0, (current, cond) => 37*current + cond.GetHashCode());
		}

		public override string ToString() {
			if (conds.Count == 0) {
				return "FALSE";
			}
			else {
				return FormatInfix(" || ", conds);
			}
		}
	}
}