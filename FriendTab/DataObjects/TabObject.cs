using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Parse;

namespace FriendTab
{
	public enum TabDirection {
		Giving = 0,
		Receiving = 1
	}

	public class TabObject
	{
		ParseObject parseData;

		// The originator is the person that created the transaction
		public TabPerson Originator { get; set; }
		// The recipient is the other person that the originator included in the transaction
		public TabPerson Recipient { get; set; }

		public TabDirection Direction { get; set; }
		public TabType Type { get; set; }
		public Tuple<double, double> LatLng { get; set; }
		public string LocationDesc { get; set; }
		public DateTime Time { get; set; }
		public string Id { get { return parseData.ObjectId; } }

		public static TabObject FromParse (ParseObject parseObject)
		{
			return new TabObject {
				Originator = TabPerson.FromParse (parseObject.Get<ParseObject> ("originator")),
				Recipient = TabPerson.FromParse (parseObject.Get<ParseObject> ("recipient")),
				Direction = (TabDirection)parseObject.Get<int> ("direction"),
				Type = TabType.FromParse (parseObject.Get<ParseObject> ("tabType")),
				LatLng = GeopointToTuple (parseObject.Get<ParseGeoPoint> ("location")),
				LocationDesc = parseObject.Get<string> ("locationDesc"),
				Time = parseObject.Get<DateTime> ("time"),
				parseData = parseObject
			};
		}

		public static async Task ExpandParseObject (ParseObject obj)
		{
			if (obj == null)
				throw new ArgumentNullException ("obj");

			var objs = new ParseObject[] {
				obj.Get<ParseObject> ("originator"),
				obj.Get<ParseObject> ("recipient"),
				obj.Get<ParseObject> ("tabType"),
			};
			foreach (var o in objs)
				await o.FetchIfNeededAsync ().ConfigureAwait (false);
		}

		public static ParseQuery<ParseObject> CreateTabListQuery (ParseObject self, ParseObject p)
		{
			var query = ParseQuery<ParseObject>.Or (new [] {
				   ParseObject.GetQuery ("Tab")
					    .Where (t => t["originator"] == p)
						.Where (t => t["recipient"] == self),
				   ParseObject.GetQuery ("Tab")
						.Where (t => t["recipient"] == p)
			            .Where (t => t["originator"] == self)
			    })
				.Include ("originator")
				.Include ("recipient")
				.Include ("tabType");
			
			return query;
		}

		public static ParseQuery<ParseObject> CreateTabActivityListQuery (int skip = 0, int limit = 10)
		{
			var current = TabPerson.CurrentPerson.ToParse ();
			var query = ParseQuery<ParseObject>.Or (new [] {
				  ParseObject.GetQuery ("Tab").Where (t => t.Get<ParseObject> ("originator") == current),
				  ParseObject.GetQuery ("Tab").Where (t => t.Get<ParseObject> ("recipient") == current)
			    })
				.Limit (limit)
				.OrderByDescending ("time");

			query = query.Include ("originator").Include ("recipient").Include ("tabType");

			if (skip > 0)
				query = query.Skip (skip);

			return query;
		}

		public ParseObject ToParse ()
		{
			if (parseData != null)
				return parseData;

			var obj = new ParseObject ("Tab");
			obj["originator"] = Originator.ToParse ();
			obj["recipient"] = Recipient.ToParse ();
			obj["direction"] = (int)Direction;
			obj["tabType"] = Type.ToParse ();
			obj["location"] = new ParseGeoPoint (LatLng.Item1, LatLng.Item2);
			obj["locationDesc"] = LocationDesc;
			obj["time"] = Time;
			parseData = obj;

			return obj;
		}

		static Tuple<double, double> GeopointToTuple (ParseGeoPoint geoPoint)
		{
			return Tuple.Create (geoPoint.Latitude, geoPoint.Longitude);
		}

		// This is internally used to see if the object was just fetched or not
		internal bool Consummed {
			get;
			set;
		}
	}
}

