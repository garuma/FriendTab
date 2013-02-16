using System;

using ParseLib;

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
				return obj.GetString ("lookupID");
			}
			set {
				obj.Put ("lookupID", value);
			}
		}

		public string ContactID {
			get {
				return obj.GetString ("contactID");
			}
			set {
				obj.Put ("contactID", value);
			}
		}

		public void Update (Action postUpdateCallback)
		{
			if (!obj.Has ("fromPerson"))
				obj.Put ("fromPerson", FromPerson.ToParse ());
			if (!obj.Has ("who"))
				obj.Put ("who", Who.ToParse ());

			try {
				obj.Save ();
				postUpdateCallback ();
			} catch {
				obj.SaveEventually (new TabSaveCallback (e => postUpdateCallback ()));
			}
		}
	}
}

