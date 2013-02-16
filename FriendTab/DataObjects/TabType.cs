using System;
using System.Linq;
using System.Collections;
using System.Threading.Tasks;
using System.Collections.Generic;

using Android.Graphics;
using Android.Util;

using ParseLib;

namespace FriendTab
{
	public class TabType : IEquatable<TabType>
	{
		//TaskCompletionSource<Bitmap> tcs;
		ParseObject parseData;
		Bitmap image;

		//static DiskCache cache;

		public string Name { get; set; }
		public string Description { get; set; }
		public string SvgImage { get; set; }

		public Bitmap Image {
			get {
				return image ?? (image = SvgUtils.GetBitmapFromSvgString (SvgImage, 36.ToPixels (), 36.ToPixels ()));
			}
		}
		/*public ParseFile ParseImage { get; set; }
		public Task<Bitmap> Image {
			get {
				if (tcs != null)
					return tcs.Task;
				tcs = new TaskCompletionSource<Bitmap> ();
				Bitmap bmp = null;

				if (cache != null && cache.TryGet (Name, out bmp) && bmp != null) {
					Android.Util.Log.Info ("Cache", "Cache hit!");
					tcs.SetResult (bmp);
					return tcs.Task;
				}

				ParseImage.GetDataInBackground (new TabGetDataCallback ((bs, e) => {
					if (e == null) {
						bmp = BitmapFactory.DecodeByteArray (bs, 0, bs.Length);
						tcs.SetResult (bmp);
						if (cache != null)
							cache.AddOrUpdate (Name, bmp, TimeSpan.FromDays (7));
					} else {
						Log.Error ("TabTypeImage", e.ToString ());
						tcs.SetException (e);
					}
				}));

				return tcs.Task;
			}
		}*/

		public static TabType FromParse (ParseObject parseTabType)
		{
			return new TabType {
				Name = parseTabType.GetString ("name"),
				Description = parseTabType.GetString ("description"),
				//ParseImage = (ParseFile)parseTabType.Get ("image"),
				SvgImage = parseTabType.GetString ("scalable"),
				parseData = parseTabType
			};
		}

		public ParseObject ToParse ()
		{
			if (parseData != null)
				return parseData;

			var obj = new ParseObject ("TabType");
			obj.Put ("name", Name);
			obj.Put ("description", Description);
			obj.Put ("scalable", SvgImage);
			parseData = obj;
			obj.SaveEventually ();

			// TODO: handle image creation, probably gonna wait on a Project Noun API
			return obj;
		}

		public static void InitializeCache (Android.Content.Context ctx)
		{
			//cache = DiskCache.CreateCache (ctx, "TabTypes");
		}

		public bool Equals (TabType rhs)
		{
			return !object.ReferenceEquals (rhs, null) && Name == rhs.Name;
		}
		
		public override int GetHashCode ()
		{
			return Name.GetHashCode ();
		}
		
		public override bool Equals (object obj)
		{
			var other = obj as TabType;
			return other != null && Equals (other);
		}

		public static bool operator== (TabType t1, TabType t2)
		{
			return !(object.ReferenceEquals (t1, null) ^ object.ReferenceEquals (t2, null))
				&& (object.ReferenceEquals (t1, t2) || t1.Equals (t2));
		}
		
		public static bool operator!= (TabType t1, TabType t2)
		{
			return !(t1 == t2);
		}
	}

	public static class TabTypes
	{
		static TaskCompletionSource<IEnumerable<TabType>> tcs;

		public static Task<IEnumerable<TabType>> GetTabTypes ()
		{
			if (tcs != null)
				return tcs.Task;

			tcs = new TaskCompletionSource<IEnumerable<TabType>> ();
			var query = new ParseQuery ("TabType");
			query.SetCachePolicy (ParseQuery.CachePolicy.CacheElseNetwork);
			query.MaxCacheAge = (long)TimeSpan.FromDays (7).TotalMilliseconds;
			query.FindInBackground (new TabFindCallback ((ls, e) => {
				if (e == null) {
					var types = ls.Select (TabType.FromParse).ToArray ();
					tcs.SetResult (types);
				} else {
					Log.Error ("TabTypes", e.ToString ());
					tcs.SetException (e);
				}
			}));

			return tcs.Task;
		}
	}

	class TabGetDataCallback : GetDataCallback
	{
		Action<byte[], ParseException> action;

		public TabGetDataCallback (Action<byte[], ParseException> action)
		{
			this.action = action;
		}

		public override void Done (byte[] data, ParseException e)
		{
			action (data, e);
		}
	}

	class TabFindCallback : FindCallback
	{
		Action<IList<ParseObject>, ParseException> action;

		public TabFindCallback (Action<IList<ParseObject>, ParseException> action)
		{
			this.action = action;
		}

		public override void Done (IList<ParseObject> objects, ParseException e)
		{
			action (objects, e);
		}
	}
}

