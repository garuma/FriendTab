using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Android.Graphics;

using ParseLib;

using Xamarin.Contacts;

namespace FriendTab
{
	public class LeaderBoardFragment : Fragment
	{
		LinearLayout entriesLayout;

		public override void OnCreate (Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);
		}

		public override Android.Views.View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
		{
			Android.Util.Log.Debug ("Leaderboard", "OnCreateView");
			var view = inflater.Inflate (Resource.Layout.LeaderBoardLayout, container, false);
			entriesLayout = view.FindViewById<LinearLayout> (Resource.Id.EntriesLayout);
			return view;
		}

		public override void OnAttach (Android.App.Activity activity)
		{
			base.OnAttach (activity);
			Android.Util.Log.Debug ("Leaderboard", "OnAttach");
			TabTypes tabTypes = new TabTypes ();
			TabType[] types = tabTypes.ToArray ();
			var testData = new Dictionary<string, UserStatEntry[]> {
				{ "alan.mcgovern@gmail.com", new UserStatEntry[] {
						new UserStatEntry { Type = types[0], Given = 2, Gotten = 3 },
						new UserStatEntry { Type = types[3], Given = 0, Gotten = 5 },
						new UserStatEntry { Type = types[4], Given = 1, Gotten = 0 }
					}
				}
			};

			var trans = FragmentManager.BeginTransaction ();
			foreach (var kvp in testData) {
				var frag = new UserStatisticFragment (() => kvp);
				trans.Add (Resource.Id.EntriesLayout, frag);
			}
			trans.Commit ();
		}
	}

	public class UserStatisticFragment : Fragment
	{
		ImageView userHead;
		KarmaMeter karmaMeter;
		TableLayout statsTable;
		Func<KeyValuePair<string, UserStatEntry[]>> creator;

		public UserStatisticFragment ()
		{

		}

		public UserStatisticFragment (Func<KeyValuePair<string, UserStatEntry[]>> creator)
		{
			this.creator = creator;
		}

		public override void OnCreate (Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);
		}

		public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
		{
			var view = inflater.Inflate (Resource.Layout.UserStatisticLayout, container, false);
			userHead = view.FindViewById<ImageView> (Resource.Id.UserHead);
			karmaMeter = view.FindViewById<KarmaMeter> (Resource.Id.KarmaBar);
			statsTable = view.FindViewById<TableLayout> (Resource.Id.statsTable);

			return view;
		}

		public override void OnStart ()
		{
			base.OnStart ();
			Android.Util.Log.Debug ("Leaderboard", "Creating Stat fragment");
			if (creator == null)
				return;
			var kvp = creator ();
			SetUserData (kvp.Key, kvp.Value);
		}

		public void SetUserData (string userEmail, IEnumerable<UserStatEntry> entries)
		{
			FetchUserAvatar (userEmail);
			int totalGiven = 0;
			int totalGotten = 0;

			var trans = FragmentManager.BeginTransaction ();
			foreach (var entry in entries) {
				var row = new LeaderBoardRow (entry);

				totalGiven += entry.Given;
				totalGotten += entry.Gotten;

				trans.Add (Resource.Id.statsTable, row);
			}
			trans.Commit ();

			// Dum di dee
			var percentage = (((totalGiven - totalGotten) / (float)(totalGiven + totalGotten)) + 1) / 2f;
			karmaMeter.KarmaValue = percentage;
			Android.Util.Log.Debug ("Karma", "Gotten: {0}, Given: {1}, result: {2}", totalGotten, totalGiven, percentage);
		}

		void FetchUserAvatar (string userEmail)
		{
			//var addressBook = new AddressBook (Activity);
			//var androidUser = addressBook.FirstOrDefault (c => c.Emails.Any (e => e.Address == userEmail));
			Contact androidUser = null;
			
			if (androidUser != null) {
				var bmp = androidUser.GetThumbnail ();
				userHead.SetImageBitmap (bmp);
			} else {
				// We try to put a gravatar in there
				var hash = MD5String (userEmail);
				Task.Factory.StartNew (() => {
					var wc = new WebClient ();
					var data = wc.DownloadData ("http://www.gravatar.com/avatar/" + hash);
					var bmp = Android.Graphics.BitmapFactory.DecodeByteArray (data, 0, data.Length);
					Activity.RunOnUiThread (() => userHead.SetImageBitmap (bmp));
				});
			}
		}

		string MD5String (string input)
		{
			var md5 = MD5.Create ();
			var email = input.ToLowerInvariant ().Trim ();
			var hash = md5.ComputeHash (Encoding.UTF8.GetBytes (email));
			StringBuilder sb = new StringBuilder();
			for (int i=0;i < hash.Length; i++) {
				sb.Append (hash[i].ToString("X2"));
			}

			return sb.ToString ();
		}
	}

	public struct UserStatEntry
	{
		public TabType Type { get; set; }
		public int Given { get; set; }
		public int Gotten { get; set; }
	}

	class LeaderBoardRow : Fragment
	{
		UserStatEntry entry;

		public LeaderBoardRow (UserStatEntry entry)
		{
			this.entry = entry;
		}

		public ImageView Image {
			get;
			private set;
		}

		public TextView Type {
			get;
			private set;
		}

		public TextView Given {
			get;
			private set;
		}

		public TextView Gotten {
			get;
			private set;
		}

		public override void OnCreate (Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);
		}
		
		public override Android.Views.View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
		{
			Android.Util.Log.Debug ("LeaderboardRow", "OnCreateView");
			var view = inflater.Inflate (Resource.Layout.UserStatLine, container, false);
			Image = view.FindViewById<ImageView> (Resource.Id.CellTypeImage);
			Type = view.FindViewById<TextView> (Resource.Id.CellTypeName);
			Given = view.FindViewById<TextView> (Resource.Id.CellGiven);
			Gotten = view.FindViewById<TextView> (Resource.Id.CellGotten);

			Image.SetImageResource (entry.Type.ImageId);
			Type.Text = entry.Type.Name;
			Given.Text = entry.Given.ToString ();
			Gotten.Text = entry.Gotten.ToString ();

			return view;
		}
	}
}

