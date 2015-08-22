// Funcalc, a spreadsheet core implementation 
// ----------------------------------------------------------------------
// Copyright (c) 2006-2014 Thomas S. Iversen, Peter Sestoft

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
using System.Diagnostics;            // Stopwatch
using System.Globalization;          // CultureInfo
using System.IO;			               // MemoryStream, Stream
using System.Text;
using System.Xml;
using System.Windows.Forms;
using Corecalc;

namespace Corecalc.IO {
  /// <summary>
  /// An IOFormat determines how to read a given XML file containing a
  /// spreadsheet workbook.
  /// Currently, only Excel 2003 XMLSS format is supported.
  /// </summary>
  abstract class IOFormat {
    public abstract Workbook Read(String filename);
    private String fileExtension;
    private String description;

    public static Stream MakeStream(String s) {
      char[] cs = s.ToCharArray();
      byte[] bs = new byte[cs.Length];
      for (int i = 0; i < cs.Length; i++)
        bs[i] = (byte)(cs[i]);
      return new MemoryStream(bs);
    }

    public String GetFilter() {
      return description + " (*." + fileExtension + ")|*." + fileExtension;
    }

    public IOFormat(String fileextension, String description) {
      this.fileExtension = fileextension;
      this.description = description;
    }

    public Boolean ValidExtension(String ext) {
      return this.fileExtension == ext;
    }
  }

  /// <summary>
  /// Class XMLSSIOFormat can read XMLSS (Excel 2003 XML format) workbook files.
  /// </summary>
  sealed class XMLSSIOFormat : IOFormat {
    const int MINROWS = 10, MINCOLS = 10;  // Must be positive

    readonly Formats fo = new Formats();

    public XMLSSIOFormat()
      : base("xml", "XMLSS") {
      fo.RefFmt = Formats.RefType.R1C1;
    }

    private int ParseRow(XmlReader rowreader, Workbook wb, Sheet sheet, int row,
                         IDictionary<String, Cell> cellParsingCache) 
    {
      /* XMLSS has origo at (1,1) = (A,1) whereas Corecalc internal 
       * representation has origo at (0,0) = (A,1).  Hence the col 
       * and row indices are 1 higher in this method.
       */
      int cellCount = 0;
      int col = 0;
      XmlReader cellreader = rowreader.ReadSubtree();
      while (cellreader.ReadToFollowing("Cell")) {
        String colindexstr = cellreader.GetAttribute("ss:Index");
        String arrayrangestr = cellreader.GetAttribute("ss:ArrayRange");
        String formulastr = cellreader.GetAttribute("ss:Formula");
        String typestr = "";
        String dataval = "";

        if (colindexstr != null) {
          if (!int.TryParse(colindexstr, out col))
            col = 0; // Looks wrong, should be 1?
        } else 
          col++;

        cellCount++;
        // If an array result occupies cells, do not overwrite
        // the formula with precomputed and cached data from 
        // the XMLSS file. Instead skip the parsing and sheetupdate.
        if (sheet[col - 1, row - 1] != null)
          continue;

        using (XmlReader datareader = cellreader.ReadSubtree()) {
          if (datareader.ReadToFollowing("Data")) {
            typestr = datareader.GetAttribute("ss:Type");
            datareader.MoveToContent();
            dataval = datareader.ReadElementContentAsString();
          }
        }

        String cellString;
        if (formulastr != null) 
          cellString = formulastr;
        else {
          // Anything else than formulas are values.
          // If XMLSS tells us it is a String we believe it
          if (typestr == "String")
            dataval = "'" + dataval;
          cellString = dataval;
        }

        // Skip blank cells
        if (cellString == "")
          continue;

        Cell cell;
        if (cellParsingCache.TryGetValue(cellString, out cell)) {
          // Copy the cell (both for mutable Formula cells and for cell-specific 
          // metadata) and but share any sharable contents.
          cell = cell.CloneCell(col - 1, row - 1);
        } else {
          // Cell contents not seen before: scan, parse and cache
          cell = Cell.Parse(cellString, wb, col, row);
          if (cell == null)
            Console.WriteLine("BAD: Null cell from \"{0}\"", cellString);
          else
            cellParsingCache.Add(cellString, cell);
        }

        if (arrayrangestr != null && cell is Formula) { // Array formula
          string[] split = arrayrangestr.Split(":".ToCharArray());

          RARef raref1 = new RARef(split[0]);
          RARef raref2;
          if (split.Length == 1) {
            // FIXME: single cell result, but still array
            raref2 = new RARef(split[0]);
          } else {
            raref2 = new RARef(split[1]);
          }

          if (raref1 != null && raref2 != null) {
            CellAddr ulCa = raref1.Addr(col - 1, row - 1);
            CellAddr lrCa = raref2.Addr(col - 1, row - 1);
            // This also updates support sets, but that's useless, because 
            // they will subsequently be reset by RebuildSupportGraph
            sheet.SetArrayFormula(cell, col - 1, row - 1, ulCa, lrCa);
          }
        } else {   // One-cell formula, or constant
          sheet[col - 1, row - 1] = cell;
        }
      }
      cellreader.Close();
      return cellCount;
    }

