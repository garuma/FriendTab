
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
using Android.Content.PM;

using Java.Lang;

using Parse;

namespace FriendTab
{
	[Activity (Label = "FriendTab", Theme = "@android:style/Theme.Holo.Light.DarkActionBar",
	           ScreenOrientation = ScreenOrientation.Portrait/* | ScreenOrientation.ReversePortrait*/,
	           ConfigurationChanges = ConfigChanges.Orientation)]
	public class MainActivity : Activity
	{
		Fragment newTabFragment;
		Fragment activityFragment;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			/* In case this activity is restored (i.e. hasn't been instanciated from
			 * our LandingActivity, we go there first to run the login logic run first.
			 * It's not pretty because we essentially incurs two activity change animations
			 * but it's easier for now.
			 */
			if (TabPerson.CurrentPerson == null) {
				Finish ();
				StartActivity (typeof (LandingActivity));
			}

			TabType.InitializeCache (this);

			ActionBar.NavigationMode = ActionBarNavigationMode.Tabs;

			// If the current user hasn't been verified we launch a timer to reset it
			if (!TabPerson.CurrentPerson.IsVerified) {
				new AlertDialog.Builder (this)
					.SetMessage ("You won't be able to fully use this application " +
					             "until you confirm your details via the email " +
					             "sent to " + TabPerson.CurrentPerson.LoginEmail)
					.SetPositiveButton ("OK", delegate {}).Show ();
				RefreshLoop (TabPerson.CurrentPerson);
			}

			// New Tab tab setup
			var tab = ActionBar.NewTab ().SetText ("Add");
			tab.TabSelected += (object sender, ActionBar.TabEventArgs e) => {
				if (newTabFragment == null) {
					newTabFragment = Fragment.Instantiate (this,
					                                       Class.FromType (typeof (ParticipantSelectionFragment)).Name);
					e.FragmentTransaction.Add (Android.Resource.Id.Content, newTabFragment, "new-tab");
				} else {
					e.FragmentTransaction.Attach (newTabFragment);
				}
			};
			tab.TabUnselected += (object sender, Android.App.ActionBar.TabEventArgs e) => {
				if (newTabFragment != null)
					e.FragmentTransaction.Detach (newTabFragment);
			};
			tab.TabReselected += (object sender, ActionBar.TabEventArgs e) => {
				if (newTabFragment != null)
					((ParticipantSelectionFragment)newTabFragment).Refresh ();
			};
			ActionBar.AddTab (tab);

			// Activity setup
			tab = ActionBar.NewTab ().SetText ("Activity");
			tab.TabSelected += (object sender, ActionBar.TabEventArgs e) => {
				if (activityFragment == null) {
					activityFragment = Fragment.Instantiate (this,
					                                         Class.FromType (typeof (ActivityFragment)).Name);
					activityFragment.SetHasOptionsMenu (true);
					e.FragmentTransaction.Add (Android.Resource.Id.Content, activityFragment, "activity");
				} else {
					e.FragmentTransaction.Attach (activityFragment);
				}
			};
			tab.TabUnselected += (object sender, Android.App.ActionBar.TabEventArgs e) => {
				if (activityFragment != null)
					e.FragmentTransaction.Detach (activityFragment);
			};
			tab.TabReselected += (object sender, ActionBar.TabEventArgs e) => {
				if (activityFragment != null)
					((ActivityFragment)activityFragment).Refresh ();
			};
			ActionBar.AddTab (tab);
		}

		void RefreshLoop (TabPerson person)
		{
			if (!person.IsVerified) {
				Action callback = async () => {
					await person.AssociatedUser.FetchAsync ();
					RefreshLoop (person);
				};
				var timer = new SignupTimer (20000, 20000, callback);
				timer.Start ();
			} else {
				var toast = Toast.MakeText (this, "Successfully registered", ToastLength.Long);
				toast.Show ();
			}
		}

		protected override void OnSaveInstanceState (Bundle outState)
		{
			base.OnSaveInstanceState (outState);
			var selectedTab = ActionBar.SelectedNavigationIndex;
			outState.PutInt ("selectedTab", selectedTab);
		}

		protected override void OnRestoreInstanceState (Bundle savedInstanceState)
		{
			base.OnRestoreInstanceState (savedInstanceState);
			if (savedInstanceState.ContainsKey ("selectedTab"))
				ActionBar.SetSelectedNavigationItem (savedInstanceState.GetInt ("selectedTab"));
		}
	}
}
