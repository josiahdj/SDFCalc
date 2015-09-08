using System;
using System.IO;

namespace Corecalc.IO {
	/// <summary>
	/// An IOFormat determines how to read a given XML file containing a
	/// spreadsheet workbook.
	/// Currently, only Excel 2003 XMLSS format is supported.
	/// </summary>
	internal abstract class IOFormat {
		public abstract Workbook Read(String filename);

		private String fileExtension;
		private String description;

		public static Stream MakeStream(String s) {
			char[] cs = s.ToCharArray();
			byte[] bs = new byte[cs.Length];
			for (int i = 0; i < cs.Length; i++) {
				bs[i] = (byte)(cs[i]);
			}
			return new MemoryStream(bs);
		}

		public String GetFilter() { return description + " (*." + fileExtension + ")|*." + fileExtension; }

		public IOFormat(String fileextension, String description) {
			this.fileExtension = fileextension;
			this.description = description;
		}

		public Boolean ValidExtension(String ext) { return this.fileExtension == ext; }
	}
}