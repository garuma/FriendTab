
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
using Android.Animation;

namespace FriendTab
{
	public enum ArrowOrientation {
		Left,
		Right
	}

	public class Arrow : View
	{
		ArrowOrientation orientation;
		Paint arrowPaint;
		ValueAnimator animator;

		public Arrow (Context ctx) : base (ctx)
		{
			Initialize ();
		}

		public Arrow (Context context, IAttributeSet attrs) :
			base (context, attrs)
		{
			Initialize ();
		}

		public Arrow (Context context, IAttributeSet attrs, int defStyle) :
			base (context, attrs, defStyle)
		{
			Initialize ();
		}

		void Initialize ()
		{
			orientation = ArrowOrientation.Right;
			arrowPaint = new Paint {
				Color = Color.Rgb (0x77, 0x77, 0x77),
				//Color = Color.Black,
				AntiAlias = true
			};
		}

		public ArrowOrientation Orientation {
			get { return orientation; }
			set {
				if (animator != null) {
					animator.Cancel ();
					animator = null;
				}
				orientation = value;
				Invalidate ();
			}
		}

		public void SetOrientation (ArrowOrientation orientation, bool animate = true)
		{
			Orientation = orientation;

			animator = ValueAnimator.OfFloat (0f, 1f);
			animator.SetDuration (250);
			animator.Update += (sender, e) => Alpha = (float)e.Animation.AnimatedValue;
			animator.AnimationEnd += (sender, e) => { animator.RemoveAllListeners (); animator = null; };
			animator.Start ();
		}

		public void Disappear ()
		{
			if (animator != null) {
				animator.Cancel ();
				animator = null;
			}

			Alpha = 0;
		}

		protected override void OnMeasure (int widthMeasureSpec, int heightMeasureSpec)
		{
			SetMeasuredDimension (16, 32);
		}

		protected override void OnDraw (Android.Graphics.Canvas canvas)
		{
			base.OnDraw (canvas);
			var half = canvas.Height / 2;
			var left = 0;
			var right = canvas.Width - 2;
			var top = 0;
			var bottom = canvas.Height - 1;
			var offsetPlier = 1;

			float[] points = null;
			switch (orientation) {
			case ArrowOrientation.Left:
				points = new float[] {
					right, top,
					left, half,
					left, half,
					right, bottom
				};
				offsetPlier = 1;
				break;
			case ArrowOrientation.Right:
				points = new float[] {
					left, top,
					right, half,
					right, half,
					left, bottom
				};
				offsetPlier = -1;
				break;
			}

			for (int dx = 0; dx < 5; dx++)
				canvas.DrawLines (Offset (points, offsetPlier * dx, 0), arrowPaint);
		}

		float[] Offset (float[] originalPoints, float dx, float dy)
		{
			if (dx == 0 && dy == 0)
				return originalPoints;
			var result = (float[])originalPoints.Clone ();
			for (int i = 0; i < originalPoints.Length; i++)
				result[i] += (i % 2) == 0 ? dx : dy;
			return result;
		}
	}
}

