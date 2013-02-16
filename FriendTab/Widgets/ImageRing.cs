
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
	public class ImageRing : View, ParticipantSelectionFragment.IParticipantDropZone
	{
		bool hovered;
		Bitmap image;
		Bitmap circledImage;
		int ringRadius = 10;
		int borderRadius = 2;

		Paint ringPaint;
		Paint ringPaintHover;
		Paint borderPaint;
		Paint imageEraser;
		Paint eraserChannel;

		public ImageRing (Context context, IAttributeSet attrs) :
			base (context, attrs)
		{
			Initialize ();
		}

		public ImageRing (Context context, IAttributeSet attrs, int defStyle) :
			base (context, attrs, defStyle)
		{
			Initialize ();
		}

		private void Initialize ()
		{
			ringPaint = new Paint () {
				Color = Color.Rgb (0x99, 0xcc, 0),
				AntiAlias = true
			};
			ringPaintHover = new Paint () {
				Color = Color.Rgb (0xcc, 0xff, 0),
				AntiAlias = true
			};
			borderPaint = new Paint () {
				Color = Color.Rgb (0x66, 0x99, 0),
				AntiAlias = true
			};
			var eraseColor = Color.Yellow;
			imageEraser = new Paint ();
			eraserChannel = new Paint () {
				Color = eraseColor,
				AntiAlias = true
			};
			imageEraser.SetXfermode (new AvoidXfermode (eraseColor, 255, AvoidXfermode.Mode.Target));

			image = BitmapFactory.DecodeResource (Resources, Resource.Drawable.ic_contact_picture);
			PrepImage ();
			//SetLayerType (Android.Views.LayerType.Software, null);
		}

		public Bitmap Image {
			get {
				return image;
			}
			set {
				image = value;
				PrepImage ();
				RequestLayout ();
				Invalidate ();
			}
		}

		public int RingRadius {
			get {
				return ringRadius;
			}
			set {
				ringRadius = value;
				Invalidate ();
			}
		}

		public int BorderRadius {
			get {
				return borderRadius;
			}
			set {
				borderRadius = value;
				Invalidate ();
			}
		}
		
		public void SetDragOverState (bool hovering)
		{
			if (hovering != hovered) {
				hovered = hovering;
				Invalidate ();
			}
		}

		void PrepImage ()
		{
			var length = Math.Min (image.Width, image.Height);
			var middle = length / 2;
			var left = Math.Max (0, (image.Width - length) / 2);
			var top = Math.Max (0, (image.Height - length) / 2);
			var buffer = Bitmap.CreateBitmap (length, length, Bitmap.Config.Argb8888);

			using (Canvas c = new Canvas (buffer)) {
				c.DrawColor (Color.Transparent);
				c.DrawCircle (middle, middle, middle, eraserChannel);
				c.DrawBitmap (image, new Rect (left, top, length, length), new Rect (0, 0, length, length), imageEraser);
				/*c.DrawBitmap (image, new Rect (left, top, length, length), new Rect (0, 0, length, length), null);
				var path = new Path ();
				path.AddCircle (middle, middle, middle, Path.Direction.Cw);
				c.ClipPath (path, Region.Op.Difference);
				c.DrawPaint (imageEraser);*/
			}
			circledImage = buffer;
		}

		protected override void OnMeasure (int widthMeasureSpec, int heightMeasureSpec)
		{
			// We make things square
			var length = circledImage.Width + 2 * (ringRadius + borderRadius + 1);
			SetMeasuredDimension (length, length);
		}

		public override void Draw (Canvas canvas)
		{
			var middle = canvas.Height / 2;
			var imageLength = circledImage.Width;
			var left = Math.Max (0, (canvas.Width - imageLength) / 2);
			var top = Math.Max (0, (canvas.Height - imageLength) / 2);

			canvas.DrawCircle (middle, middle, imageLength / 2 + ringRadius + borderRadius, borderPaint);
			canvas.DrawCircle (middle, middle, imageLength / 2 + ringRadius, hovered ? ringPaintHover : ringPaint);
			canvas.DrawBitmap (circledImage, left, top, null);
		}
	}
}

