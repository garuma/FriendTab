using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

using ParseLib;

namespace FriendTab
{
	public class TabPlace
	{
		ParseObject parseObject;
		// Since some location might not have been sent yet, we use that collection
		// to check them on the local side
		static HashSet<string> localLocations = new HashSet<string> ();

		TabPlace (ParseObject parseObject)
		{
			this.parseObject = parseObject;
		}

		public string PlaceName {
			get;
			set;
		}

		public Tuple<double, double> LatLng {
			get;
			set;
		}

		public TabPerson Person {
			get;
			set;
		}

		static TabPlace FromParse (ParseObject obj)
		{
			var geoPoint = obj.GetParseGeoPoint ("latLng");
			return new TabPlace (obj) {
				PlaceName = obj.GetString ("placeName"),
				LatLng = Tuple.Create (geoPoint.Latitude, geoPoint.Longitude),
				Person = TabPerson.FromParse (obj.GetParseObject ("person"))
			};
		}

		public ParseObject ToParse ()
		{
			if (parseObject != null)
				return parseObject;

			parseObject = new ParseObject ("Place");
			parseObject.Put ("placeName", PlaceName);
			parseObject.Put ("latLng", new ParseGeoPoint (LatLng.Item1, LatLng.Item2));
			parseObject.Put ("person", Person.ToParse ());

			return parseObject;
		}

		public static Task<IEnumerable<TabPlace>> GetPlacesByLocation (Tuple<double, double> latLng, double radius = 10)
		{
			var tcs = new TaskCompletionSource<IEnumerable<TabPlace>> ();
			var query = new ParseQuery ("Place");
			query.WhereWithinKilometers ("latLng", new ParseGeoPoint (latLng.Item1, latLng.Item2), radius);
			query.Include ("person");
			query.Limit = 10;

			query.FindInBackground (new TabFindCallback ((os, e) => {
				if (e != null)
					tcs.SetException (e);
				else
					tcs.SetResult (os.Select (FromParse).ToList ());
			}));

			return tcs.Task;
		}

		public static void RegisterPlace (string placeName, Tuple<double, double> latLng)
		{
			if (string.IsNullOrWhiteSpace (placeName) || localLocations.Contains (placeName))
				return;

			var queryChecker = new ParseQuery ("Place");
			queryChecker.SetCachePolicy (ParseQuery.CachePolicy.NetworkOnly);
			queryChecker.WhereEqualTo ("placeName", placeName);
			queryChecker.CountInBackground (new TabCountCallback ((c, e) => {
				if (e == null && c == 0 && localLocations.Add (placeName)) {
					var place = new TabPlace (null) {
						PlaceName = placeName,
						LatLng = latLng,
						Person = TabPerson.CurrentPerson
					};
					place.ToParse ().SaveInBackground ();
				}
			}));
		}
	}
}

