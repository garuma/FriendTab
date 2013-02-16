using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Android.Graphics;

using ParseLib;

namespace FriendTab
{
	public class SelectedUserInfo
	{
		TaskCompletionSource<TabPerson> person;

		public string DisplayName { get; set; }
		public IEnumerable<string> Emails { get; set; }
		public string LookupID { get; set; }
		public string ContactID { get; set; }
		public TabPerson PersonCache { get; set; }
		public Bitmap AvatarBitmap { get; set; }

		public Task<TabPerson> ToPerson ()
		{
			if (person != null)
				return person.Task;

			person = new TaskCompletionSource<TabPerson> ();
			var recipientQuery = new ParseQuery ("Person");
			recipientQuery.SetCachePolicy (ParseQuery.CachePolicy.CacheElseNetwork);
			if (Emails.Any ())
				recipientQuery.WhereContainedIn ("emails", Emails.Select (TabPerson.MD5Hash).ToArray ());
			else
				recipientQuery.WhereEqualTo ("displayName", DisplayName);

			ParseObject recipient = null;
			TabPerson recipientPerson = null;
			recipientQuery.GetFirstInBackground (new TabGetCallback ((o, e) => {
				if (o != null) {
					recipient = o;
					recipientPerson = TabPerson.FromParse (recipient);
					// TODO: add the emails that are in Emails and not in recipientPerson.Emails
					person.SetResult (recipientPerson);
				} else {
					recipientPerson = new TabPerson {
						DisplayName = DisplayName,
						Emails = Emails,
					};
					recipient = recipientPerson.ToParse ();
					recipient.SaveInBackground (new TabSaveCallback (ex => {
						if (ex == null)
							person.SetResult (recipientPerson);
						else {
							Android.Util.Log.Error ("PersonCreator", ex.ToString ());
							person.SetException (ex);
						}
					}));
					recipientQuery.ClearCachedResult ();
				}
			}));

			return person.Task;
		}
	}

	public class TabSaveCallback : SaveCallback
	{
		Action<ParseException> callback;

		public TabSaveCallback (Action<ParseException> callback)
		{
			this.callback = callback;
		}

		public override void Done (ParseException e)
		{
			callback (e);
		}
	}
}
