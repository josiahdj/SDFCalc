// Funcalc, spreadsheet with functions
// ----------------------------------------------------------------------
// Copyright (c) 2006-2014 Peter Sestoft and others

// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use, copy,
// modify, merge, publish, distribute, sublicense, and/or sell copies
// of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

//  * The above copyright notice and this permission notice shall be
//    included in all copies or substantial portions of the Software.

//  * The software is provided "as is", without warranty of any kind,
//    express or implied, including but not limited to the warranties of
//    merchantability, fitness for a particular purpose and
//    noninfringement.  In no event shall the authors or copyright
//    holders be liable for any claim, damages or other liability,
//    whether in an action of contract, tort or otherwise, arising from,
//    out of or in connection with the software or the use or other
//    dealings in the software.
// ----------------------------------------------------------------------

using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace Corecalc.Funcalc {
  /// <summary>
  /// A PathCond represents an evaluation condition.
  /// </summary>
  public abstract class PathCond : IEquatable<PathCond> {
    public static readonly PathCond FALSE = new Disj();
    public static readonly PathCond TRUE = new Conj();

    public abstract PathCond And(CachedAtom expr);
    public abstract PathCond AndNot(CachedAtom expr);
    public abstract PathCond Or(PathCond other);

    public abstract bool Is(bool b);
    public abstract CGExpr ToCGExpr();
    public abstract bool Equals(PathCond other);

    protected static PathCond[] AddItem(IEnumerable<PathCond> set, PathCond item) {
      HashList<PathCond> result = new HashList<PathCond>();
      result.AddAll(set);
      result.Add(item);
      return result.ToArray();
    }

    protected static String FormatInfix(String op, IEnumerable<PathCond> conds) {
      bool first = true;
      StringBuilder sb = new StringBuilder();
      sb.Append("(");
      foreach (PathCond p in conds) {
        if (!first)
          sb.Append(op);
        first = false;
        sb.Append(p);
      }
      return sb.Append(")").ToString();
    }
  }

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

    public CachedAtom Negate() {
      return new CachedAtom(this, !negated);
    }

    public override PathCond And(CachedAtom cond) {
      if (this.EqualsNega(cond))
        return FALSE;
      else
        return Conj.Make(this, cond);
    }

    public override PathCond AndNot(CachedAtom cond) {
      if (this.Equals(cond))
        return FALSE;
      else
        return Conj.Make(this, cond.Negate());
    }

    public override PathCond Or(PathCond other) {
      if (this.EqualsNega(other))
        return TRUE;
      else if (other is Conj || other is Disj)
        // TODO: This doesn't preserve order of disjuncts:
        return other.Or(this);
      else
        return Disj.Make(this, other);
    }

    public override bool Is(bool b) {
      return false;
    }

    public override CGExpr ToCGExpr() {
      cachedExpr.IncrementGenerateCount();
      if (negated)
        return new CGNot(new CGExpr[] { cachedExpr });
      else
        return cachedExpr;
    }

    public override bool Equals(PathCond other) {
      CachedAtom atom = other as CachedAtom;
      return atom != null && cachedExpr.Equals(atom.cachedExpr) && negated == atom.negated;
    }

    public override int GetHashCode() {
      return cachedExpr.GetHashCode() * 2 + (negated ? 1 : 0);
    }

    public bool EqualsNega(PathCond other) {
      CachedAtom atom = other as CachedAtom;
      return atom != null && cachedExpr.Equals(atom.cachedExpr) && negated != atom.negated;
    }

    public override string ToString() {
      if (negated)
        return "NOT(" + cachedExpr.ToString() + ")";
      else
        return cachedExpr.ToString();
    }
  }

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
      foreach (PathCond conj in conjs)
        if (conj.Is(false))
          return FALSE;
        else if (!conj.Is(true))
          result.Add(conj);
      if (result.Count == 0)
        return TRUE;
      else if (result.Count == 1)
        return result.Single();
      else
        return new Conj(result.ToArray());
    }

    public override PathCond And(CachedAtom cond) {
      if (conds.Contains(cond.Negate()))
        return FALSE;
      else 
        return Make(AddItem(this.conds, cond));
    }

    public override PathCond AndNot(CachedAtom cond) {
      if (conds.Contains(cond))
        return FALSE;
      else 
        return Make(AddItem(this.conds, cond.Negate()));
    }

    public override PathCond Or(PathCond other) {
      if (this.Is(true) || other.Is(true))
        return TRUE;
      else if (this.Is(false))
        return other;
      else if (other.Is(false))
        return this;
      else if (conds.Contains(other))
        // Reduce ((p1 & ... & pn)) | pi to pi
        return other;
      else if (other is Disj)
        // TODO: This doesn't preserve order of disjuncts:
        return other.Or(this);
      else if (other is Conj) {
        if ((other as Conj).conds.Contains(this))
          // Reduce (pi | (p1 & ... & pn)) to pi
          return this;
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
          } else
            return Disj.Make(AddItem(this.conds, other));
        }
      } else
        return Disj.Make(this, other);
    }

    public override bool Is(bool b) { // Conj() is TRUE, and Conj(b) is b
      return b && conds.Count == 0 || conds.Count == 1 && conds.Single().Is(b);
    }

    public override CGExpr ToCGExpr() {
      if (conds.Count == 1)
        return conds.Single().ToCGExpr();
      else 
        return new CGAnd(conds.Select(cond => cond.ToCGExpr()).ToArray());
    }

    public override bool Equals(PathCond other) {
      return other is Conj && conds.UnsequencedEquals((other as Conj).conds);
    }

    public override int GetHashCode() {
      int result = 0;
      foreach (PathCond cond in conds)
        result = 37 * result + cond.GetHashCode();
      return result;
    }

    public override string ToString() {
      if (conds.Count == 0)
        return "TRUE";
      else
        return FormatInfix(" && ", conds);
    }
  }

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
      foreach (PathCond disj in disjs)
        if (disj.Is(true))
          return TRUE;
        else if (!disj.Is(false))
          result.Add(disj);
      if (result.Count == 0)
        return FALSE;
      else if (result.Count == 1)
        return result.Single();
      else
        return new Disj(result.ToArray());
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

    public override PathCond AndNot(CachedAtom cond) {
      return Conj.Make(this, cond.Negate());
    }

    public override PathCond Or(PathCond other) {
      if (other is CachedAtom && conds.Contains((other as CachedAtom).Negate()))
        // Reduce Or(OR(...,e,...), NOT(e)) and Or(OR(...,NOT(e),...), e) to TRUE
        return TRUE;
      else if (other is Disj) {
        HashList<PathCond> result = new HashList<PathCond>();
        result.AddAll(conds);
        foreach (PathCond cond in (other as Disj).conds) {
          if (cond is CachedAtom && conds.Contains((cond as CachedAtom).Negate()))
            // Reduce Or(OR(...,e,...),OR(...,NOT(e),...)) to TRUE
            // and    Or(OR(...,NOT(e),...),OR(...,e,...)) to TRUE
            return TRUE;
          result.Add(cond);
        }
        return Disj.Make(result.ToArray());
      } else if (other is Conj) {
        if ((other as Conj).conds.Contains(this))
          // Reduce (pi | (p1 & ... & pn)) to pi
          return this;
        else 
          foreach (PathCond cond in (other as Conj).conds)
            if (conds.Contains(cond))
              // Reduce (p1 | ... | pn) | (... & pi & ...) to (p1 | ... | pn)
              return this;
        return Disj.Make(AddItem(this.conds, other));
      } else
        return Disj.Make(AddItem(this.conds, other));
    }

    public override bool Is(bool b) { // Disj() is FALSE, and Disj(b) is b
      return !b && conds.Count == 0 || conds.Count == 1 && conds.Single().Is(b);
    }

    public override CGExpr ToCGExpr() {
      if (conds.Count == 1)
        return conds.Single().ToCGExpr();
      else 
        return new CGOr(conds.Select(cond => cond.ToCGExpr()).ToArray());
    }

    public override bool Equals(PathCond other) {
      return other is Disj && conds.UnsequencedEquals((other as Disj).conds);
    }

    public override int GetHashCode() {
      int result = 0;
      foreach (PathCond cond in conds)
        result = 37 * result + cond.GetHashCode();
      return result;
    }

    public override string ToString() {
      if (conds.Count == 0)
        return "FALSE";
      else
        return FormatInfix(" || ", conds);
    }
  }
}
