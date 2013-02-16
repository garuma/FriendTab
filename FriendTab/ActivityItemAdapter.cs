
using System;
using System.Net;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Provider;
using Android.Views.Animations;
using AUri = Android.Net.Uri;

namespace FriendTab
{
	public class ActivityItemAdapter : BaseAdapter
	{
		public const int MapHeight = 150;
		const string MapsApiKey = "AIzaSyBCV2cg27itQAx2M0gZbjnTJ3dUUOTB7bI";

		List<TabObject> data = new List<TabObject> ();
		Activity activity;
		int mapWidth;

		BitmapCache cache;
		BitmapCache faceCache;

		readonly Bitmap NoFacePicture;
		readonly Drawable NoFaceDrawable;
		readonly Bitmap NoMapPicture; 

		Dictionary<string, Task> pendingFetch = new Dictionary<string, Task> ();

		public ActivityItemAdapter (Activity activity)
		{
			this.activity = activity;
			this.cache = BitmapCache.CreateCache (activity, "MapCache");
			this.faceCache = BitmapCache.CreateCache (activity, "FaceCache");

			var display = activity.WindowManager.DefaultDisplay;
			var point = new Point ();
			display.GetSize (point);
			this.mapWidth = point.X;

			NoFacePicture = SvgUtils.GetBitmapFromSvgRes (activity.Resources,
			                                              Resource.Drawable.ic_contact_picture,
			                                              64.ToPixels (),
			                                              64.ToPixels ());
			NoFaceDrawable = new VignetteDrawable (NoFacePicture, withEffect: false);
			NoMapPicture = PrepareNoMapPicture (Resource.Drawable.gmaps);
		}

		Bitmap PrepareNoMapPicture (int baseImage)
		{
			var bmp = Bitmap.CreateBitmap (mapWidth, MapHeight, Bitmap.Config.Argb8888);
			var baseBmp = BitmapFactory.DecodeResource (activity.Resources, baseImage);
			bmp.EraseColor (Color.Rgb (227, 227, 227));
			using (var canvas = new Canvas (bmp))
				canvas.DrawBitmap (baseBmp,
				                   (int)(mapWidth / 2 - baseBmp.Width / 2),
				                   (int)(MapHeight / 2 - baseBmp.Height / 2),
				                   null);

			return bmp;
		}

		public void FeedData (IEnumerable<TabObject> newData)
		{
			data.AddRange (newData);
			NotifyDataSetChanged ();
		}

		public void PrependData (TabObject newData)
		{
			data.Insert (0, newData);
			NotifyDataSetChanged ();
		}

		public override View GetView (int position, View convertView, ViewGroup parent)
		{
			var view = EnsureView (parent, convertView);
			var versionNumber = Interlocked.Increment (ref view.VersionNumber);

			var item = data [position];

			if (item.Id.Equals (view.ShownId, StringComparison.OrdinalIgnoreCase))
				return view;

			view.ShownId = item.Id;
			var mapView = view.FindViewById<FadeImageView> (Resource.Id.MapPicture);
			var contactPicture = view.FindViewById<FadeImageView> (Resource.Id.ContactPicture);
			var contactName = view.FindViewById<TextView> (Resource.Id.ContactName);
			var locationInfo = view.FindViewById<TextView> (Resource.Id.locationInfo);
			var timeInfo = view.FindViewById<TextView> (Resource.Id.timeInfo);
			var mainFrame = view.FindViewById<View> (Resource.Id.mainFrame);
			var secondaryFrame = view.FindViewById<View> (Resource.Id.secondaryFrame);
			var headerSeparator = view.FindViewById<TextView> (Resource.Id.HeaderSeparator);

			// Load map
			view.CurrentMapCoordinates = item.LatLng;
			view.CurrentMapUri = null;
			mapView.SetImageBitmap (NoMapPicture);
			mapView.LayoutParameters.Height = 0;

			// Use the other actor in the transaction
			var otherPerson = item.Originator.DisplayName == TabPerson.CurrentPerson.DisplayName ? item.Recipient : item.Originator;
			if (otherPerson.DisplayName != view.CurrentPerson) {
				contactPicture.SetImageDrawable (NoFaceDrawable);
				Bitmap bmp = null;
				var other = otherPerson.GetAndroidPersonDetail ();
				if (other.IsCompleted) {
					bmp = GetPersonPicture (other.Result);
					if (bmp != null)
						contactPicture.SetImageDrawable (new VignetteDrawable (bmp), true);
					else
						contactPicture.SetImageDrawable (NoFaceDrawable);
					view.CurrentPerson = otherPerson.DisplayName;
				} else {
					other.ContinueWith (t => {
						if (view.VersionNumber != versionNumber)
							return;
						bmp = GetPersonPicture (t.Result);
						activity.RunOnUiThread (() => {
							if (view.VersionNumber != versionNumber)
								return;
							if (bmp != null)
								contactPicture.SetImageDrawable (new VignetteDrawable (bmp), true);
							else
								contactPicture.SetImageDrawable (NoFaceDrawable);
							view.CurrentPerson = otherPerson.DisplayName;
						});
					}, TaskContinuationOptions.ExecuteSynchronously);
				}
			}

			// Load tab type icon
			LoadTabIconForItem (view, item.Type, versionNumber);

			contactName.Text = otherPerson.DisplayName.Replace (" ", System.Environment.NewLine);
			locationInfo.Text = item.LocationDesc;
			timeInfo.Text = item.Time.ToString ("d") + " " + item.Time.ToString ("HH:mm");

			if (item.Direction == TabDirection.Giving && otherPerson == item.Recipient
			    || item.Direction == TabDirection.Receiving && otherPerson == item.Originator) {
				// Green
				mainFrame.SetBackgroundColor (Color.Rgb (0x99, 0xCC, 0x00));
				secondaryFrame.SetBackgroundColor (Color.Rgb (0x66, 0x99, 0x00));
				headerSeparator.Text = "↜";
			} else {
				// Red
				mainFrame.SetBackgroundColor (Color.Rgb (0xff, 0x44, 0x44));
				secondaryFrame.SetBackgroundColor (Color.Rgb (0xcc, 0x00, 0x00));
				headerSeparator.Text = "↝";
			}

			// We animate the view if it's the first time it's added
			if (!item.Consummed) {
				item.Consummed = true;
				var animation = AnimationUtils.MakeInChildBottomAnimation (view.Context);
				view.StartAnimation (animation);
			}

			return view;
		}

