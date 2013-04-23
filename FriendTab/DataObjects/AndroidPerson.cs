using System;
using System.Threading.Tasks;

using Parse;

namespace FriendTab
{
	// Represent for a given Person the contact id/lookup id corresponding to the CurrentPerson address book
	public class AndroidPerson
	{
		ParseObject obj;

		internal AndroidPerson (ParseObject obj)
		{
			this.obj = obj ?? new ParseObject ("AndroidPerson");
		}

		public TabPerson FromPerson { get; internal set; }
		public TabPerson Who { get; internal set; }

		public string LookupID {
			get {
				return obj.GetOrNull<string> ("lookupID");
			}
			set {
				obj["lookupID"] = value;
			}
		}

		public string ContactID {
			get {
				return obj.GetOrNull<string> ("contactID");
			}
			set {
				obj["contactID"] = value;
			}
		}

		public async void Update ()
		{
			if (!obj.ContainsKey ("fromPerson"))
				obj["fromPerson"] = FromPerson.ToParse ();
			if (!obj.ContainsKey ("who"))
				obj["who"] = Who.ToParse ();

			do {
				try {
					await obj.SaveAsync ().ConfigureAwait (false);
					return;
				} catch (Exception e) {
					Android.Util.Log.Debug ("AndroidPerson", e.ToString ());
				}
			} while (true);
		}
	}
}

