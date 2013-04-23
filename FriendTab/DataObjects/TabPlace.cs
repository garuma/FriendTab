using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

using Parse;

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
			var geoPoint = obj.Get<ParseGeoPoint> ("latLng");
			return new TabPlace (obj) {
				PlaceName = obj.Get<string> ("placeName"),
				LatLng = Tuple.Create (geoPoint.Latitude, geoPoint.Longitude),
				Person = TabPerson.FromParse (obj.Get<ParseObject> ("person"))
			};
		}

		public ParseObject ToParse ()
		{
			if (parseObject != null)
				return parseObject;

			parseObject = new ParseObject ("Place");
			parseObject["placeName"] = PlaceName;
			parseObject["latLng"] = new ParseGeoPoint (LatLng.Item1, LatLng.Item2);
			parseObject["person"] = Person.ToParse ();

			return parseObject;
		}

		public static async Task<IEnumerable<TabPlace>> GetPlacesByLocation (Tuple<double, double> latLng, double radius = 10)
		{
			var query = ParseObject.GetQuery ("Place");
			query.WhereWithinDistance ("latLng",
			                           new ParseGeoPoint (latLng.Item1, latLng.Item2),
			                           ParseGeoDistance.FromKilometers (radius))
				.Include ("person")
				.Limit (10);

			var results = await query.FindAsync ().ConfigureAwait (false);
			return results.Select (FromParse).ToList ();
		}

		public static void RegisterPlace (string placeName, Tuple<double, double> latLng)
		{
			if (string.IsNullOrWhiteSpace (placeName) || localLocations.Contains (placeName))
				return;

			var query = ParseObject.GetQuery ("Place").Where (p => p.Get<string> ("placeName") == placeName);
			query.CountAsync ().ContinueWith (c => {
				if (c.Exception == null && c.Result == 0 && localLocations.Add (placeName)) {
					var place = new TabPlace (null) {
						PlaceName = placeName,
						LatLng = latLng,
						Person = TabPerson.CurrentPerson
					};
					place.ToParse ().SaveAsync ().Wait ();
				}
			});
		}
	}
}