		Bitmap GetPersonPicture (AndroidPerson androidDetails)
		{
			if (string.IsNullOrEmpty (androidDetails.LookupID))
				return null;

			Bitmap bmp = null;
			if (faceCache.TryGet (androidDetails.LookupID, out bmp))
				return bmp;

			var lookupUri = AUri.WithAppendedPath (AUri.WithAppendedPath (ContactsContract.Contacts.ContentLookupUri,
			                                                              androidDetails.LookupID),
			                                       androidDetails.ContactID);
			var contactUri = ContactsContract.Contacts.LookupContact (activity.ContentResolver,
			                                                          lookupUri);
			if (contactUri == null)
				return null;
			var photo = ContactsContract.Contacts.OpenContactPhotoInputStream (activity.ContentResolver,
			                                                                   contactUri,
			                                                                   true);
			if (photo == null)
				return null;
			bmp = Bitmap.CreateScaledBitmap (BitmapFactory.DecodeStream (photo),
			                                 64.ToPixels (),
			                                 64.ToPixels (),
			                                 true);
			faceCache.AddOrUpdate (androidDetails.LookupID, bmp, TimeSpan.FromDays (1));

			return bmp;
		}

		void LoadMapForItem (ActivityItem view, Tuple<double, double> latLng, int versionNumber)
		{
			FadeImageView mapView = view.FindViewById<FadeImageView> (Resource.Id.MapPicture);
			string url = BuildMapUrl (latLng, mapWidth, MapHeight);

			if (url == view.CurrentMapUri)
				return;

			Bitmap map = null;
			if (cache.TryGet (url, out map)) {
				mapView.SetImageBitmap (map, true);
				view.CurrentMapUri = url;
				view.CurrentMapCoordinates = latLng;
			} else {
				mapView.SetImageBitmap (NoMapPicture);
				Action doMapSetting = () => {
					if (view.VersionNumber != versionNumber)
					return;
					activity.RunOnUiThread (() => {
						if (view.VersionNumber != versionNumber)
							return;
						if (map == null)
							cache.TryGet (url, out map);
						mapView.SetImageBitmap (map, true);
						view.CurrentMapUri = url;
						view.CurrentMapCoordinates = latLng;
					});
				};
				if (pendingFetch.ContainsKey (url))
					pendingFetch [url].ContinueWith (t => doMapSetting (), TaskContinuationOptions.ExecuteSynchronously);
				else
					pendingFetch[url] = SerialScheduler.Factory.StartNew (() => {
						map = DownloadoCacher (url);
						doMapSetting ();
					});
			}
		}

		void LoadTabIconForItem (ActivityItem view, TabType type, int versionNumber)
		{
			ImageView itemPicture = view.FindViewById<ImageView> (Resource.Id.ItemPicture);
			if (view.CurrentTabType != type.Name)
				itemPicture.SetImageBitmap (type.Image);
		}

		public override Java.Lang.Object GetItem (int position)
		{
			return new Java.Lang.String (data[position].Originator.DisplayName);
		}

		public override long GetItemId (int position)
		{
			return position;
		}

		public override bool HasStableIds {
			get {
				return true;
			}
		}

		public override int Count {
			get {
				return data.Count;
			}
		}

		public override int GetItemViewType (int position)
		{
			return 0;
		}

		public override int ViewTypeCount {
			get {
				return 1;
			}
		}

		public override bool AreAllItemsEnabled ()
		{
			return true;
		}

		public override bool IsEnabled (int position)
		{
			return true;
		}

		public override bool IsEmpty {
			get {
				return data.Count == 0;
			}
		}

		ActivityItem EnsureView (ViewGroup root, View convertView)
		{
			if (convertView != null)
				return (ActivityItem)convertView;
			var activityView = new ActivityItem (activity);
			activityView.ItemExpanded += (v, expanded) => {
				if (expanded)
					LoadMapForItem (v, v.CurrentMapCoordinates, v.VersionNumber);
			};
			return activityView;
		}

		string BuildMapUrl (Tuple<double, double> latLng, double width, double height)
		{
			string baseUrl = "https://maps.googleapis.com/maps/api/staticmap?";
			string location = string.Format ("{0},{1}", latLng.Item1, latLng.Item2);
			string size = string.Format ("{0}x{1}", width, height);
			var arguments = string.Format ("center={0}&zoom=14&scale=1&sensor=false&markers={0}&size={1}&key={2}",
			                               location, size, MapsApiKey);
			return baseUrl + arguments;
		}

		Bitmap DownloadoCacher (string url)
		{
			Bitmap map = null;
			if (cache.TryGet (url, out map))
				return map;

			var wc = new WebClient ();
			byte[] bytes = wc.DownloadData (url);
			map = BitmapFactory.DecodeByteArray (bytes, 0, bytes.Length);
			cache.AddOrUpdate (url, map, TimeSpan.FromDays (7));
			return map;
		}
	}
}

