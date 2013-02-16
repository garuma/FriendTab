
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Android.Graphics;

namespace FriendTab
{
	public class InsetTextView : View
	{
		static readonly int DefaultFontSize = 35.ToPixels ();
		const float Offset = 1;

		string text;
		Paint textPaint;
		Paint lightPaint;
		Paint darkPaint;

		public InsetTextView (Context context) : base (context)
		{
			Initialize ();
		}

		public InsetTextView (Context context, IAttributeSet attrs) :
			base (context, attrs)
		{
			text = attrs.GetAttributeValue (null, "text");
			Initialize ();
		}

		public InsetTextView (Context context, IAttributeSet attrs, int defStyle) :
			base (context, attrs, defStyle)
		{
			text = attrs.GetAttributeValue (null, "text");
			Initialize ();
		}

		private void Initialize ()
		{
			textPaint = new Paint () {
				Color = Color.Rgb (0xf3, 0xf3, 0xf3),
				AntiAlias = true,
				TextAlign = Paint.Align.Center,
				TextSize = DefaultFontSize
			};
			textPaint.SetTypeface (Typeface.DefaultBold);
			lightPaint = new Paint () {
				Color = Color.Rgb (0xff, 0xff, 0xff),
				AntiAlias = true,
				TextAlign = Paint.Align.Center,
				TextSize = DefaultFontSize
			};
			lightPaint.SetTypeface (Typeface.DefaultBold);
			darkPaint = new Paint () {
				Color = Color.Argb (0x30, 0, 0, 0),
				AntiAlias = true,
				TextAlign = Paint.Align.Center,
				TextSize = DefaultFontSize
			};
			darkPaint.SetTypeface (Typeface.DefaultBold);
		}

		public string Text {
			get {
				return text;
			}
			set {
				text = value ?? string.Empty;
				Invalidate ();
			}
		}

		public Color TextColor {
			get {
				return textPaint.Color;
			}
			set {
				textPaint.Color = value;
				Invalidate ();
			}
		}

		protected override void OnMeasure (int widthMeasureSpec, int heightMeasureSpec)
		{
			var width = (int)Math.Ceiling (textPaint.MeasureText (text)) + 2;
			var metrics = textPaint.GetFontMetricsInt ();
			var height = -metrics.Top + metrics.Bottom + 2;

			SetMeasuredDimension (width, height);
		}

		protected override void OnDraw (Canvas canvas)
		{
			var hCenter = canvas.Width / 2;
			var vCenter = canvas.Height - (textPaint.GetFontMetricsInt ().Bottom + 1);

			canvas.DrawText (text, hCenter - Offset, vCenter - Offset, darkPaint);
			canvas.DrawText (text, hCenter + Offset, vCenter + Offset, lightPaint);
			canvas.DrawText (text, hCenter, vCenter, textPaint);
		}
	}
}

