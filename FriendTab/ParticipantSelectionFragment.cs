using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Android.Provider;
using Android.Graphics;
using Android.Locations;
using Android.Views.Animations;

using Parse;

namespace FriendTab
{
	public class ParticipantSelectionFragment : Fragment
	{
		readonly static Dictionary<string, CategoryBadge> BadgeInstances = new Dictionary<string, CategoryBadge> ();

		View userDropZone;
		TapUserBadge userBadge;
		Locator locator;
		ViewGroup categoryPlaceholder;
		ImageView selfFace;
		ImageView otherFace;
		LinearLayout faceLayout;
		FrameLayout selfFaceLayout;
		FrameLayout otherFaceLayout;
		KarmaMeter karmaBar;
		Arrow arrowWay;
		ProgressBar statsSpinner;
		FlashBarController flashBarCtrl;
		LinearLayout whatEntry;
		LinearLayout whatLayout;

		SelectedUserInfo lastSelectedUser;
		Bitmap contactPicture;

		public ParticipantSelectionFragment ()
		{
		}

		public override void OnCreate (Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);
		}

		public override void OnAttach (Activity activity)
		{
			base.OnAttach (activity);
			contactPicture = SvgUtils.GetBitmapFromSvgRes (activity.Resources,
			                                               Resource.Drawable.ic_contact_picture,
			                                               90.ToPixels (),
			                                               90.ToPixels ());
			locator = new Locator (activity);
		}

		public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
		{
			var view = inflater.Inflate (Resource.Layout.ParticipantSelectionLayout, container, false);

			view.FindViewById<LinearLayout> (Resource.Id.animatedLayout).LayoutTransition = new Android.Animation.LayoutTransition ();

			selfFace = view.FindViewById<ImageView> (Resource.Id.selfFace);
			otherFace = view.FindViewById<ImageView> (Resource.Id.otherFace);
			faceLayout = view.FindViewById<LinearLayout> (Resource.Id.userFacesLayout);
			selfFaceLayout = view.FindViewById<FrameLayout> (Resource.Id.selfFaceLayout);
			otherFaceLayout = view.FindViewById<FrameLayout> (Resource.Id.otherFaceLayout);
			selfFaceLayout.Drag += HandleDrag;
			otherFaceLayout.Drag += HandleDrag;
			arrowWay = view.FindViewById<Arrow> (Resource.Id.arrowWay);
			statsSpinner = view.FindViewById<ProgressBar> (Resource.Id.StatsLoadSpinner);
			whatEntry = view.FindViewById<LinearLayout> (Resource.Id.whatEntry);
			whatLayout = view.FindViewById<LinearLayout> (Resource.Id.whatLayout);

			userDropZone = view.FindViewById<LinearLayout> (Resource.Id.whoLayout);

			var inset = view.FindViewById<InsetTextView> (Resource.Id.testInset);
			inset.Text = "Drag Us";
			karmaBar = view.FindViewById<KarmaMeter> (Resource.Id.karmaBar);
			flashBarCtrl = new FlashBarController (view.FindViewById (Resource.Id.flashbar));

			Task.Factory.StartNew (() => {
				var contactData = ContactsContract.Profile.ContentUri;
				var stream = ContactsContract.Contacts.OpenContactPhotoInputStream (Activity.ContentResolver,
				                                                                    contactData,
				                                                                    false);
				if (stream != null) {
					var options = new BitmapFactory.Options {
						InPreferQualityOverSpeed = true,
						InPreferredConfig = Bitmap.Config.Argb8888,
					};
					var bmp = BitmapFactory.DecodeStream (stream, null, options);
					Activity.RunOnUiThread (() => selfFace.SetImageBitmap (bmp));
				}
			});

			userBadge = (TapUserBadge)FragmentManager.FindFragmentById (Resource.Id.userBadge);
			CategoryBadge.StartDragValidate = () => userBadge.SelectedUser != null;
			userBadge.SelectedUserChanged += HandleSelectedUserChanged;

			categoryPlaceholder = view.FindViewById<LinearLayout> (Resource.Id.categoryPlaceholder);
			TabTypes.GetTabTypes ().ContinueWith (types => {
				var badges = types.Result
					.Select (t => Tuple.Create (t, SvgUtils.GetBitmapFromSvgString (t.SvgImage,
					                                                                40.ToPixels (),
					                                                                40.ToPixels ())))
					.ToArray ();
				Activity.RunOnUiThread (() => AddTabTypeBadges (badges, inflater));
			});

			if (lastSelectedUser != null)
				userBadge.RestoreSelectedUserInfo (lastSelectedUser);

			return view;
		}

