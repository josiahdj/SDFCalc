namespace Corecalc {
	public class Formats {
		public enum RefType { A1, C0R0, R1C1 }

		public RefType RefFmt { get; set; } = RefType.A1;

		public char RangeDelim { get; set; } = ':';

		public char ArgDelim { get; set; } = ',';

		public bool ShowFormulas { get; set; }
	}
}