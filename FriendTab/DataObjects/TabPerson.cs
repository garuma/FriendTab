using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography;

using Parse;

using Android.Util;

namespace FriendTab
{
	// This is our DTO of a user
	public class TabPerson : IEquatable<TabPerson>
	{
		static HashAlgorithm hasher = MD5.Create ();
		static Dictionary<string, AndroidPerson> temporaryAndroidPersons = new Dictionary<string, AndroidPerson> ();

		ParseObject parseData;
		AndroidPerson androidPerson;

		//IEnumerable<TabType> preferredTabTypes;

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
				return AssociatedUser.ContainsKey ("emailVerified") && AssociatedUser.Get<bool> ("emailVerified");
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
				DisplayName = parseUser.Get<string> ("displayName"),
				Emails = parseUser.Get<IList<string>> ("emails").ToArray (),
				AssociatedUser = parseUser.GetOrNull<ParseUser> ("parseUser"),
				parseData = parseUser,
			};
		}

		/*public async Task<IEnumerable<TabType>> GetPreferredTabTypes ()
		{
			if (parseData == null)
				throw new NoParseDataException ("TabPerson");

			if (preferredTabTypes != null)
				return preferredTabTypes;

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
		}*/

		public async Task AddEmail (string email)
		{
			if (parseData == null)
				throw new NoParseDataException ("TabPerson");

			parseData.AddUniqueToList ("emails", MD5Hash (email));
			await parseData.SaveAsync ().ConfigureAwait (false);
		}

		public async Task<ParseUser> FetchUser ()
		{
			if (parseData == null)
				throw new InvalidOperationException ("No parseData available to query the user");
			var user = parseData.Get<ParseUser> ("parseUser");
			await user.FetchIfNeededAsync ();
			AssociatedUser = user;
			return user;
		}

		// In this method, we md5 the provided emails before sending them to parse
		// so to not be evil (we don't care about them after all)
		public ParseObject ToParse ()
		{
			if (parseData != null)
				return parseData;
			var parseObject = new ParseObject ("Person");
			parseObject["displayName"] = DisplayName;
			parseObject.AddRangeUniqueToList ("emails", Emails.Select (MD5Hash).ToArray ());
			if (AssociatedUser != null)
				parseObject["parseUser"] = AssociatedUser;
			parseData = parseObject;

			return parseObject;
		}

		public async Task<AndroidPerson> GetAndroidPersonDetail ()
		{
			if (androidPerson != null)
				return androidPerson;
			if (parseData == null)
				throw new NoParseDataException ("TabPerson");

			if (temporaryAndroidPersons.TryGetValue (parseData.ObjectId, out androidPerson))
				return androidPerson;

			var query = GetAndroidPersonQuery ();
			var result = await query.FirstOrDefaultAsync ();
			androidPerson = new AndroidPerson (result);
			if (result == null)
				temporaryAndroidPersons[parseData.ObjectId] = androidPerson;

			return androidPerson;
		}

		ParseQuery<ParseObject> GetAndroidPersonQuery ()
		{
			return ParseObject.GetQuery ("AndroidPerson")
				.Where (ap => ap.Get<ParseUser> ("fromPerson") == TabPerson.CurrentPerson.ToParse ())
				.Where (ap => ap.Get<ParseObject> ("who") == parseData);
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
}

