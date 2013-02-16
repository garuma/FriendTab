
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
	public class ColorSeparator : View
	{
		Color color;

		public ColorSeparator (Context context, IAttributeSet attrs) :
			base (context, attrs)
		{
			Initialize ();
		}

		public ColorSeparator (Context context, IAttributeSet attrs, int defStyle) :
			base (context, attrs, defStyle)
		{
			Initialize ();
		}

		private void Initialize ()
		{
			Height = 2;
			color = Color.Rgb (0x99, 0x99, 0x99);
		}

		public Color SeparatorColor {
			get {
				return color;
			}
			set {
				color = value;
				Invalidate ();
			}
		}

		public new int Height {
			get;
			set;
		}

		protected override void OnMeasure (int widthMeasureSpec, int heightMeasureSpec)
		{
			SetMeasuredDimension (View.MeasureSpec.GetSize (widthMeasureSpec),
			                      Height);
		}

		public override void Draw (Canvas canvas)
		{
			canvas.DrawColor (color);
		}
	}
}

