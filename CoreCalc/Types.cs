// Funcalc, a spreadsheet core implementation 
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

using SC = System.Collections;

// Delegate types, exception classes, formula formatting options, 
// and specialized collection classes

namespace Corecalc {

  /// <summary>
  /// An IDepend is an object such as Cell, Expr, CGExpr, ComputeCell 
  /// that can tell what full cell addresses it depends on.
  /// </summary>
  public interface IDepend {
    void DependsOn(FullCellAddr here, Action<FullCellAddr> dependsOn);
  }

  /// <summary>
  /// Applier is the delegate type used to represent implementations of
  /// built-in functions and sheet-defined functions in the interpretive 
  /// implementation.
  /// </summary>
  /// <param name="sheet">The sheet containing the cell in which the function is called.</param>
  /// <param name="es">The function call's argument expressions.</param>
  /// <param name="col">The column containing the cell in which the function is called.</param>
  /// <param name="row">The row containing the cell in which the function is called.</param>
  /// <returns></returns>
  public delegate Value Applier(Sheet sheet, Expr[] es, int col, int row);
  
  /// <summary>
  /// A CyclicException signals that a cyclic dependency is discovered 
  /// during evaluation.
  /// </summary>
  public class CyclicException : Exception {
    public readonly FullCellAddr culprit;

    public CyclicException(String msg, FullCellAddr culprit) : base(msg) {
      this.culprit = culprit;
    }
  }

  /// <summary>
  /// An ImpossibleException signals a violation of internal consistency 
  /// assumptions in the spreadsheet implementation.
  /// </summary>
  class ImpossibleException : Exception {
    public ImpossibleException(String msg) : base(msg) { }
  }

  /// <summary>
  /// A NotImplementedException signals that something could have 
  /// been implemented but was not.
  /// </summary>
  class NotImplementedException : Exception {
    public NotImplementedException(String msg) : base(msg) { }
  }

  // ----------------------------------------------------------------
  // Formula formatting options

  public class Formats {
    public enum RefType { A1, C0R0, R1C1 }

	public RefType RefFmt { get; set; } = RefType.A1;

	public char RangeDelim { get; set; } = ':';

	public char ArgDelim { get; set; } = ',';

	public bool ShowFormulas { get; set; }
  }

  // ----------------------------------------------------------------
  // A hash bag, a replacement for C5.HashBag<T>

  public class HashBag<T> : IEnumerable<T> {
    // Invariant: foreach (k,v) in multiplicity, v>0
    private readonly IDictionary<T, int> multiplicity = new Dictionary<T, int>();

    public bool Add(T item) {
      int count;
      if (multiplicity.TryGetValue(item, out count))
        multiplicity[item] = count + 1;
      else 
        multiplicity[item] = 1;
      return true;
    }

    public bool Remove(T item) {
      int count;
      if (multiplicity.TryGetValue(item, out count)) {
        count--;
        if (count == 0)
          multiplicity.Remove(item);
        else
          multiplicity[item] = count;
        return true;
      } else
        return false;
    }
    
    public void AddAll(IEnumerable<T> xs) {
      foreach (T x in xs)
        Add(x);
    }

    public void RemoveAll(IEnumerable<T> xs) {
      foreach (T x in xs)
        Remove(x);
    }

    public IEnumerable<KeyValuePair<T,int>> ItemMultiplicities() {
	    return multiplicity;
    }

	  public void Clear() {
      multiplicity.Clear();
    }

    public IEnumerator<T> GetEnumerator() {
      foreach (KeyValuePair<T, int> entry in multiplicity) 
        for (int i=0; i<entry.Value; i++)
          yield return entry.Key;
    }

    SC.IEnumerator SC.IEnumerable.GetEnumerator() {
      return GetEnumerator();
    }
  }

  // ----------------------------------------------------------------
  // An data structure that preserves insertion order of unique elements, 
  // and fast Contains, Add, AddAll, Intersection, Difference, and UnsequencedEquals 

  public class HashList<T> : IEnumerable<T> where T : IEquatable<T> {
    // Invariants: No duplicates in seq; seq and set have the same 
    // sets of items and the same number of items.
    private readonly List<T> seq = new List<T>();
    private readonly HashSet<T> set = new HashSet<T>();

    public bool Contains(T item) {
      return set.Contains(item);
    }

    public int Count { get { return seq.Count; }}

    public bool Add(T item) {
      if (set.Contains(item)) 
        return false;
      else {
        seq.Add(item);
        set.Add(item);
        return true;
      }
    }

    public void AddAll(IEnumerable<T> xs) {
      foreach (T x in xs)
        Add(x);
    }

    public static HashList<T> Union(HashList<T> ha1, HashList<T> ha2) {
      HashList<T> result = new HashList<T>();
      result.AddAll(ha1);
      result.AddAll(ha2);
      return result;
    }

    public static HashList<T> Intersection(HashList<T> ha1, HashList<T> ha2) {
      HashList<T> result = new HashList<T>();
      foreach (T x in ha1)
        if (ha2.Contains(x))
          result.Add(x);
      return result;
    }

    public static HashList<T> Difference(HashList<T> ha1, HashList<T> ha2) {
      HashList<T> result = new HashList<T>();
      foreach (T x in ha1)
        if (!ha2.Contains(x))
          result.Add(x);
      return result;
    }

    public bool UnsequencedEquals(HashList<T> that) {
      if (Count != that.Count)
        return false;
	  return seq.All(x => that.set.Contains(x));
    }

    public T[] ToArray() {
      return seq.ToArray();
    }

    public IEnumerator<T> GetEnumerator() {
      return seq.GetEnumerator();
    }

    SC.IEnumerator SC.IEnumerable.GetEnumerator() {
      return GetEnumerator();
    }
  }

  /// <summary>
  /// Machinery to cache the creation of objects of type U, when created 
  /// from objects of type T, and for later access via an integer index.
  /// </summary>
  /// <typeparam name="T">The type of key, typically String</typeparam>
  /// <typeparam name="U">The type of resulting cached item</typeparam>

  sealed class ValueCache<T, U> where T : IEquatable<T> {
    // Assumes: function make is monogenic
    // Invariant: array[dict[x]].Equals(make(x))
    private readonly IDictionary<T, int> dict = new Dictionary<T, int>();
    private readonly IList<U> array = new List<U>();
    private readonly Func<int, T, U> make;

    public ValueCache(Func<int, T, U> make) {
      this.make = make;
    }

    public int GetIndex(T x) {
      int index;
      if (!dict.TryGetValue(x, out index)) {
        index = array.Count;
        dict.Add(x, index);
        array.Add(make(index, x));
      }
      return index;
    }

    public U this[int index] {
      get { return array[index]; }
    }
  }

  /// <summary>
  /// Machinery to store objects of type T for later access via an integer index.
  /// </summary>
  /// <typeparam name="T">The type of item stored in the array</typeparam>

  sealed class ValueTable<T> where T : IEquatable<T> {
    private readonly IDictionary<T, int> dict = new Dictionary<T, int>();
    private readonly IList<T> array = new List<T>();

    public int GetIndex(T x) {
      int index;
      if (!dict.TryGetValue(x, out index)) {
        index = array.Count;
        array.Add(x);
        dict.Add(x, index);
      }
      return index;
    }

    public T this[int index] {
      get { return array[index]; }
    }
  }
}
