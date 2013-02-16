using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace FriendTab
{
	public class AskLocationDialog : DialogFragment
	{
		EditText entry;
		ArrayAdapter<string> adapter;
		Locator locator;
		string hintAutoLocation;

		TaskCompletionSource<string> locationName = new TaskCompletionSource<string> ();

		public Task<string> LocationName {
			get {
				return locationName.Task;
			}
		}

		public void FillInLocation (string locationDesc)
		{
			locationName.TrySetResult (locationDesc);
		}

		public AskLocationDialog (Locator locator)
		{
			this.locator = locator;
		}

		public void FilterWithLocation (Tuple<double, double> location)
		{
			TabPlace.GetPlacesByLocation (location).ContinueWith (t => Activity.RunOnUiThread (() => {
				var progress = Dialog.FindViewById<ProgressBar> (Resource.Id.LocationProgress);
				progress.Visibility = ViewStates.Gone;
				if (!t.Result.Any ()) {
					var noneText = Dialog.FindViewById<TextView> (Resource.Id.NoneText);
					noneText.Visibility = ViewStates.Visible;
				} else {
					var completion = Dialog.FindViewById<ListView> (Resource.Id.CompletionList);
					adapter = new ArrayAdapter<string> (Activity,
				                                    Android.Resource.Layout.SimpleListItemActivated1,
				                                    t.Result.Select (p => p.PlaceName).ToArray ());
					completion.Adapter = adapter;
				}
			}));
		}

		public override void OnCreate (Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);
			SetStyle (DialogFragmentStyle.Normal, Android.Resource.Style.ThemeHoloDialog);
		}

		public override Dialog OnCreateDialog (Bundle savedInstanceState)
		{
			var innerView = Activity.LayoutInflater.Inflate (Resource.Layout.AskLocationLayout, null);
			entry = innerView.FindViewById<EditText> (Resource.Id.LocationEntry);
			var completion = innerView.FindViewById<ListView> (Resource.Id.CompletionList);
			var progress = innerView.FindViewById<ProgressBar> (Resource.Id.LocationProgress);
			progress.Visibility = ViewStates.Visible;

			entry.AfterTextChanged += (sender, e) => {
				if (adapter != null)
					adapter.Filter.InvokeFilter (entry.Text);
			};
			completion.ItemClick += (sender, e) => {
				entry.Text = adapter.GetItem (e.Position);
				Android.Text.Selection.SetSelection (entry.EditableText, entry.Text.Length);
			};

			if (locator.LatestNamedLocation != null
			    && locator.LatestNamedLocation.Item1 + TimeSpan.FromMinutes (10) > DateTime.Now) {
				entry.Text = locator.LatestNamedLocation.Item2;
				Android.Text.Selection.SetSelection (entry.EditableText, entry.Text.Length);
			}

			if (!string.IsNullOrEmpty (locator.LastAutoNamedLocation)) {
				hintAutoLocation = locator.LastAutoNamedLocation;
				entry.Hint = hintAutoLocation;
			}

			var d = new AlertDialog.Builder (Activity)
				.SetTitle ("Set Current Location")
				.SetIcon (Android.Resource.Drawable.IcDialogMap)
				.SetPositiveButton ("Continue", (s, dce) => ProcessResult (entry.Text))
				.SetNegativeButton ("Cancel", (s, dce) => locationName.SetCanceled ())
				.SetInverseBackgroundForced (true)
				.SetCancelable (true)
				//.SetOnCancelListener (new FooListener { NegativeAction = () => locationName.SetCanceled () })
				.SetView (innerView)
				.Create ();

			return d;
		}

		void ProcessResult (string value)
		{
			var v = string.IsNullOrEmpty (value) ? (hintAutoLocation ?? "Somewhere") : value;
			locationName.SetResult (v);
		}

		class FooListener : Java.Lang.Object, IDialogInterfaceOnCancelListener
		{
			public Action NegativeAction { get; set; }

			public void OnCancel (IDialogInterface dialog)
			{
				NegativeAction ();
			}
		}
	}
}

