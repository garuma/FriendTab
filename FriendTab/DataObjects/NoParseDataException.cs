using System;

namespace FriendTab
{
	public class NoParseDataException : ApplicationException
	{
		public NoParseDataException (string className)
			: base (string.Format ("{0} must be fetched from Parse to allow this operation", className))
		{
		}
	}
}

