
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

using ParseLib;

namespace FriendTab
{
	[Activity (Label = "FriendTab", Theme = "@android:style/Theme.Holo.Light.NoActionBar")]		
	public class SignUpActivity : Activity
	{
		ParseUser registeredUser;

		string userName;
		string password;

		TextView informativeText;
		Button actionButton;
		Button secondActionButton;
		ProgressBar progress;

		bool isDead;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			userName = bundle.GetString ("Username");
			password = bundle.GetString ("Password");

			SetContentView (Resource.Layout.SignupPage);

			informativeText = FindViewById<TextView> (Resource.Id.InformativeText);
			actionButton = FindViewById<Button> (Resource.Id.ActionButton);
			secondActionButton = FindViewById<Button> (Resource.Id.ActionSecondButton);
			progress = FindViewById<ProgressBar> (Resource.Id.Spinner);
			actionButton.Click += CancelAction;
		}

		void CancelAction (object sender, EventArgs e)
		{
			if (registeredUser != null)
				registeredUser.DeleteInBackground ();
			SetResult (Result.Canceled);
			Finish ();
		}

		protected override void OnStart ()
		{
			var user = new ParseUser () {
				Username = userName,
				Email = userName
			};
			user.SetPassword (password);
			user.SignUpInBackground (new TabSignUpCallback (user, SignUpCallback));
		}

		protected override void OnDestroy ()
		{
			isDead = true;
			base.OnDestroy ();
		}

		void SignUpCallback (ParseUser user, ParseException e)
		{
			if (isDead) {
				user.DeleteInBackground ();
				return;
			}

			progress.Visibility = ViewStates.Invisible;

			if (user != null && e == null) {
				informativeText.Text = "Verification email sent. Waiting for confirmation...";
				RefreshLoop (user);
			} else {
				SetResult (Result.Canceled);
				actionButton.Text = "< Go Back";
				informativeText.Text = "Error: the username likely exists already";
			}
		}

		void RefreshLoop (ParseUser user)
		{
			if (user.Has ("emailVerified")) {
				SetResult (Result.Ok);
				informativeText.Text = "Setting up...";
				LandingActivity.LaunchApp (this, user, null);
				return;
			} else {
				registeredUser.RefreshInBackground (new TabRefreshCallback ((o, e) => {
					RefreshLoop (user);
				}));
			}
		}
	}

	class TabSignUpCallback : SignUpCallback
	{
		Action<ParseUser, ParseException> action;
		ParseUser user;
		
		public TabSignUpCallback (ParseUser user, Action<ParseUser, ParseException> action)
		{
			this.user = user;
			this.action = action;
		}
		
		public override void Done (ParseException p0)
		{
			action (user, p0);
		}
	}

	class TabRefreshCallback : RefreshCallback
	{
		Action<ParseObject, ParseException> action;

		public TabRefreshCallback (Action<ParseObject, ParseException> action)
		{
			this.action = action;
		}

		public override void Done (ParseObject pobject, ParseException pexception)
		{
			action (pobject, pexception);
		}
	}
}

