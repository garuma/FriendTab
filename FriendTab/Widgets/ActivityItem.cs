
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

namespace FriendTab
{
	public class ActivityItem : LinearLayout
	{
		public event Action<ActivityItem, bool> ItemExpanded;
		public int VersionNumber = int.MinValue;
		ImageView mapView;

		public string ShownId {
			get;
			set;
		}

		public string CurrentMapUri {
			get;
			set;
		}

		public Tuple<double,double> CurrentMapCoordinates {
			get;
			set;
		}

		public string CurrentPerson {
			get;
			set;
		}

		public string CurrentTabType {
			get;
			set;
		}

		public bool Expandable {
			get {
				return true;
			}
		}

		public bool Expanded {
			get {
				return mapView.Height > 0;
			}
			set {
				if (ItemExpanded != null)
					ItemExpanded (this, value);
				//mapView.LayoutParameters.Height = value ? ActivityItemAdapter.MapHeight : 0;
			}
		}

		public ActivityItem (Context context) :
			base (context)
		{
			Initialize ();
		}

		public ActivityItem (Context context, IAttributeSet attrs) :
			base (context, attrs)
		{
			Initialize ();
		}

		public ActivityItem (Context context, IAttributeSet attrs, int defStyle) :
			base (context, attrs, defStyle)
		{
			Initialize ();
		}

		void Initialize ()
		{
			var inflater = (LayoutInflater)Context.GetSystemService (Context.LayoutInflaterService);
			inflater.Inflate (Resource.Layout.ActivityItemLayout, this, true);
			mapView = FindViewById<ImageView> (Resource.Id.MapPicture);
			mapView.Click += (sender, e) => {
				if (CurrentMapCoordinates == null)
					return;
				var coords = CurrentMapCoordinates;
				var uri = "geo:" + coords.Item1 + "," + coords.Item2;
				Context.StartActivity (new Intent(Intent.ActionView, Android.Net.Uri.Parse (uri)));
			};
		}
	}
}

