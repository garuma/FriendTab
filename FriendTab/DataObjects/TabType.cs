using System;
using System.Linq;
using System.Collections;
using System.Threading.Tasks;
using System.Collections.Generic;

using Android.Graphics;
using Android.Util;

using Parse;

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
				Name = parseTabType.Get<string> ("name"),
				Description = parseTabType.Get<string> ("description"),
				//ParseImage = (ParseFile)parseTabType.Get ("image"),
				SvgImage = parseTabType.Get<string> ("scalable"),
				parseData = parseTabType
			};
		}

		public ParseObject ToParse ()
		{
			if (parseData != null)
				return parseData;

			var obj = new ParseObject ("TabType");
			obj["name"] = Name;
			obj["description"] = Description;
			obj["scalable"] = SvgImage;
			parseData = obj;
			obj.SaveAsync ();

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
		static IEnumerable<TabType> tabTypes;

		public static async Task<IEnumerable<TabType>> GetTabTypes ()
		{
			if (tabTypes != null)
				return tabTypes;

			tabTypes = (await ParseObject.GetQuery ("TabType").FindAsync ().ConfigureAwait (false))
				.Select (TabType.FromParse)
				.ToArray ();
			return tabTypes;
		}
	}
}

