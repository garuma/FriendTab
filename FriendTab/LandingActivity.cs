using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Mail;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Provider;

using Parse;

namespace FriendTab
{
	[Activity (Label = "FriendTab", MainLauncher = true, Theme = "@android:style/Theme.Black.NoTitleBar",
	           ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait,
	           ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation)]
	public class LandingActivity : Activity
	{
		const int SignUpCode = 100;
		UserProfile profile;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			DensityExtensions.Initialize (this);

			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Main);
			var signInBtn = FindViewById<Button> (Resource.Id.SignInButton);
			var signUpBtn = FindViewById<Button> (Resource.Id.SignUpButton);
			var userEntry = FindViewById<EditText> (Resource.Id.EmailEntry);
			var passwordEntry = FindViewById<EditText> (Resource.Id.PasswordEntry);

			/* If the user is already logged in, we show a blank landing page
			 * (as there is a bit of delay when acquiring the TabPerson
			 * so this activity content is still shown).
			 */
			if (ParseUser.CurrentUser != null) {
				signInBtn.Visibility = signUpBtn.Visibility = userEntry.Visibility = passwordEntry.Visibility = ViewStates.Invisible;
				ParseUser.CurrentUser.FetchAsync ();
				LaunchApp (this, ParseUser.CurrentUser, null);
			}

			profile = UserProfile.Instantiate (this);

			SignupTimer timer = null;
			userEntry.AfterTextChanged += (sender, e) => {
				var login = userEntry.Text;
				if (string.IsNullOrEmpty (login))
					return;
				if (timer != null)
					timer.Cancel ();
				timer = new SignupTimer (1000, 1000, async () => {
					var disponibility = await CheckLoginDisponibility (login);
					if (userEntry.Text == login)
						signUpBtn.Enabled = disponibility;
				});
				timer.Start ();
			};
			var initialEmail = profile.PrimayAddress ?? (profile.Emails == null ? null : profile.Emails.FirstOrDefault ()) ?? null;
			if (!string.IsNullOrEmpty (initialEmail))
				userEntry.Text = initialEmail;
			if (!string.IsNullOrEmpty (userEntry.Text))
				passwordEntry.RequestFocus ();

			ProgressDialog spinDialog = new ProgressDialog (this) { Indeterminate = true };
			spinDialog.SetCancelable (false);

			Action<ParseUser, Task> callback = (user, task) => {
				if (task.IsFaulted) {
					Android.Util.Log.Debug ("Login",
					                        "User not recognized: {0}",
					                        task.Exception.Flatten ().ToString ());
					spinDialog.Dismiss ();
					var builder = new AlertDialog.Builder (this);
					builder.SetMessage (Resource.String.login_error);
					builder.SetPositiveButton ("OK", (a, b) => passwordEntry.Text = string.Empty);
					builder.Create ().Show ();

					return;
				}

				Android.Util.Log.Debug ("Login", "User {0} successfully logged. New? {1}",
				                        user.Username,
				                        user.IsNew);

				LaunchApp (this, user, spinDialog.Dismiss);
			};

