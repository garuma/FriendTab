
using System;
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

namespace FriendTab
{
	public class TapUserBadge : Fragment, ParticipantSelectionFragment.IParticipantDropZone
	{
		public event EventHandler SelectedUserChanged;

		FadeImageView icon;
		TextView title;
		TextView subtitle;
		TextView tapInset;
		LinearLayout infoLayout;
		bool isPicking;
		Bitmap contactPicture;

		// Represent the information of the currently selected user (null if none)
		// first item is the display name and the second is the list of emails
		// third is the lookup id and fourth is contact id
		public SelectedUserInfo SelectedUser {
			get;
			private set;
		}

		public override void OnCreate (Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);
		}

		public override Android.Views.View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
		{
			var view = inflater.Inflate (Resource.Layout.TapBadge, container, false);
			icon = view.FindViewById<FadeImageView> (Resource.Id.BadgeIcon);
			icon.SetImageBitmap (contactPicture, false);
			title = view.FindViewById<TextView> (Resource.Id.BadgeTitle);
			subtitle = view.FindViewById<TextView> (Resource.Id.BadgeSubtitle);
			tapInset = view.FindViewById<TextView> (Resource.Id.TapInset);
			infoLayout = view.FindViewById<LinearLayout> (Resource.Id.TapInfoLayout);

			SetupBadge (view);

			return view;
		}

		public override void OnAttach (Activity activity)
		{
			base.OnAttach (activity);
			contactPicture = SvgUtils.GetBitmapFromSvgRes (activity.Resources,
			                                               Resource.Drawable.ic_contact_picture,
			                                               80.ToPixels (),
			                                               80.ToPixels ());
		}

		public void RestoreSelectedUserInfo (SelectedUserInfo selectedUser)
		{
			SelectedUser = selectedUser;
			ProcessSelectedUser (selectedUser);
		}

		void SetupBadge (View view)
		{
			view.Clickable = true;
			view.Click += (sender, e) => {
				if (isPicking)
					return;
				isPicking = true;
				var pickIntent = new Intent (Intent.ActionPick,
				                             ContactsContract.Contacts.ContentUri);
				StartActivityForResult (pickIntent, 0);
			};
		}

		public void SetDragOverState (bool hovering)
		{
			View.SetBackgroundResource (hovering ? Resource.Drawable.boxdrag : Resource.Drawable.box);
		}

		public override void OnActivityResult (int requestCode, Result resultCode, Intent data)
		{
			base.OnActivityResult (requestCode, resultCode, data);

			if (resultCode != Result.Ok) {
				isPicking = false;
				return;
			}

			var contactData = data.Data;
			var projs = new string[] {
				ContactsContract.Contacts.InterfaceConsts.Id,
				ContactsContract.Contacts.InterfaceConsts.LookupKey,
				ContactsContract.Contacts.InterfaceConsts.DisplayName,
			};
			var cursor = Activity.ContentResolver.Query (contactData, projs, null, null, null);

			int index = -1;
			string contactID = null;
			string lookupID = null;
			string displayName = string.Empty;
			Bitmap photoBmp = null;

			if (cursor.MoveToFirst ()) {
				contactID = cursor.GetString (cursor.GetColumnIndex (projs [0]));
				lookupID = cursor.GetString (cursor.GetColumnIndex (projs [1]));

				if ((index = cursor.GetColumnIndex (projs [2])) != -1)
					displayName = cursor.GetString (index);

				var photoStream = ContactsContract.Contacts.OpenContactPhotoInputStream (Activity.ContentResolver,
				                                                                         contactData,
				                                                                         false);
				if (photoStream != null)
					photoBmp = BitmapFactory.DecodeStream (photoStream);
			}
			cursor.Close ();

			// Get emails
			var emailCursor = Activity.ContentResolver.Query (ContactsContract.CommonDataKinds.Email.ContentUri,
			                                                  null,
			                                                  ContactsContract.CommonDataKinds.Email.InterfaceConsts.ContactId + " = ?",
			                                                  new[] { contactID }, null);
			var emails = new List<string> (emailCursor.Count);
			while (emailCursor.MoveToNext ())
				emails.Add (emailCursor.GetString (emailCursor.GetColumnIndex (ContactsContract.CommonDataKinds.Email.Address)));
			emailCursor.Close ();

			SelectedUser = new SelectedUserInfo {
				DisplayName = displayName,
				Emails = emails,
				LookupID = lookupID,
				ContactID = contactID,
				AvatarBitmap = photoBmp
			};

			ProcessSelectedUser (SelectedUser);

			isPicking = false;
		}

		void ProcessSelectedUser (SelectedUserInfo selectedUser)
		{
			tapInset.Visibility = ViewStates.Gone;
			infoLayout.Visibility = ViewStates.Visible;

			title.Text = selectedUser.DisplayName;

			if (selectedUser.AvatarBitmap != null)
				icon.SetImageBitmap (selectedUser.AvatarBitmap, true);
			else
				icon.SetImageBitmap (contactPicture, true);

			if (selectedUser.Emails.Any ()) {
				subtitle.Text = selectedUser.Emails.First ();
				subtitle.Visibility = ViewStates.Visible;
			} else {
				subtitle.Visibility = ViewStates.Invisible;
			}

			if (SelectedUserChanged != null)
				SelectedUserChanged (this, EventArgs.Empty);
		}
	}
}

