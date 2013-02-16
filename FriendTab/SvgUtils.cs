using System;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Android.Graphics;
using Android.Animation;
using Android.Graphics.Drawables;
using Android.Views.Animations;

using SVGParser = Org.Anddev.Andengine.Extension.Svg.SVGParser;

namespace FriendTab
{
	public static class SvgUtils
	{
		public static Bitmap GetBitmapFromSvgRes (Android.Content.Res.Resources resources, int resID,
		                                          int width, int height)
		{
			var svg = SVGParser.ParseSVGFromResource (resources,
			                                          resID);
			var bmp = Bitmap.CreateBitmap (width, height, Bitmap.Config.Argb8888);
			using (var c = new Canvas (bmp)) {
				var dst = new RectF (0, 0, width, height); 
				c.DrawPicture (svg.Picture, dst);
			}
			// Returns an immutable copy
			return Bitmap.CreateBitmap (bmp);
		}

		public static Bitmap GetBitmapFromSvgString (string svgString, int width, int height)
		{
			var svg = SVGParser.ParseSVGFromString (svgString);
			var bmp = Bitmap.CreateBitmap (width, height, Bitmap.Config.Argb8888);
			using (var c = new Canvas (bmp)) {
				var dst = new RectF (0, 0, width, height); 
				c.DrawPicture (svg.Picture, dst);
			}
			// Returns an immutable copy
			return Bitmap.CreateBitmap (bmp);
		}
	}
}

