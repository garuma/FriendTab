using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using ParseLib;

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
				Originator = TabPerson.FromParse (parseObject.GetParseObject ("originator")),
				Recipient = TabPerson.FromParse (parseObject.GetParseObject ("recipient")),
				Direction = (TabDirection)parseObject.GetInt ("direction"),
				Type = TabType.FromParse (parseObject.GetParseObject ("tabType")),
				LatLng = GeopointToTuple (parseObject.GetParseGeoPoint ("location")),
				LocationDesc = parseObject.GetString ("locationDesc"),
				Time = JavaDateToDateTime (parseObject.GetDate ("time")),
				parseData = parseObject
			};
		}

		public static Task ExpandParseObject (ParseObject obj)
		{
			if (obj == null)
				throw new ArgumentNullException ("obj");

			var objs = new ParseObject[] {
				obj.GetParseObject ("originator"),
				obj.GetParseObject ("recipient"),
				obj.GetParseObject ("tabType"),
			};

			return Task.Factory.StartNew (() => {
				foreach (var o in objs)
					o.FetchIfNeeded ();
			});
		}

		public static ParseQuery CreateTabQuery (ParseObject self, ParseObject p, ParseObject tabType, int way)
		{
			var query = ParseQuery.Or (new ParseQuery[] {
				new ParseQuery ("Tab")
					.WhereEqualTo ("originator", p)
					.WhereEqualTo ("recipient", self)
					.WhereEqualTo ("direction", way > 0 ? (int)TabDirection.Receiving : (int)TabDirection.Giving),
				new ParseQuery ("Tab")
					.WhereEqualTo ("recipient", p)
					.WhereEqualTo ("originator", self)
					.WhereEqualTo ("direction", way > 0 ? (int)TabDirection.Giving : (int)TabDirection.Receiving),
			});
			query.WhereEqualTo ("tabType", tabType);

			return query;
		}

		public static ParseQuery CreateTabListQuery (ParseObject self, ParseObject p)
		{
			var query = ParseQuery.Or (new ParseQuery[] {
				new ParseQuery ("Tab")
					.WhereEqualTo ("originator", p)
					.WhereEqualTo ("recipient", self),
				new ParseQuery ("Tab")
					.WhereEqualTo ("recipient", p)
					.WhereEqualTo ("originator", self)
			});
			// Include the field we need
			query.Include ("originator");
			query.Include ("recipient");
			query.Include ("tabType");
			
			return query;
		}

		public ParseObject ToParse ()
		{
			if (parseData != null)
				return parseData;

			var obj = new ParseObject ("Tab");
			obj.Put ("originator", Originator.ToParse ());
			obj.Put ("recipient", Recipient.ToParse ());
			obj.Put ("direction", (int)Direction);
			obj.Put ("tabType", Type.ToParse ());
			obj.Put ("location", new ParseGeoPoint (LatLng.Item1, LatLng.Item2));
			obj.Put ("locationDesc", LocationDesc);
			obj.Put ("time", DateTimeToJavaDate (Time));
			parseData = obj;

			return obj;
		}

		static Tuple<double, double> GeopointToTuple (ParseGeoPoint geoPoint)
		{
			return Tuple.Create (geoPoint.Latitude, geoPoint.Longitude);
		}

		static DateTime JavaDateToDateTime (Java.Util.Date javaDate)
		{
			return new DateTime (1970, 01, 01, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds (javaDate.Time).ToLocalTime ();
		}

		static Java.Util.Date DateTimeToJavaDate (DateTime dateTime)
		{
			var ms = (dateTime.ToUniversalTime () - (new DateTime (1970, 01, 01, 0, 0, 0, DateTimeKind.Utc))).TotalMilliseconds;
			return new Java.Util.Date ((long)ms);
		}

		// This is internally used to see if the object was just fetched or not
		internal bool Consummed {
			get;
			set;
		}
	}
}

