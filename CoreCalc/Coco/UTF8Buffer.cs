namespace CoreCalc.Coco {
	public class UTF8Buffer : Buffer {
		public UTF8Buffer(Buffer b) : base(b) { }

		public override int Read() {
			int ch;
			do {
				ch = base.Read();
				// until we find a utf8 start (0xxxxxxx or 11xxxxxx)
			} while ((ch >= 128) && ((ch & 0xC0) != 0xC0) && (ch != EOF));
			if (ch < 128 || ch == EOF) {
				// nothing to do, first 127 chars are the same in ascii and utf8
				// 0xxxxxxx or end of file character
			}
			else if ((ch & 0xF0) == 0xF0) {
				// 11110xxx 10xxxxxx 10xxxxxx 10xxxxxx
				int c1 = ch & 0x07;
				ch = base.Read();
				int c2 = ch & 0x3F;
				ch = base.Read();
				int c3 = ch & 0x3F;
				ch = base.Read();
				int c4 = ch & 0x3F;
				ch = (((((c1 << 6) | c2) << 6) | c3) << 6) | c4;
			}
			else if ((ch & 0xE0) == 0xE0) {
				// 1110xxxx 10xxxxxx 10xxxxxx
				int c1 = ch & 0x0F;
				ch = base.Read();
				int c2 = ch & 0x3F;
				ch = base.Read();
				int c3 = ch & 0x3F;
				ch = (((c1 << 6) | c2) << 6) | c3;
			}
			else if ((ch & 0xC0) == 0xC0) {
				// 110xxxxx 10xxxxxx
				int c1 = ch & 0x1F;
				ch = base.Read();
				int c2 = ch & 0x3F;
				ch = (c1 << 6) | c2;
			}
			return ch;
		}
	}
}