    private int ParseSheet(XmlReader sheetreader, Workbook wb, Sheet sheet,
                           IDictionary<String, Cell> cellParsingCache) 
    {
      int cellCount = 0;
      if (sheetreader.ReadToFollowing("Table")) {
        int row = 0;
        using (XmlReader rowreader = sheetreader.ReadSubtree()) {
          while (rowreader.ReadToFollowing("Row")) {
            String rowindexstr = rowreader.GetAttribute("ss:Index");
            if (rowindexstr != null) {
              if (!int.TryParse(rowindexstr, out row))
                row = 0; 
            } else
              row++;
            cellCount += ParseRow(rowreader, wb, sheet, row, cellParsingCache);
          }
        }
      }
      return cellCount;
    }

    private int ParseSheets(XmlTextReader reader, Workbook wb, 
                            IDictionary<String, Cell> cellParsingCache) 
    {
      int cellCount = 0;
      while (reader.ReadToFollowing("Worksheet")) {
        String sheetname = reader.GetAttribute("ss:Name");
        using (XmlReader sheetreader = reader.ReadSubtree()) {
          Sheet sheet = wb[sheetname];
          cellCount += ParseSheet(sheetreader, wb, sheet, cellParsingCache);
          if (sheet.IsFunctionSheet) 
            ScanSheet(sheet, RegisterSdfs);
        }
      }
      return cellCount;
    }

    private void ScanSheet(Sheet sheet, Action<Sheet, int, int> f) {
      for (int row = 0; row < sheet.Rows; row++)
        for (int col = 0; col < sheet.Cols; col++)
          f(sheet, col, row);
    }

    // Register SDFs (and maybe later: convert DELAY calls to DelayCell).
    private void RegisterSdfs(Sheet sheet, int col, int row) {
      Cell cell = sheet[col, row];
      if (cell == null || !(cell is Formula))
        return;
      Expr e = (cell as Formula).Expr;
      if (!(e is FunCall))
        return;
      FunCall funCall = e as FunCall;
      Expr[] es = funCall.es;
      switch (funCall.function.name) {
        case "DEFINE":
          if (es.Length >= 2 && es[0] is TextConst && es[1] is CellRef) {
            String sdfName = (es[0] as TextConst).value.value;
            FullCellAddr outputCell = (es[1] as CellRef).GetAbsoluteAddr(sheet, col, row);
            FullCellAddr[] inputCells = new FullCellAddr[es.Length - 2];
            bool ok = true;
            for (int i = 2; ok && i < es.Length; i++) {
              CellRef inputCellRef = es[i] as CellRef;
              ok = inputCellRef != null;
              if (ok)
                inputCells[i - 2] = inputCellRef.GetAbsoluteAddr(sheet, col, row);
            }
            if (ok)
              Funcalc.SdfManager.Register(outputCell, inputCells, sdfName);
          }
          break;
        case "DELAY":
          break;
        default:
          /* do nothing */
          break;
      }
    }

    // Create SDFs 
    private void CreateSdfs(Sheet sheet, int col, int row) {
      Cell cell = sheet[col, row];
      if (cell == null || !(cell is Formula))
        return;
      Expr e = (cell as Formula).Expr;
      if (!(e is FunCall))
        return;
      FunCall funCall = e as FunCall;
      Expr[] es = funCall.es;
      switch (funCall.function.name) {
        case "DEFINE":
          if (es.Length >= 2 && es[0] is TextConst && es[1] is CellRef) {
            String sdfName = (es[0] as TextConst).value.value;
            FullCellAddr outputCell = (es[1] as CellRef).GetAbsoluteAddr(sheet, col, row);
            FullCellAddr[] inputCells = new FullCellAddr[es.Length - 2];
            bool ok = true;
            for (int i = 2; ok && i < es.Length; i++) {
              CellRef inputCellRef = es[i] as CellRef;
              ok = inputCellRef != null;
              if (ok)
                inputCells[i - 2] = inputCellRef.GetAbsoluteAddr(sheet, col, row);
            }
            if (ok)
              Funcalc.SdfManager.CreateFunction(sdfName, outputCell, inputCells);
          }
          break;
        default:
          /* do nothing */
          break;
      }
    }

