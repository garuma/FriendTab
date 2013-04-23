using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Android.Graphics;

using Parse;

namespace FriendTab
{
	public class SelectedUserInfo
	{
		TabPerson person;

		public string DisplayName { get; set; }
		public IEnumerable<string> Emails { get; set; }
		public string LookupID { get; set; }
		public string ContactID { get; set; }
		public TabPerson PersonCache { get; set; }
		public Bitmap AvatarBitmap { get; set; }

		public async Task<TabPerson> ToPerson ()
		{
			if (person != null)
				return person;

			var recipientQuery = ParseObject.GetQuery ("Person");
			if (Emails.Any ())
				recipientQuery = recipientQuery.WhereContainedIn ("emails", Emails.Select (TabPerson.MD5Hash).ToArray ());
			else
				recipientQuery = recipientQuery.Where (p => p.Get<string> ("displayName") == DisplayName);

			try {
				var recipient = await recipientQuery.FirstOrDefaultAsync ().ConfigureAwait (false);
				if (recipient != null)
					person = TabPerson.FromParse (recipient);
			} catch (Exception e) {
				Android.Util.Log.Debug ("SelectedUserInfo", e.ToString ());
			};

			// In case of an error
			if (person == null) {
				var recipientPerson = new TabPerson {
					DisplayName = DisplayName,
					Emails = Emails,
				};
				var recipient = recipientPerson.ToParse ();
				await recipient.SaveAsync ();
				person = recipientPerson;
			}

			return person;
		}
	}
}
