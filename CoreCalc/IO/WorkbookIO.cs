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
// Stopwatch
using System.Globalization; // CultureInfo
// MemoryStream, Stream
using System.Text;

using Corecalc;

namespace Corecalc.IO {
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
			if (def) {
				defaultformat = format;
			}
		}

		private IOFormat FindFormat(String filename) {
			String[] fields = filename.Split((".").ToCharArray());
			String ext = fields[fields.Length - 1];
			foreach (IOFormat format in formats) {
				if (format.ValidExtension(ext)) {
					return format;
				}
			}
			return null;
		}

		// Attempt to read workbook from file using a supported 
		// format; may return null.

		public Workbook Read(String filename) {
			IOFormat format = FindFormat(filename);
			// If we found a format, try it
			if (format != null) {
				return format.Read(filename);
			}
			else {
				return null;
			}
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

		public int DefaultFormatIndex() { return formats.IndexOf(defaultformat) + 1; }
	}
}