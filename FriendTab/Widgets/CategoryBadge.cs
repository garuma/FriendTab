
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
	public class CategoryBadge : View
	{
		readonly static int OverallSize = 102.ToPixels ();
		readonly static int radius = 34.ToPixels ();
		readonly static int radiusBorder = 1;
		readonly static int removeRadius = 12.ToPixels ();
		readonly static int innerRadius = 10.ToPixels ();
		readonly static int offset = 3.ToPixels ();
		readonly static int bubbleOffset = 12.ToPixels ();
		readonly static int center;
		readonly static int mainCenter;

		static Paint backPaint;
		static Paint darkerBackPaint;
		static Paint shadowPaint;
		static Paint removePaint;
		static Paint redPaint;
		static Paint greenPaint;
		static Paint imgPaint;

		Bitmap icon;
		int count;
		int currentBubbleTransparency = 255;

		public CategoryBadge (Context context) : base (context)
		{
			Initialize ();
		}

		public CategoryBadge (Context context, IAttributeSet attrs) :
			base (context, attrs)
		{
			Initialize ();
		}

		public CategoryBadge (Context context, IAttributeSet attrs, int defStyle) :
			base (context, attrs, defStyle)
		{
			Initialize ();
		}

		static CategoryBadge ()
		{
			center = OverallSize / 2;
			mainCenter = center + offset;

			backPaint = new Paint () {
				Color = Color.Rgb (0xc7, 0xc7, 0xc7),
				AntiAlias = true
			};
			backPaint.SetShader (new LinearGradient (mainCenter, 0, mainCenter, radius * 2,
			                                         Color.Rgb (0xc7, 0xc7, 0xc7),
			                                         Color.Argb (0xa2, 0xc7, 0xc70, 0xc7),
			                                         //Color.Argb (0xa2, 0xc7, 0xc7, 0xc7),
			                                         Shader.TileMode.Clamp));
			darkerBackPaint = new Paint () {
				Color = Color.Rgb (0xa7, 0xa7, 0xa7),
				AntiAlias = true
			};
			shadowPaint = new Paint () {
				Color = Color.Rgb (0x9a, 0x9a, 0x9a),
				AntiAlias = true
			};
			removePaint = new Paint () {
				AntiAlias = true,
				Color = Color.White,
				TextAlign = Paint.Align.Center,
				TextSize = 14.ToPixels ()
			};
			//removePaint.SetXfermode (new PorterDuffXfermode (PorterDuff.Mode.Clear));
			removePaint.SetXfermode (new PorterDuffXfermode (PorterDuff.Mode.DstIn));
			redPaint = new Paint () {
				Color = Color.Rgb (0xff, 0x44, 0x44),
				AntiAlias = true
			};
			greenPaint = new Paint () {
				Color = Color.Rgb (0x99, 0xcc, 0x00),
				AntiAlias = true
			};			
			imgPaint = new Paint () {
				AntiAlias = false,
				FilterBitmap = false
			};
		}

		// Register a function that tells when a drag operation can be started
		public static Func<bool> StartDragValidate {
			get;
			set;
		}

		public Bitmap IconDrawable {
			get {
				return icon;
			}
			set {
				icon = value;
				Invalidate ();
			}
		}

		public string ItemName {
			get;
			set;
		}

		// The number displayed in the bubble,
		// if 0, nothing is displayed
		// if positive then the dude owe us
		// if negative we owe the dude something
		public int Count {
			get {
				return count;
			}
			set {
				if (count == value)
					return;
				count = value;
				Invalidate ();
			}
		}

		public void SetCount (int count, bool animate)
		{
			if (!animate) {
				Count = count;
				return;
			}

			ValueAnimator outAnimator = null;

			if (this.count != 0) {
				outAnimator = ValueAnimator.OfInt (255, 0);
				outAnimator.SetDuration (250);
				outAnimator.Update += (sender, e) => { currentBubbleTransparency = (int)e.Animation.AnimatedValue; Invalidate (); };
				outAnimator.AnimationEnd += (sender, e) => { this.count = count; outAnimator.RemoveAllListeners (); };
			} else {
				this.count = count;
			}

			var inAnimator = ValueAnimator.OfInt (0, 255);
			inAnimator.SetDuration (700);
			inAnimator.Update += (sender, e) => { currentBubbleTransparency = (int)e.Animation.AnimatedValue; Invalidate (); };
			inAnimator.AnimationEnd += (sender, e) => inAnimator.RemoveAllListeners ();

			if (outAnimator != null) {
				var set = new AnimatorSet ();
				set.PlaySequentially (outAnimator, inAnimator);
				set.Start ();
			} else {
				inAnimator.Start ();
			}
		}

		void Initialize ()
		{
			// Hardware layer doesn't let us clear
			SetLayerType (Android.Views.LayerType.Software, null);
			LongClick += HandleClick;
		}

		void HandleClick (object sender, EventArgs e)
		{
			if (StartDragValidate == null || !StartDragValidate ()) {
				Toast.MakeText (Context, "You need to select a contact first", ToastLength.Short).Show ();
			} else if (!TabPerson.CurrentPerson.IsVerified) {
				Toast.MakeText (Context, "You need to verify your account email first", ToastLength.Short).Show ();
			} else {
				var data = ClipData.NewPlainText ("category", ItemName);
				StartDrag (data, new CategoryShadowBuilder (this), null, 0);
			}
		}

		class CategoryShadowBuilder : View.DragShadowBuilder
		{
			const int centerOffset = 52;

			int width, height;

			public CategoryShadowBuilder (View baseView) : base (baseView)
			{
			}

			public override void OnProvideShadowMetrics (Point shadowSize, Point shadowTouchPoint)
			{
				width = View.Width;
				height = View.Height;

				shadowSize.Set (width * 2, height * 2);
				// touch point is in the middle of the (height, width) top-right rect
				shadowTouchPoint.Set (width + width / 2 - centerOffset, height / 2 + centerOffset);
			}

			public override void OnDrawShadow (Canvas canvas)
			{
				const float sepAngle = (float)Math.PI / 16;
				const float circleRadius = 2f;

				// Draw the shadow circles in the top-right corner
				float centerX = width + width / 2 - centerOffset;
				float centerY = height / 2 + centerOffset;
				var baseColor = Color.Black;
				var paint = new Paint () {
					AntiAlias = true,
					Color = baseColor
				};
				canvas.DrawCircle (centerX, centerY, circleRadius + 1, paint);
				for (int radOffset = 70; centerX + radOffset < canvas.Width; radOffset += 20) {
					// Vary the alpha channel based on how far the dot is
					baseColor.A = (byte)(128 * (2f * (width / 2f - 1.3f * radOffset + 60) / width) + 100);
					paint.Color = baseColor;
					// Draw the dots along a circle of radius radOffset and centered on centerX,centerY
					for (float angle = 0; angle < Math.PI * 2; angle += sepAngle) {
						var pointX = centerX + (float)Math.Cos (angle) * radOffset;
						var pointY = centerY + (float)Math.Sin (angle) * radOffset;
						canvas.DrawCircle (pointX, pointY, circleRadius, paint);
					}
				}

				// Draw the category in the upper-left corner
				canvas.DrawBitmap (View.DrawingCache, 0, 0, null);
			}
		}

		public override void Draw (Android.Graphics.Canvas canvas)
		{
			canvas.DrawCircle (mainCenter, mainCenter + 2, radius + radiusBorder, shadowPaint);
			canvas.DrawCircle (mainCenter, mainCenter, radius + radiusBorder, darkerBackPaint);
			canvas.DrawCircle (mainCenter, mainCenter, radius, backPaint);
			canvas.DrawBitmap (icon, mainCenter - icon.Width / 2, mainCenter - icon.Height / 2, imgPaint);

			if (Count != 0) {
				int bubbleCenter = center - removeRadius - bubbleOffset;
				greenPaint.Alpha = redPaint.Alpha = currentBubbleTransparency;
				removePaint.Alpha = 255 - currentBubbleTransparency;
				canvas.DrawCircle (bubbleCenter, bubbleCenter, removeRadius, removePaint);
				canvas.DrawCircle (bubbleCenter, bubbleCenter, innerRadius, Count < 0 ? redPaint : greenPaint);

				var c = Math.Abs (Count).ToString ();
				var textBounds = new Rect ();
				removePaint.GetTextBounds (c, 0, c.Length, textBounds);

				canvas.DrawText (c, bubbleCenter, bubbleCenter + textBounds.Height () / 2, removePaint);
			}
			DrawingCacheEnabled = true;
		}

		protected override void OnMeasure (int widthMeasureSpec, int heightMeasureSpec)
		{
			SetMeasuredDimension (OverallSize, OverallSize);
		}
	}
}

