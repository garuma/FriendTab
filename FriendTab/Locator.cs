using System;
using System.Linq;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Android.Locations;

namespace FriendTab
{
	public class Locator : Java.Lang.Object, ILocationListener
	{
		const int TwoMinutes = 1000 * 60 * 2;
		bool isActivelySearching;
		Location lastKnownLocation;
		LocationManager locationManager;
		string currentProvider;
		Activity ctx;
		CallbackTimer timer;
		Geocoder geocoder;

		public Locator (Activity ctx)
		{
			this.ctx = ctx;
			locationManager = (LocationManager)ctx.GetSystemService (Context.LocationService);
			locationManager.RequestLocationUpdates (LocationManager.PassiveProvider, 5 * 60 * 1000, 2, this);
			if (Geocoder.IsPresent)
				geocoder = new Geocoder (ctx);
		}

		public void StartActiveLocationSearching ()
		{
			if (isActivelySearching) {
				if (timer != null) {
					timer.Cancel ();
					timer.Start ();
				}
				return;
			}
			isActivelySearching = true;

			currentProvider = locationManager.GetBestProvider (new Criteria {
				AltitudeRequired = false,
				BearingRequired = false,
				SpeedRequired = false,
				Accuracy = Accuracy.Low,
				CostAllowed = true
			}, true);

			var cachedLocation = locationManager.GetLastKnownLocation (currentProvider);
			if (IsBetterLocation (cachedLocation, lastKnownLocation))
				lastKnownLocation = cachedLocation;

			locationManager.RemoveUpdates (this);
			locationManager.RequestLocationUpdates (currentProvider, 100, 3, this);
			timer = new CallbackTimer (1000 * 30, () => GetLocationAndStopActiveSearching ());
			timer.Start ();
		}

		public Location GetLocationAndStopActiveSearching ()
		{
			if (!isActivelySearching)
				return lastKnownLocation;
			isActivelySearching = false;

			if (currentProvider != null) {
				locationManager.RemoveUpdates (this);
				// Re-enable the passive provider
				ctx.RunOnUiThread (() => locationManager.RequestLocationUpdates (LocationManager.PassiveProvider, 5 * 60 * 1000, 2, this));
				currentProvider = null;
			}

			return lastKnownLocation;
		}

		public Location LastKnownLocation {
			get {
				return lastKnownLocation;
			}
		}

		public Tuple<DateTime, string> LatestNamedLocation {
			get;
			private set;
		}

		public string LastAutoNamedLocation {
			get;
			private set;
		}

		public void RefreshNamedLocation (string name)
		{
			LatestNamedLocation = Tuple.Create (DateTime.Now, name);
		}

		public void OnLocationChanged (Location location)
		{
			if (IsBetterLocation (location, lastKnownLocation)) {
				lastKnownLocation = location;
				if (geocoder != null)
					SerialScheduler.Factory.StartNew (RefreshGeocodedLocationName);
			}
		}

		void RefreshGeocodedLocationName ()
		{
			var list = geocoder.GetFromLocation (lastKnownLocation.Latitude,
			                                     lastKnownLocation.Longitude,
			                                     1);
			if (list == null || list.Count == 0)
				return;

			var address = list [0];
			var options = new string[] { address.FeatureName, address.Thoroughfare, address.Locality, address.CountryName };
			LastAutoNamedLocation = options.FirstOrDefault (o => !string.IsNullOrEmpty (o));
		}

		public void OnStatusChanged (string provider, Availability status, Bundle extras) {}

		public void OnProviderEnabled (string provider) {}

		public void OnProviderDisabled (string provider) {}

		bool IsBetterLocation (Location location, Location currentBestLocation)
		{
			if (currentBestLocation == null)
				// A new location is always better than no location
				return true;

			// Check whether the new location fix is newer or older
			long timeDelta = location.Time - currentBestLocation.Time;
			bool isSignificantlyNewer = timeDelta > TwoMinutes;
			bool isSignificantlyOlder = timeDelta < -TwoMinutes;
			bool isNewer = timeDelta > 0;

			// If it's been more than two minutes since the current location, use the new location
			// because the user has likely moved
			if (isSignificantlyNewer)
				return true;
				// If the new location is more than two minutes older, it must be worse
			else if (isSignificantlyOlder)
				return false;

			// Check whether the new location fix is more or less accurate
			int accuracyDelta = (int) (location.Accuracy - currentBestLocation.Accuracy);
			bool isLessAccurate = accuracyDelta > 0;
			bool isMoreAccurate = accuracyDelta < 0;
			bool isSignificantlyLessAccurate = accuracyDelta > 200;

			// Check if the old and new location are from the same provider
			bool isFromSameProvider = IsSameProvider(location.Provider,
			                                         currentBestLocation.Provider);

			// Determine location quality using a combination of timeliness and accuracy
			if (isMoreAccurate)
				return true;
			else if (isNewer && !isLessAccurate)
				return true;
			else if (isNewer && !isSignificantlyLessAccurate && isFromSameProvider)
				return true;
			
			return false;
		}

		bool IsSameProvider(String provider1, String provider2)
		{
			if (provider1 == null)
				return provider2 == null;
			return provider1 == provider2;
		}

		class CallbackTimer : CountDownTimer
		{
			Action callback;

			public CallbackTimer (int milliFinish, Action callback) : base (milliFinish, milliFinish)
			{
				this.callback = callback;
			}

			public override void OnFinish ()
			{
				callback ();
			}

			public override void OnTick (long millisUntilFinished)
			{
			}
		}
	}
}