    private Boolean isXMLSS(XmlTextReader reader) {
      return reader.NodeType == XmlNodeType.Element &&
             reader.Name == "Workbook" &&
             reader.GetAttribute("xmlns") != null;
    }

    private void ParseWorkBook(XmlTextReader reader, Workbook wb) {
      int cellCount = 0;
      // To help find and share cells with identical R1C1 representation:
      IDictionary<String, Cell> cellParsingCache = new Dictionary<String, Cell>();
      while (reader.Read())
        if (isXMLSS(reader))
          cellCount += ParseSheets(reader, wb, cellParsingCache);
      foreach (Sheet sheet in wb)
        if (sheet.IsFunctionSheet)
          ScanSheet(sheet, CreateSdfs);
      wb.RebuildSupportGraph();
      wb.ResetVolatileSet();
      Console.Write("Read XMLSS, {0} cells of which {1} unique ", cellCount, cellParsingCache.Count);
    }

    private Workbook MakeEmptySheets(XmlTextReader reader) {
      Workbook wb = null;
      while (reader.Read()) {
        if (isXMLSS(reader)) {
          wb = new Workbook();
          while (reader.ReadToFollowing("Worksheet")) {
            String sheetname = reader.GetAttribute("ss:Name");
            bool functionSheet = sheetname.StartsWith("@");
            using (XmlReader sheetreader = reader.ReadSubtree()) {
              int cols = MINCOLS, rows = MINROWS; // In the sheet
              if (sheetreader.ReadToFollowing("Table")) {
                // Try to use Expanded{Row,Column}Count although not required
                String tmpstr = sheetreader.GetAttribute("ss:ExpandedRowCount");
                if (int.TryParse(tmpstr, out rows))
                  rows = Math.Max(rows, MINROWS);
                tmpstr = sheetreader.GetAttribute("ss:ExpandedColumnCount");
                if (int.TryParse(tmpstr, out cols))
                  cols = Math.Max(cols, MINCOLS);
              }
              new Sheet(wb, sheetname, cols, rows, functionSheet);
            }
          }
        }
      }
      return wb;
    }

    public override Workbook Read(String filename) {
      XmlTextReader reader = null;
      Workbook wb = null;
      Stopwatch watch = new Stopwatch();
      watch.Reset(); watch.Start();
      try {
        reader = new XmlTextReader(filename);
        reader.WhitespaceHandling = WhitespaceHandling.None;
        wb = MakeEmptySheets(reader);
        if (reader != null)
          reader.Close();
        if (wb != null) {
          reader = new XmlTextReader(filename);
          reader.WhitespaceHandling = WhitespaceHandling.None;
          ParseWorkBook(reader, wb);
        }
      } catch (IOException exn) {
        String msg = "Cannot read workbook";
        MessageBox.Show(msg + "\n" + exn, msg, MessageBoxButtons.OK, MessageBoxIcon.Error);
      } finally {
        if (reader != null)
          reader.Close();
      }
      watch.Stop();
      Console.WriteLine("in {0} ms", watch.ElapsedMilliseconds);
      return wb;
    }
  }

  /// <summary>
  /// A WorkBookIO can read certain XML workbook files.
  /// </summary>
  public class WorkBookIO {
    private List<IOFormat> formats;
    private IOFormat defaultformat;
    public WorkBookIO() {
      formats = new List<IOFormat>();
      AddFormat(new XMLSSIOFormat(), def: true);
      // AddFormat(new GnumericIOFormat(), def: false); // Removed
    }

    private void AddFormat(IOFormat format, bool def) {
      formats.Add(format);
      if (def)
        defaultformat = format;
    }

    private IOFormat FindFormat(String filename) {
      String[] fields = filename.Split((".").ToCharArray());
      String ext = fields[fields.Length - 1];
      foreach (IOFormat format in formats) {
        if (format.ValidExtension(ext))
          return format;
      }
      return null;
    }

    // Attempt to read workbook from file using a supported 
    // format; may return null.

    public Workbook Read(String filename) {
      IOFormat format = FindFormat(filename);
      // If we found a format, try it
      if (format != null) 
        return format.Read(filename);
      else
        return null;
    }

    public String SupportedFormatFilter() {
      StringBuilder sb = new StringBuilder();
      foreach (IOFormat ioformat in formats) {
        sb.Append(ioformat.GetFilter());
        sb.Append("|");
      }

      sb.Append("All files (*.*)|*.*");
      return sb.ToString();
    }

    public int DefaultFormatIndex() {
      return formats.IndexOf(defaultformat) + 1;
    }
  }
}
