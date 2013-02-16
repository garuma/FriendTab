/*
 * Copyright (C) 2012 The Android Open Source Project
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

using Android.Animation;
using Android.Content;
using Android.Views;
using Android.Util;

namespace FriendTab
{
	public class ExpandHelper : Java.Lang.Object
	{
		public interface Callback
		{
			View GetChildAtRawPosition(float x, float y);
			View GetChildAtPosition(float x, float y);
			bool CanChildBeExpanded(View v);
			bool SetUserExpandedChild(View v, bool userxpanded);
		}

		const string Tag = "ExpandHelper";
		protected const bool Debug = false;
		const long ExpandDuration = 250;
		const long GlowDuration = 150;

		// Set to false to disable focus-based gestures (two-finger pull).
		const bool UseDrag = true;
		// Set to false to disable scale-based gestures (both horizontal and vertical).
		const bool UseSpan = false;
		// Both gestures types may be active at the same time.
		// At least one gesture type should be active.
		// A variant of the screwdriver gesture will emerge from either gesture type.

		// amount of overstretch for maximum brightness expressed in U
		// 2f: maximum brightness is stretching a 1U to 3U, or a 4U to 6U
		const float StretchInterval = 2f;

		// level of glow for a touch, without overstretch
		// overstretch fills the range (GLOW_BASE, 1.0]
		const float GlowBase = 0.5f;

#pragma warning disable 414
		Context context;
#pragma warning restore

		bool stretching;
		View eventSource;
		View currView;
		View currViewTopGlow;
		View currViewBottomGlow;
		float oldHeight;
		float naturalHeight;
		float initialTouchFocusY;
		float initialTouchSpan;
		Callback callback;
		DoubleSwipeDetector detector;
		ViewScaler scaler;
		//ObjectAnimator scaleAnimation;
		ValueAnimator scaleAnimation;
		AnimatorSet glowAnimationSet;
		ObjectAnimator glowTopAnimation;
		ObjectAnimator glowBottomAnimation;

		int smallSize;
		int largeSize;
		float maximumStretch;

		GravityFlags gravity;

		class ViewScaler
		{
			View mView;
			ViewGroup.LayoutParams pars;

			public ViewScaler() {}

			public void SetView(View v)
			{
				mView = v;
				pars = v.LayoutParameters;
			}

			public float Height {
				get {
					int height = mView == null ? 0 : mView.LayoutParameters.Height;
					if (height < 0) {
						height = mView.MeasuredHeight;
					}
					return (float) height;
				}
				set {
					if (mView == null)
						return;
					mView.Visibility = value == 0 ? ViewStates.Invisible : ViewStates.Visible;
					pars.Height = (int)value;
					mView.RequestLayout ();
				}
			}

			public int GetNaturalHeight (int maximum)
			{
				ViewGroup.LayoutParams lp = mView.LayoutParameters;
				int oldHeight = lp.Height;
				lp.Height = ViewGroup.LayoutParams.WrapContent;
				mView.LayoutParameters = lp;
				mView.Measure (
					View.MeasureSpec.MakeMeasureSpec(mView.MeasuredWidth, MeasureSpecMode.Exactly),
					View.MeasureSpec.MakeMeasureSpec(maximum, MeasureSpecMode.AtMost));
				lp.Height = oldHeight;
				mView.LayoutParameters = lp;
				var height = mView.MeasuredHeight;
				return height;
			}
		}

		class AnimatorListener : AnimatorListenerAdapter
		{
			public override void OnAnimationStart(Animator animation)
			{
				View target = (View) ((ObjectAnimator) animation).Target;
				if (target.Alpha <= 0.0f)
					target.Visibility = ViewStates.Visible;
			}

			public override void OnAnimationEnd(Animator animation)
			{
				View target = (View) ((ObjectAnimator) animation).Target;
				if (target.Alpha <= 0.0f)
					target.Visibility = ViewStates.Invisible;
			}
		}

		class GestureDetector : DoubleSwipeDetector.IOnScaleGestureListener
		{
			ExpandHelper parent;

			public GestureDetector (ExpandHelper parent)
			{
				this.parent = parent;
			}

			public bool OnScaleBegin (DoubleSwipeDetector detector)
			{
				float x = detector.FocusX;
				float y = detector.FocusY;

				View v = null;
				if (parent.eventSource != null) {
					int[] location = new int[2];
					parent.eventSource.GetLocationOnScreen(location);
					x += (float) location[0];
					y += (float) location[1];
					v = parent.callback.GetChildAtRawPosition(x, y);
				} else {
					v = parent.callback.GetChildAtPosition(x, y);
				}

				// your fingers have to be somewhat close to the bounds of the view in question
				parent.initialTouchFocusY = detector.FocusY;
				parent.initialTouchSpan = Math.Abs(detector.CurrentSpan);

				parent.stretching = parent.InitScale(v);
				return parent.stretching;
			}

			public bool OnScale (DoubleSwipeDetector detector)
			{
				// are we scaling or dragging?
				float span = Math.Abs(detector.CurrentSpan) - parent.initialTouchSpan;
				span *= UseSpan ? 1f : 0f;
				float drag = detector.FocusY - parent.initialTouchFocusY;
				drag *= UseDrag ? 1f : 0f;
				drag *= parent.gravity == GravityFlags.Bottom ? -1f : 1f;
				float pull = Math.Abs(drag) + Math.Abs(span) + 1f;
				float hand = drag * Math.Abs(drag) / pull + span * Math.Abs(span) / pull;

				hand = hand + parent.oldHeight;
				float target = hand;

				hand = hand < parent.smallSize ? parent.smallSize : (hand > parent.largeSize ? parent.largeSize : hand);
				hand = hand > parent.naturalHeight ? parent.naturalHeight : hand;

				parent.scaler.Height = hand;

				// glow if overscale
				float stretch = (float) Math.Abs((target - hand) / parent.maximumStretch);
				float strength = 1f / (1f + (float) Math.Pow(Math.E, -1 * ((8f * stretch) - 5f)));
				parent.SetGlow(GlowBase + strength * (1f - GlowBase));
				return true;
			}

			public void OnScaleEnd(DoubleSwipeDetector detector)
			{
				parent.FinishScale(false);
			}
		}

		public ExpandHelper(Context context, Callback callback, int small, int large) {
			this.smallSize = small;
			this.maximumStretch = Math.Max (smallSize, 1) * StretchInterval;
			this.largeSize = large;
			this.context = context;
			this.callback = callback;
			this.scaler = new ViewScaler();
			this.gravity = GravityFlags.Top;

			//this.scaleAnimation = ObjectAnimator.OfFloat (scaler, "Height", 0f);
			this.scaleAnimation = ValueAnimator.OfFloat (0f);
			this.scaleAnimation.Update += (sender, e) => scaler.Height = (float)e.Animation.AnimatedValue;
			this.scaleAnimation.SetDuration (ExpandDuration);

			AnimatorListenerAdapter glowVisibilityController = new AnimatorListener ();
			glowTopAnimation = ObjectAnimator.OfFloat (null, "alpha", 0f);
			glowTopAnimation.AddListener (glowVisibilityController);
			glowBottomAnimation = ObjectAnimator.OfFloat (null, "alpha", 0f);
			glowBottomAnimation.AddListener (glowVisibilityController);
			glowAnimationSet = new AnimatorSet();
			glowAnimationSet.Play (glowTopAnimation).With(glowBottomAnimation);
			glowAnimationSet.SetDuration(GlowDuration);

			detector = new DoubleSwipeDetector(context, new GestureDetector (this));
		}

		public bool Progressing {
			get {
				return detector.IsInProgress;
			}
		}

		public void SetEventSource (View eventSource)
		{
			this.eventSource = eventSource;
		}

		public void SetGravity (GravityFlags gravity)
		{
			this.gravity = gravity;
		}

		public void SetGlow (float glow)
		{
			if (!glowAnimationSet.IsRunning || glow == 0f) {
				if (glowAnimationSet.IsRunning) {
					glowAnimationSet.Cancel ();
				}
				if (currViewTopGlow != null && currViewBottomGlow != null) {
					if (glow == 0f || currViewTopGlow.Alpha == 0f) { 
						// animate glow in and out
						glowTopAnimation.SetTarget(currViewTopGlow);
						glowBottomAnimation.SetTarget(currViewBottomGlow);
						glowTopAnimation.SetFloatValues(glow);
						glowBottomAnimation.SetFloatValues(glow);
						glowAnimationSet.SetupStartValues();
						glowAnimationSet.Start();
					} else {
						// set it explicitly in reponse to touches.
						currViewTopGlow.Alpha = glow;
						currViewBottomGlow.Alpha = glow;
						HandleGlowVisibility();
					}
				}
			}
		}

		void HandleGlowVisibility ()
		{
			currViewTopGlow.Visibility = currViewTopGlow.Alpha <= 0.0f ? ViewStates.Invisible : ViewStates.Visible;
			currViewBottomGlow.Visibility = currViewBottomGlow.Alpha <= 0.0f ? ViewStates.Invisible : ViewStates.Visible;
		}

		public bool OnInterceptTouchEvent (MotionEvent ev)
		{
			stretching = detector.OnTouchEvent(ev);
			return stretching;
		}

		public bool OnTouchEvent (MotionEvent ev)
		{
			var action = ev.Action;
			if (stretching)
				detector.OnTouchEvent(ev);
			switch (action) {
			case MotionEventActions.Up:
			case MotionEventActions.Cancel:
				stretching = false;
				ClearView();
				break;
			}
			return true;
		}

		bool InitScale (View v)
		{
			if (v != null) {
				stretching = true;
				SetView(v);
				SetGlow(GlowBase);
				scaler.SetView(v);
				oldHeight = scaler.Height;
				if (callback.CanChildBeExpanded(v))
					naturalHeight = scaler.GetNaturalHeight(largeSize);
				else
					naturalHeight = oldHeight;
				v.Parent.RequestDisallowInterceptTouchEvent (true);
			}
			return stretching;
		}

		void FinishScale (bool force) {
			float h = scaler.Height;
			bool wasClosed = (oldHeight == smallSize);
			if (wasClosed) {
				h = (force || h > smallSize) ? naturalHeight : smallSize;
			} else {
				h = (force || h < naturalHeight) ? smallSize : naturalHeight;
			}
			if (scaleAnimation.IsRunning) {
				scaleAnimation.Cancel();
			}
			scaleAnimation.SetFloatValues (scaler.Height, h);
			scaleAnimation.Start();
			stretching = false;
			SetGlow(0f);
			callback.SetUserExpandedChild(currView, h == naturalHeight);

			ClearView();
		}

		void ClearView ()
		{
			currView = null;
			currViewTopGlow = null;
			currViewBottomGlow = null;
		}

		void SetView (View v)
		{
			currView = v;
			ViewGroup g = v as ViewGroup;
			if (g == null)
				g = v.Parent.Parent as ViewGroup;
			if (g != null) {
				currViewTopGlow = g.FindViewById(Resource.Id.TopGlow);
				currViewBottomGlow = g.FindViewById(Resource.Id.BottomGlow);
			}
		}

		/*public void OnClick(View v)
		{
			InitScale(v);
			FinishScale(true);
		}*/
	}
}
