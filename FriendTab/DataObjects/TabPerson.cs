using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography;

using ParseLib;

using Android.Util;

namespace FriendTab
{
	// This is our DTO of a user
	public class TabPerson : IEquatable<TabPerson>
	{
		static HashAlgorithm hasher = MD5.Create ();
		static Dictionary<string, AndroidPerson> temporaryAndroidPersons = new Dictionary<string, AndroidPerson> ();

		ParseObject parseData;
		TaskCompletionSource<IEnumerable<TabType>> preferredTabTypes;
		TaskCompletionSource<AndroidPerson> androidPerson;

		public string DisplayName { get; set; }
		public IEnumerable<string> Emails { get; set; }
		public ParseUser AssociatedUser { get; set; }

		public string ObjectID {
			get {
				return parseData == null ? null : parseData.ObjectId;
			}
		}

		public bool IsVerified {
			get {
				return AssociatedUser.Has ("emailVerified") && AssociatedUser.GetBoolean ("emailVerified");
			}
		}

		public string LoginEmail {
			get {
				return AssociatedUser.Email;
			}
		}

		public static TabPerson CurrentPerson {
			get;
			set;
		}

		public static TabPerson FromParse (ParseObject parseUser)
		{
			return new TabPerson {
				DisplayName = parseUser.GetString ("displayName"),
				Emails = parseUser.GetList ("emails").Cast<string> ().ToArray (),
				AssociatedUser = parseUser.GetParseUser ("parseUser"),
				parseData = parseUser,
			};
		}

		public Task<IEnumerable<TabType>> GetPreferredTabTypes ()
		{
			if (parseData == null)
				throw new NoParseDataException ("TabPerson");

			if (preferredTabTypes != null)
				return preferredTabTypes.Task;

			preferredTabTypes = new TaskCompletionSource<IEnumerable<TabType>> ();
			var relation = parseData.GetRelation ("preferredTabTypes");
			var query = relation.Query;
			query.SetCachePolicy (ParseQuery.CachePolicy.NetworkElseCache);
			query.FindInBackground (new TabFindCallback ((ps, e) => {
				if (e == null)
					preferredTabTypes.SetResult (ps.Select (TabType.FromParse));
				else {
					Log.Error ("TabPersonPrefType", e.ToString ());
					preferredTabTypes.SetException (e);
				}
			}));

			return preferredTabTypes.Task;
		}

		public void AddPreferredTabType (TabType type)
		{
			if (parseData == null)
				throw new NoParseDataException ("TabPerson");

			parseData.GetRelation ("preferredTabTypes").Add (type.ToParse ());
			parseData.SaveEventually ();
		}

		public void RemovePreferredTabType (TabType type)
		{
			if (parseData == null)
				throw new NoParseDataException ("TabPerson");

			parseData.GetRelation ("preferredTabTypes").Remove (type.ToParse ());
			parseData.SaveEventually ();
		}

		public void AddEmail (string email)
		{
			if (parseData == null)
				throw new NoParseDataException ("TabPerson");

			parseData.AddUnique ("emails", MD5Hash (email));
			parseData.SaveEventually ();
		}

		public Task<ParseUser> FetchUser ()
		{
			var tcs = new TaskCompletionSource<ParseUser> ();
			if (parseData == null)
				tcs.SetException (new InvalidOperationException ("No parseData available to query the user"));
			parseData.GetParseUser ("parseUser")
				.FetchIfNeededInBackground (new TabGetCallback ((o, e) => {
					if (e != null)
						tcs.SetException (e);
					else {
						tcs.SetResult ((ParseUser)o);
						AssociatedUser = (ParseUser)o;
					}
				}));

			return tcs.Task;
		}

		// In this method, we md5 the provided emails before sending them to parse
		// so to not be evil (we don't care about them after all)
		public ParseObject ToParse ()
		{
			if (parseData != null)
				return parseData;
			var parseObject = new ParseObject ("Person");
			parseObject.Put ("displayName", DisplayName);
			parseObject.AddAll ("emails", Emails.Select (MD5Hash).ToArray ());
			if (AssociatedUser != null)
				parseObject.Put ("parseUser", AssociatedUser);
			parseData = parseObject;

			return parseObject;
		}

		public Task<AndroidPerson> GetAndroidPersonDetail ()
		{
			if (androidPerson != null)
				return androidPerson.Task;
			if (parseData == null)
				throw new NoParseDataException ("TabPerson");

			androidPerson = new TaskCompletionSource<AndroidPerson> ();

			if (temporaryAndroidPersons.ContainsKey (parseData.ObjectId)) {
				androidPerson.TrySetResult (temporaryAndroidPersons[parseData.ObjectId]);
				return androidPerson.Task;
			}

			var query = GetAndroidPersonQuery ();
			query.GetFirstInBackground (new TabGetCallback ((o, e) => {
				var ap = new AndroidPerson (o);
				androidPerson.TrySetResult (ap);
				// Protect against faulty Parse cache
				if (o == null)
					temporaryAndroidPersons[parseData.ObjectId] = ap;
			}));

			return androidPerson.Task;
		}

		internal void ClearCachedAndroidPersonDetail ()
		{
			androidPerson = null;
			var query = GetAndroidPersonQuery ();
			query.ClearCachedResult ();
		}

		ParseQuery GetAndroidPersonQuery ()
		{
			var query = new ParseQuery ("AndroidPerson");
			query.SetCachePolicy (ParseQuery.CachePolicy.CacheElseNetwork);
			query.MaxCacheAge = (long)TimeSpan.FromDays (7).TotalMilliseconds;
			query.WhereEqualTo ("fromPerson", TabPerson.CurrentPerson.ToParse ());
			query.WhereEqualTo ("who", parseData);
			return query;
		}

		internal static string MD5Hash (string input)
		{
			var bytes = System.Text.Encoding.UTF8.GetBytes (input);
			return hasher.ComputeHash (bytes)
				.Select (b => String.Format ("{0:X2}", b))
				.Aggregate (string.Concat);
		}

		public bool Equals (TabPerson rhs)
		{
			return !object.ReferenceEquals (rhs, null) && 
				((ObjectID != null && ObjectID == rhs.ObjectID) || DisplayName == rhs.DisplayName);
		}

		public override int GetHashCode ()
		{
			return DisplayName.GetHashCode ();
		}

		public override bool Equals (object obj)
		{
			var other = obj as TabPerson;
			return other != null && Equals (other);
		}

		public static bool operator== (TabPerson p1, TabPerson p2)
		{
			return !(object.ReferenceEquals (p1, null) ^ object.ReferenceEquals (p2, null))
				&& (object.ReferenceEquals (p1, p2) || p1.Equals (p2));
		}

		public static bool operator!= (TabPerson p1, TabPerson p2)
		{
			return !(p1 == p2);
		}
	}

	class TabGetCallback : GetCallback
	{
		Action<ParseObject, ParseException> action;

		public TabGetCallback (Action<ParseObject, ParseException> action)
		{
			this.action = action;
		}

		public override void Done (ParseObject obj, ParseException e)
		{
			action (obj, e);
		}
	}
}