			signInBtn.Click += (sender, e) => {
				string email;
				if (!TryExtractEmailFromRawInput (userEntry.Text, out email)) {
					var builder = new AlertDialog.Builder (this);
					builder.SetMessage (Resource.String.invalid_email);
					builder.SetPositiveButton ("OK", (a, b) => userEntry.Text = string.Empty);
					builder.Create ().Show ();
					return;
				}
				spinDialog.SetMessage ("Signing in...");
				spinDialog.Show ();

				ParseUser.LogInAsync (email, passwordEntry.Text).ContinueWith (t => callback (t.Result, t));
			};
			signUpBtn.Click += (sender, e) => {
				string email;
				if (!TryExtractEmailFromRawInput (userEntry.Text, out email)) {
					var builder = new AlertDialog.Builder (this);
					builder.SetMessage (Resource.String.invalid_email);
					builder.SetPositiveButton ("OK", (a, b) => userEntry.Text = string.Empty);
					builder.Create ().Show ();
					return;
				}

				spinDialog.SetMessage ("Signing up...");
				spinDialog.Show ();

				var user = new ParseUser () {
					Username = email,
					Email = email,
					Password = passwordEntry.Text
				};
				user.SignUpAsync ().ContinueWith (t => callback (user, t));
			};
		}

		internal async void LaunchApp (Activity ctx, ParseUser withUser, Action uiCallback)
		{
			var query = ParseObject.GetQuery ("Person")
				.Where (p => p.Get<ParseUser> ("parseUser") == withUser)
				.Include ("parseUser");

			ParseObject self = null;
			try {
				self = await query.FirstOrDefaultAsync ().ConfigureAwait (false);
			} catch (Exception e) {
				Android.Util.Log.Error ("Landing", "Error when trying to retrieve user. Normal if empty. {0}", e.ToString ());
			}

			TabPerson person = null;

			if (self == null) {
				// Check if our TabPerson wasn't created indirectly by another user
				query = ParseObject.GetQuery ("Person")
					.Where (p => p.Get<IList<string>> ("emails").Contains (TabPerson.MD5Hash (withUser.Email)));
				try {
					person = TabPerson.FromParse (await query.FirstAsync ().ConfigureAwait (false));
					person.AssociatedUser = withUser;
				} catch {
					// We use the main address email we got by parseUser
					// and we will fill the rest lazily later from profile
					// idem for the display name
					person = new TabPerson {
						AssociatedUser = withUser,
						Emails = new string[] { withUser.Email }
					};
				}
			} else {
				TabPerson.CurrentPerson = TabPerson.FromParse (self);
			}

			ctx.RunOnUiThread (async () => {
				// If the user was created, we setup a CursorLoader to query the information we need
				if (person != null) {
					person.DisplayName = string.IsNullOrEmpty (profile.DisplayName) ? MakeNameFromEmail (withUser.Email) : profile.DisplayName;
					person.Emails = profile.Emails.Union (person.Emails);
					await person.ToParse ().SaveAsync ();
					TabPerson.CurrentPerson = person;
				}

				if (uiCallback != null)
					uiCallback ();
				ctx.Finish ();
				ctx.StartActivity (typeof (MainActivity));
			});
		}

		string MakeNameFromEmail (string email)
		{
			try {
				var address = new MailAddress (email);
				return string.IsNullOrEmpty (address.DisplayName) ? address.User : address.DisplayName;
			} catch {
				return email;
			}
		}

		bool TryExtractEmailFromRawInput (string rawEmail, out string result)
		{
			result = null;
			try {
				var mail = new MailAddress (rawEmail);
				result = mail.Address;
				return true;
			} catch {
				return false;
			}
		}

		async Task<bool> CheckLoginDisponibility (string login)
		{
			var userQuery = ParseUser.Query.Where (u => u.Username == login);
			try {
				var c = await userQuery.CountAsync ().ConfigureAwait (false);
				return c == 0;
			} catch (Exception e) {
				Android.Util.Log.Debug ("loginDisponibility", e.ToString ());
				// In case of an error, we assume the name is not taken
				return true;
			}
		}
		
		protected override void OnActivityResult (int requestCode, Result resultCode, Intent data)
		{
			base.OnActivityResult (requestCode, resultCode, data);
			if (requestCode == SignUpCode) {
				if (resultCode == Result.Ok)
					Finish ();
			}
		}

		struct UserProfile
		{
			public string DisplayName { get; private set; }
			public string PrimayAddress { get; private set; }
			public HashSet<string> Emails { get; private set; }

			public static UserProfile Instantiate (Activity ctx)
			{
				var profile = new UserProfile ();
				profile.Emails = new HashSet<string> ();

				var projs = new[] {
					ContactsContract.Profile.InterfaceConsts.DisplayName
				};

				try {
					var cursor = ctx.ContentResolver.Query (ContactsContract.Profile.ContentUri,
					                                        projs,
					                                        null, null, null);
					if (cursor.MoveToFirst ()) {
						profile.DisplayName = cursor.GetString (cursor.GetColumnIndex (projs[0]));
						cursor.Close ();
						cursor.Dispose ();

						var uri = Android.Net.Uri.WithAppendedPath (ContactsContract.Profile.ContentUri,
						                                            ContactsContract.Contacts.Data.ContentDirectory);
						projs = new[] {
							ContactsContract.CommonDataKinds.Email.Address,
							ContactsContract.CommonDataKinds.Email.InterfaceConsts.IsPrimary
						};
						// Get emails
						var emailCursor = ctx.ContentResolver.Query (uri,
						                                             projs,
						                                             ContactsContract.Contacts.Data.InterfaceConsts.Mimetype + " = ?",
						                                             new[] { ContactsContract.CommonDataKinds.Email.ContentItemType }, null);
						while (emailCursor.MoveToNext ()) {
							var email = emailCursor.GetString (emailCursor.GetColumnIndex (projs[0]));
							profile.Emails.Add (email);
							var isPrimaryColumn = emailCursor.GetColumnIndex (projs[1]);
							var isPrimary = isPrimaryColumn != -1 && emailCursor.GetInt (isPrimaryColumn) > 0;
							if (isPrimary)
								profile.PrimayAddress = email;
						}
						if (profile.PrimayAddress == null && profile.Emails.Count == 1)
							profile.PrimayAddress = profile.Emails.First ();
						emailCursor.Close ();
						emailCursor.Dispose ();
					}
				} catch (Exception e) {
					Android.Util.Log.Error ("ProfileFetcher", "Unable to read profile: {0}", e.ToString ());
				}

				return profile;
			}
		}
	}

	internal class SignupTimer : CountDownTimer
	{
		public event EventHandler Tick;
		Action finishAction;
		
		public SignupTimer (int milliEnd, int milliSlice, Action finishAction)
			: base (milliEnd, milliSlice)
		{
			this.finishAction = finishAction;
		}
		
		public override void OnTick (long millisUntilFinished)
		{
			if (Tick != null)
				Tick (this, EventArgs.Empty);
		}
		
		public override void OnFinish ()
		{
			if (finishAction != null)
				finishAction ();
		}
	}
}
