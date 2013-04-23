/*
 * Copyright 2012 Roman Nurik
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Threading;
using System.Threading.Tasks;

using Android.Animation;
using Android.OS;
using Android.Text;
using Android.Views;
using Android.Widget;

namespace FriendTab
{
	public class FlashBarController : AnimatorListenerAdapter
	{
		View barView;
		TextView messageView;
		Button flashBarBtn;
		ViewPropertyAnimator barAnimator;
		Handler hideHandler = new Handler();

		string message;

		const int DefaultHideTime = 5000;
		
		public FlashBarController (View flashBarView)
		{
			barView = flashBarView;
			barAnimator = barView.Animate ();

			messageView = barView.FindViewById<TextView> (Resource.Id.flashbar_message);
			flashBarBtn = barView.FindViewById<Button> (Resource.Id.flashbar_button);

			HideBar (true);
		}

		public async Task<bool> ShowBarAsync (bool immediate = false, string withMessage = null, int withMessageId = -1)
		{
			if (withMessage != null) {
				this.message = withMessage;
				messageView.Text = message;
			}
			if (withMessageId != -1) {
				this.message = barView.Resources.GetString (withMessageId);
				messageView.Text = message;
			}

			var tcs = new TaskCompletionSource<bool> ();

			Action hideRunnable = () => { HideBar (false); tcs.TrySetResult (false); };
			hideHandler.PostDelayed (hideRunnable, DefaultHideTime);

			EventHandler clickHandler = (s, e) => { HideBar (true); tcs.TrySetResult (true); };
			flashBarBtn.Click += clickHandler;

			barView.Visibility = ViewStates.Visible;
			if (immediate) {
				barView.Alpha = 1;
			} else {
				barAnimator.Cancel();
				barAnimator.Alpha (1);
				barAnimator.SetDuration (barView.Resources.GetInteger (Android.Resource.Integer.ConfigShortAnimTime));
				barAnimator.SetListener (null);
			}

			await tcs.Task;

			flashBarBtn.Click -= clickHandler;
			hideHandler.RemoveCallbacks (hideRunnable);

			return tcs.Task.Result;
		}
		
		public void HideBar (bool immediate = false)
		{
			if (immediate) {
				barView.Visibility = ViewStates.Gone;
				barView.Alpha = 0;
			} else {
				barAnimator.Cancel();
				barAnimator.Alpha (0);
				barAnimator.SetDuration (barView.Resources.GetInteger (Android.Resource.Integer.ConfigShortAnimTime));
				barAnimator.SetListener (this);
			}
		}

		public override void OnAnimationEnd (Animator animation)
		{
			barView.Visibility = ViewStates.Gone;
			message = null;
		}
	}
}



