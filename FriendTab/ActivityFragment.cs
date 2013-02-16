
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

using ParseLib;

namespace FriendTab
{
	public class ActivityFragment : ListFragment
	{
		bool loading;
		ActivityItemAdapter adapter;
		int currentDataIndex;
		View loadingBar;
		TextView noContent;

		public override void OnCreate (Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);
		}

		public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
		{
			return inflater.Inflate (Resource.Layout.ActivityLayout, container, false);
		}

		public override void OnViewCreated (View view, Bundle savedInstanceState)
		{
			base.OnViewCreated (view, savedInstanceState);
			ListView.Scroll += HandleScroll;
			loadingBar = view.FindViewById (Resource.Id.LoadingContent);
			noContent = view.FindViewById<TextView> (Resource.Id.NoContent);
		}

		public override void OnAttach (Activity activity)
		{
			base.OnAttach (activity);

			adapter = new ActivityItemAdapter (activity);
			currentDataIndex = 0;
			ListAdapter = adapter;
		}

		public override void OnStart ()
		{
			base.OnStart ();
			if (!TabPerson.CurrentPerson.IsVerified) {
				loadingBar.Visibility = ViewStates.Gone;
				noContent.Visibility = ViewStates.Visible;
				noContent.Text = "You need to verify your account email.";
			} else if (currentDataIndex == 0) {
				RetrieveActivityData (adapter);
			}
		}

		public void AddLocalTabObject (TabObject tabObject)
		{
			adapter.PrependData (tabObject);
			currentDataIndex++;
		}

		public override void OnCreateOptionsMenu (IMenu menu, MenuInflater inflater)
		{
			inflater.Inflate (Resource.Menu.activity_menu, menu);
		}

		public override bool OnOptionsItemSelected (IMenuItem item)
		{
			// There is only refresh
			Refresh ();
			return true;
		}

		public void Refresh ()
		{
			currentDataIndex = 0;
			adapter = new ActivityItemAdapter (Activity);
			RetrieveActivityData (adapter);
			ListAdapter = adapter;
		}

		void HandleScroll (object sender, AbsListView.ScrollEventArgs e)
		{
			if (loading || e.FirstVisibleItem + e.VisibleItemCount < e.TotalItemCount)
				return;
			loading = true;

			RetrieveActivityData (adapter);
		}

		void RetrieveActivityData (ActivityItemAdapter adapter)
		{
			int count = currentDataIndex == 0 ? 5 : 10;
			var query = CreateQuery (skip: currentDataIndex, limit: count);
			if (currentDataIndex == 0) {
				loadingBar.Visibility = ViewStates.Visible;
				noContent.Visibility = ViewStates.Gone;
			}
			query.FindInBackground (new TabFindCallback ((ps, e) => {
				if (e == null) {
					if (ps.Count == 0) {
						if (currentDataIndex == 0) {
							loadingBar.Visibility = ViewStates.Gone;
							noContent.Visibility = ViewStates.Visible;
						}
						return;
					}
					var tabObjects = ps.Select (TabObject.FromParse).ToList ();
					adapter.FeedData (tabObjects);
					currentDataIndex += count;
					loading = false;
				} else {
					Log.Error ("ActivityRetriever", e.ToString ());
					loading = false;
				}
			}));
		}

		ParseQuery CreateQuery (int skip = 0, int limit = 10)
		{
			var query = ParseQuery.Or (new ParseQuery[] {
				new ParseQuery ("Tab").WhereEqualTo ("originator", TabPerson.CurrentPerson.ToParse ()),
				new ParseQuery ("Tab").WhereEqualTo ("recipient", TabPerson.CurrentPerson.ToParse ())
			});
			query.SetCachePolicy (ParseQuery.CachePolicy.NetworkElseCache);
			query.OrderByDescending ("time");
			query.Include ("originator");
			query.Include ("recipient");
			query.Include ("tabType");

			if (skip > 0)
				query.Skip = skip;
			query.Limit = limit;

			return query;
		}
	}
}

