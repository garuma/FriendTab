using System;
using Parse;

namespace FriendTab
{
	public static class ParseExtensions
	{
		public static TParse GetOrNull<TParse> (this ParseObject obj, string key) where TParse : class
		{
			TParse result = null;
			obj.TryGetValue (key, out result);
			return result;
		}
	}
}