		void AddTabTypeBadges (IEnumerable<Tuple<TabType, Bitmap>> badges, LayoutInflater inflater)
		{
			foreach (var b in badges) {
				var type = b.Item1;
				var bmp = b.Item2;

				var layout = new ViewGroup.LayoutParams (ViewGroup.LayoutParams.WrapContent,
				                                         ViewGroup.LayoutParams.WrapContent);
				var badge = new CategoryBadge (inflater.Context) {
					ItemName = type.Name,
					IconDrawable = bmp
				};
				categoryPlaceholder.AddView (badge, categoryPlaceholder.ChildCount - 1, layout);
				BadgeInstances[type.Name] = badge;
			}
		}

		public override void OnDestroyView ()
		{
			if (!Activity.IsFinishing && userBadge != null) {
				lastSelectedUser = userBadge.SelectedUser;
				try {
					var transaction = FragmentManager.BeginTransaction ();
					transaction.Remove (userBadge);
					transaction.Commit ();
				} catch {
				}
			}

			base.OnDestroyView ();
		}

		async void HandleSelectedUserChanged (object sender, EventArgs evt)
		{
			if (whatEntry.Visibility == ViewStates.Invisible) {
				whatEntry.Visibility = ViewStates.Visible;
				whatLayout.Visibility = ViewStates.Visible;
				whatEntry.StartAnimation (new AlphaAnimation (0, 1) { Duration = 1000 });
				whatLayout.StartAnimation (new AlphaAnimation (0, 1) { Duration = 1000 });
			}

			var selectedContact = userBadge.SelectedUser;
			if (selectedContact.AvatarBitmap != null)
				otherFace.SetImageBitmap (selectedContact.AvatarBitmap);
			else
				otherFace.SetImageBitmap (contactPicture);

			// Reset badge count;
			foreach (var c in BadgeInstances)
				c.Value.Count = 0;

			statsSpinner.Visibility = ViewStates.Visible;

			var person = await selectedContact.ToPerson ();
			selectedContact.PersonCache = person;

			UpdateTabStatistics (person, selectedContact);

			var ap = await person.GetAndroidPersonDetail ();
			ap.ContactID = selectedContact.ContactID;
			ap.LookupID = selectedContact.LookupID;
			ap.FromPerson = TabPerson.CurrentPerson;
			ap.Who = person;
			ap.Update ();
		}

		void HandleDrag (object sender, Android.Views.View.DragEventArgs e)
		{
			var evt = e.Event;
			switch (evt.Action) {
			case DragAction.Started:
				locator.StartActiveLocationSearching ();
				e.Handled = true;
				var parms = faceLayout.LayoutParameters as LinearLayout.LayoutParams;
				parms = new LinearLayout.LayoutParams (parms);
				parms.Height = ViewGroup.LayoutParams.WrapContent;
				parms.Gravity = GravityFlags.Center;
				faceLayout.LayoutParameters = parms;
				userDropZone.Visibility = ViewStates.Gone;
				break;
			case DragAction.Entered:
			case DragAction.Exited:
				var frame = sender as FrameLayout;
				if (frame != null)
					frame.SetBackgroundResource (evt.Action == DragAction.Entered ? Resource.Drawable.boxdrag : Resource.Drawable.box);
				if (evt.Action == DragAction.Entered)
					arrowWay.SetOrientation (frame == selfFaceLayout ? ArrowOrientation.Right : ArrowOrientation.Left);
				else
					arrowWay.Disappear ();
				break;
			case DragAction.Drop:
				e.Handled = true;
				var data = e.Event.ClipData.GetItemAt (0).Text;
				if (sender == otherFaceLayout)
					RegisterNewTab (null, userBadge.SelectedUser, data);
				else
					RegisterNewTab (userBadge.SelectedUser, null, data);
				break;
			case DragAction.Ended:
				e.Handled = true;
				userDropZone.Visibility = ViewStates.Visible;
				parms = faceLayout.LayoutParameters as LinearLayout.LayoutParams;
				parms = new LinearLayout.LayoutParams (parms);
				parms.Height = 1;
				parms.Gravity = GravityFlags.Center;
				faceLayout.LayoutParameters = parms;
				selfFaceLayout.SetBackgroundResource (Resource.Drawable.box);
				otherFaceLayout.SetBackgroundResource (Resource.Drawable.box);
				arrowWay.Disappear ();

				break;
			}
		}

