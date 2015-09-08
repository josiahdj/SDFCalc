using System;

namespace CoreCalc.Types {
	/// <summary>
	/// An ImpossibleException signals a violation of internal consistency 
	/// assumptions in the spreadsheet implementation.
	/// </summary>
	internal class ImpossibleException : Exception {
		public ImpossibleException(String msg) : base(msg) { }
	}
}