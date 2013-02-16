
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace FriendTab
{
	class ActivityItemData : Java.Lang.Object
	{
		public Android.Net.Uri UserID { get; set; }
		public string CategoryID { get; set; }
		public bool Positive { get; set; }
		public DateTime EventTime { get; set; }
		public Tuple<double, double> LatLng { get; set; }
		public string Location { get; set; }
	}
}