		async void RegisterNewTab (SelectedUserInfo originator, SelectedUserInfo recipient, string tabTypeName, string locationDesc = null)
		{
			var selectedPerson = await (originator ?? recipient).ToPerson ();
			var tabType = (await TabTypes.GetTabTypes ())
				.FirstOrDefault (t => t.Name.Equals (tabTypeName, StringComparison.OrdinalIgnoreCase));

			var dialog = new AskLocationDialog (locator);
			if (!string.IsNullOrEmpty (locationDesc))
				dialog.FillInLocation (locationDesc);

			try {
				locationDesc = await dialog.LocationName;
			} catch (Exception e) {
				Android.Util.Log.Debug ("RegisterLocation", e.ToString ());
				return;
			}

			var dir = originator == null ? TabDirection.Giving : TabDirection.Receiving;
			var location = locator.GetLocationAndStopActiveSearching ();
			locator.RefreshNamedLocation (locationDesc);
			TabPlace.RegisterPlace (locationDesc,
			                        Tuple.Create (location.Latitude, location.Longitude));

			var tab = new TabObject {
				Originator = TabPerson.CurrentPerson,
				Recipient = selectedPerson,
				Type = tabType,
				Direction = dir,
				LatLng = Tuple.Create (location.Latitude, location.Longitude),
				LocationDesc = locationDesc,
				Time = DateTime.Now
			};
			Action postSave = () => {
				UpdateTabStatistics (selectedPerson, originator ?? recipient, true);
				Activity.RunOnUiThread (() => PostedNewTab (tab));
			};
			var po = tab.ToParse ();
			while (true) {
				try {
					await po.SaveAsync ();
					postSave ();
					break;
				} catch (Exception e) {
					Log.Error ("TabSaver", e.ToString ());
				}
				await flashBarCtrl.ShowBarAsync (withMessageId: Resource.String.flashbar_tab_error);
			}

			if (locationDesc == null) {
				var lastLocation = locator.LastKnownLocation;
				if (lastLocation != null)
					dialog.FilterWithLocation (Tuple.Create (lastLocation.Latitude, lastLocation.Longitude));
				dialog.Show (FragmentManager, "location-asker");
			}
		}

		void PostedNewTab (TabObject tab)
		{
			var text = string.Format ("Added {0} {1} {2}",
			                          tab.Type.Name,
			                          tab.Direction == TabDirection.Giving ? "to" : "from",
			                          tab.Recipient.DisplayName);
			Toast.MakeText (Activity, text, ToastLength.Short).Show ();
			statsSpinner.Visibility = ViewStates.Visible;
			var activityFragment = FragmentManager.FindFragmentByTag ("activity") as ActivityFragment;
			if (activityFragment != null)
				activityFragment.AddLocalTabObject (tab);
		}

		async void UpdateTabStatistics (TabPerson other, SelectedUserInfo selectedContact, bool force = false)
		{
			var p = other.ToParse ();
			var self = TabPerson.CurrentPerson.ToParse ();

			var query = TabObject.CreateTabListQuery (self, p);
			while (true) {
				try {
					var ps = await query.FindAsync ().ConfigureAwait (false);
					var tabs = ps.Select (TabObject.FromParse).ToArray ();
					if (tabs.Length == 0) {
						statsSpinner.Visibility = ViewStates.Invisible;
						karmaBar.SetAnimatedVisibility (false);
						return;
					}

					var counts = tabs.GroupBy (tab => tab.Type.Name).ToArray ();
					int totalPositive = 0, totalNegative = 0;
					int totalTypes = counts.Length;

					foreach (var group in counts) {
						var badge = BadgeInstances[group.Key];
						int positive = 0, negative = 0;

						foreach (var tab in group) {
							if ((tab.Originator == other && tab.Direction == TabDirection.Receiving)
							    || (tab.Originator == TabPerson.CurrentPerson && tab.Direction == TabDirection.Giving))
								positive++;
							if ((tab.Originator == TabPerson.CurrentPerson && tab.Direction == TabDirection.Receiving)
							    || (tab.Originator == other && tab.Direction == TabDirection.Giving))
								negative++;
						}

						totalPositive += positive;
						totalNegative += negative;

						Activity.RunOnUiThread (() => {
							if (selectedContact.DisplayName != userBadge.SelectedUser.DisplayName)
								return;
							badge.SetCount (positive - negative, true);
							if (--totalTypes == 0) {
								if (totalPositive == 0 && totalNegative == 0)
									karmaBar.SetAnimatedVisibility (false);
								else {
									karmaBar.SetAnimatedVisibility (true);
									karmaBar.SetKarmaBasedOnValues (totalPositive, totalNegative);
								}
								statsSpinner.Visibility = ViewStates.Invisible;
							}
						});
					}
					return;
				} catch (Exception ex) {
					Android.Util.Log.Error ("TabStats", ex.ToString (), "Error while retrieving tab list");
					statsSpinner.Visibility = ViewStates.Invisible;
				}
				await flashBarCtrl.ShowBarAsync (withMessageId: Resource.String.flashbar_stats_error);
			}
		}

		public void Refresh ()
		{
			var selectedContact = userBadge.SelectedUser;
			if (selectedContact == null || selectedContact.PersonCache == null)
				return;

			statsSpinner.Visibility = ViewStates.Visible;
			UpdateTabStatistics (selectedContact.PersonCache, selectedContact, true);
		}

		internal interface IParticipantDropZone
		{
			void SetDragOverState (bool hovering);
		}
	}
}

