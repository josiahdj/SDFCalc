// Funcalc, spreadsheet with functions
// ----------------------------------------------------------------------
// Copyright (c) 2006-2012 Thomas S. Iversen, Peter Sestoft and others

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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using CoreCalc.GUI;

namespace Corecalc.Benchmarks {
	public class Benchmarks {
		private TextWriter tw;
		public bool useLog;

		public Benchmarks(bool useLog) { this.useLog = useLog; }

		private TextWriter Tw {
			get {
				if (tw == null) {
					tw = new StreamWriter("benchmark_results.txt");
				}
				return tw;
			}
		}

		public void BenchmarkRecalculation(WorkbookForm wf, int runs) {
			BenchmarkWorkbook(wf,
							  runs,
							  "Workbook standard recalculation",
							  wf.Workbook.Recalculate);
		}

		public void BenchmarkRecalculationFull(WorkbookForm wf, int runs) {
			BenchmarkWorkbook(wf,
							  runs,
							  "Workbook full recalculation",
							  wf.Workbook.RecalculateFull);
		}

		public void BenchmarkRecalculationFullRebuild(WorkbookForm wf, int runs) {
			BenchmarkWorkbook(wf,
							  runs,
							  "Workbook full recalculation rebuild",
							  wf.Workbook.RecalculateFullRebuild);
		}

		private void BenchmarkWorkbook(WorkbookForm wf, int runs, string benchmarkName, Func<long> benchmark) {
			Log("=== Benchmark workbook called: ");

			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Reset();
			stopwatch.Start();
			for (int i = 0; i < runs; i++) {
				benchmark();
			}
			stopwatch.Stop();
			double average = stopwatch.ElapsedMilliseconds/(double)runs;
			Log(String.Format("[{0}] Average of the {1} runs: {2:N2} ms",
							  benchmarkName,
							  runs,
							  average));
			wf.SetStatusLine((long)(average + 0.5));
		}

		//log both a flat file and console
		private void Log(string s) {
			if (useLog) {
				Console.WriteLine(s);
				Tw.WriteLine(s);
				Tw.Flush();
			}
		}
	}
}