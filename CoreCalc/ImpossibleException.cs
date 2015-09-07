using System;

namespace Corecalc {
	/// <summary>
	/// An ImpossibleException signals a violation of internal consistency 
	/// assumptions in the spreadsheet implementation.
	/// </summary>
	class ImpossibleException : Exception {
		public ImpossibleException(String msg) : base(msg) { }
	}
}