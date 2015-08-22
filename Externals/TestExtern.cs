using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Threading;
using System.Globalization;
using System.Net;
using Microsoft.Office.Interop.Excel;

namespace Externals {
  public class TestExtern {
    public static double Sum(double[] xs) {
      return xs.Sum();
    }
  
    public static double[] Fibs(double d) {
      int n = Math.Max(2, (int)d);
      double[] res = new double[n];
      double a = res[0] = 1, b = res[1] = 1;
      for (int i=2; i<n; i++) {
        res[i] = a + b;
        a = b;
        b = res[i];
      }
      return res;
    }

    public static double[,] MatMult(double[,] A, double[,] B) {
      int 
        aRows = A.GetLength(0), 
        aCols = A.GetLength(1),
        bRows = B.GetLength(0), 
        bCols = B.GetLength(1),
        rRows = aRows, 
        rCols = bCols;
      if (aCols == bRows) {
        double[,] R = new double[rRows, rCols];
        for (int r = 0; r < rRows; r++) {
          for (int c = 0; c < rCols; c++) {
            double sum = 0.0;
            for (int k = 0; k < aCols; k++)
              sum += A[r, k] * B[k, c];
            R[r, c] = sum;
          }
        }
        return R;
      } else
        return null;
    }

    public static String HttpGet(String address) {
      WebClient wc = new WebClient();
      byte[] bytes = wc.DownloadData(address);
      return Encoding.UTF8.GetString(bytes);
    }
  }
}
