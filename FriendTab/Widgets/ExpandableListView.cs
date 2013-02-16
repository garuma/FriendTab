
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

namespace FriendTab
{
	public class ExpandableListView : ListView, ExpandHelper.Callback
	{
		ExpandHelper expandHelper;
		bool expandoRequested;

		public ExpandableListView (Context context) :
			base (context)
		{
			Initialize ();
		}

		public ExpandableListView (Context context, IAttributeSet attrs) :
			base (context, attrs)
		{
			Initialize ();
		}

		public ExpandableListView (Context context, IAttributeSet attrs, int defStyle) :
			base (context, attrs, defStyle)
		{
			Initialize ();
		}

		void Initialize ()
		{
			Focusable = false;
			FocusableInTouchMode = false;
			DescendantFocusability = DescendantFocusability.BlockDescendants;
			LongClickable = false;
			Clickable = false;
			SetSelector (Android.Resource.Color.Transparent);
			expandHelper = new ExpandHelper (Context, this, 0, ActivityItemAdapter.MapHeight);
			expandHelper.SetEventSource (this);
		}

		public override bool OnInterceptTouchEvent (MotionEvent ev)
		{
			expandoRequested = expandHelper.OnInterceptTouchEvent (ev);
			return expandoRequested;
		}

		public override bool OnTouchEvent (MotionEvent e)
		{
			expandHelper.OnTouchEvent (e);
			return expandHelper.Progressing || e.PointerCount == 2 || base.OnTouchEvent (e);
		}

		public View GetChildAtRawPosition (float x, float y)
		{
			var index = GetItemPositionFromRawYCoordinates ((int)y);
			if (index == -1)
				return null;
			var view = GetChildAt (index);
			return view.FindViewById (Resource.Id.MapPicture);
		}

		public View GetChildAtPosition (float x, float y)
		{
			return GetChildAtRawPosition (x, y);
		}

		public bool CanChildBeExpanded (View v)
		{
			var activityItem = GetActivityItemFromChildView (v);
			return activityItem != null && activityItem.Expandable;
		}

		public bool SetUserExpandedChild (View v, bool userxpanded)
		{
			var activityItem = GetActivityItemFromChildView (v);
			if (activityItem != null)
				activityItem.Expanded = userxpanded;
			return activityItem != null;
		}

		ActivityItem GetActivityItemFromChildView (View v)
		{
			if (v == null)
				return null;
			ActivityItem item = v as ActivityItem;
			if (item != null)
				return item;
			var parent = v.Parent;
			while (parent != null && (item = parent as ActivityItem) == null)
				parent = parent.Parent;
			return item;
		}

		int GetItemPositionFromRawYCoordinates (int rawY)
		{
			int total = LastVisiblePosition - FirstVisiblePosition + 1;

			int[] coords = new int[2];
			for (int i = 0; i < total; i++) {
				var child = GetChildAt (i);
				child.GetLocationOnScreen (coords);
				int top = coords[1];
				int bottom = top + child.Height;
				if ((rawY >= top) && (rawY <= bottom))
					return i;
			}

			return -1;
		}
	}
}

