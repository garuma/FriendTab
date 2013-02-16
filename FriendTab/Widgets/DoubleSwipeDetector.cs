/*
 * Copyright (C) 2010 The Android Open Source Project
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

using Android.Runtime;
using Android.Content;
using Android.Util;
using Android.Views;

namespace FriendTab
{
	public class DoubleSwipeDetector
	{
		public interface IOnScaleGestureListener {

			bool OnScale(DoubleSwipeDetector detector);

			bool OnScaleBegin(DoubleSwipeDetector detector);

			void OnScaleEnd(DoubleSwipeDetector detector);
		}

		public class SimpleOnScaleGestureListener : IOnScaleGestureListener {

			public bool OnScale(DoubleSwipeDetector detector) {
				return false;
			}

			public bool OnScaleBegin(DoubleSwipeDetector detector) {
				return true;
			}

			public void OnScaleEnd(DoubleSwipeDetector detector) {
				// Intentionally empty
			}
		}

		static float PRESSURE_THRESHOLD = 0.67f;

#pragma warning disable 414
		Context context;
#pragma warning restore
		IOnScaleGestureListener mListener;
		bool mGestureInProgress;

		MotionEvent mPrevEvent;
		MotionEvent mCurrEvent;

		float mFocusX;
		float mFocusY;
		float mPrevFingerDiffX;
		float mPrevFingerDiffY;
		float mCurrFingerDiffX;
		float mCurrFingerDiffY;
		float mCurrLen;
		float mPrevLen;
		float mScaleFactor;
		float mCurrPressure;
		float mPrevPressure;
		long mTimeDelta;

		bool mInvalidGesture;

		// Pointer IDs currently responsible for the two fingers controlling the gesture
		int mActiveId0;
		int mActiveId1;
		bool mActive0MostRecent;

		public DoubleSwipeDetector (Context context, IOnScaleGestureListener listener)
		{
			this.context = context;
			mListener = listener;
		}

		public bool OnTouchEvent (MotionEvent evt)
		{
			var action = evt.ActionMasked;

			if (action == MotionEventActions.Down) {
				Reset(); // Start fresh
			}

			bool handled = true;
			if (mInvalidGesture) {
				handled = false;
			} else if (!mGestureInProgress) {
				switch (action) {
				case MotionEventActions.Down: {
					mActiveId0 = evt.GetPointerId(0);
					mActive0MostRecent = true;
				}
					break;

				case MotionEventActions.Up:
					Reset();
					break;

				case MotionEventActions.PointerDown: {
					// We have a new multi-finger gesture
					if (mPrevEvent != null) mPrevEvent.Recycle();
					mPrevEvent = MotionEvent.Obtain(evt);
					mTimeDelta = 0;

					int index1 = evt.ActionIndex;
					int index0 = evt.FindPointerIndex(mActiveId0);
					mActiveId1 = evt.GetPointerId(index1);
					if (index0 < 0 || index0 == index1) {
						// Probably someone sending us a broken evt stream.
						index0 = FindNewActiveIndex(evt, mActiveId1, -1);
						mActiveId0 = evt.GetPointerId(index0);
					}
					mActive0MostRecent = false;

					SetContext(evt);

					mGestureInProgress = mListener.OnScaleBegin(this);
					break;
				}
				}
			} else {
				// Transform gesture in progress - attempt to handle it
				switch (action) {
				case MotionEventActions.Down: {
					// End the old gesture and begin a new one with the most recent two fingers.
					mListener.OnScaleEnd(this);
					int oldActive0 = mActiveId0;
					int oldActive1 = mActiveId1;
					Reset();

					mPrevEvent = MotionEvent.Obtain(evt);
					mActiveId0 = mActive0MostRecent ? oldActive0 : oldActive1;
					mActiveId1 = evt.GetPointerId(evt.ActionIndex);
					mActive0MostRecent = false;

					int index0 = evt.FindPointerIndex (mActiveId0);
					if (index0 < 0 || mActiveId0 == mActiveId1) {
						index0 = FindNewActiveIndex(evt, mActiveId1, -1);
						mActiveId0 = evt.GetPointerId(index0);
					}

					SetContext(evt);

					mGestureInProgress = mListener.OnScaleBegin(this);
				}
					break;

				case MotionEventActions.PointerUp: {
					int pointerCount = evt.PointerCount;
					int actionIndex = evt.ActionIndex;
					int actionId = evt.GetPointerId(actionIndex);

					bool gestureEnded = false;
					if (pointerCount > 2) {
						if (actionId == mActiveId0) {
							int newIndex = FindNewActiveIndex(evt, mActiveId1, actionIndex);
							if (newIndex >= 0) {
								mListener.OnScaleEnd(this);
								mActiveId0 = evt.GetPointerId(newIndex);
								mActive0MostRecent = true;
								mPrevEvent = MotionEvent.Obtain(evt);
								SetContext(evt);
							mGestureInProgress = mListener.OnScaleBegin(this);
							} else {
								gestureEnded = true;
							}
						} else if (actionId == mActiveId1) {
							int newIndex = FindNewActiveIndex(evt, mActiveId0, actionIndex);
							if (newIndex >= 0) {
								mListener.OnScaleEnd(this);
								mActiveId1 = evt.GetPointerId(newIndex);
								mActive0MostRecent = false;
								mPrevEvent = MotionEvent.Obtain(evt);
								SetContext(evt);
								mGestureInProgress = mListener.OnScaleBegin(this);
							} else {
								gestureEnded = true;
							}
						}
						mPrevEvent.Recycle();
						mPrevEvent = MotionEvent.Obtain(evt);
						SetContext(evt);
					} else {
						gestureEnded = true;
					}

					if (gestureEnded) {
						// Gesture ended
						SetContext(evt);

						// Set focus point to the remaining finger
						int activeId = actionId == mActiveId0 ? mActiveId1 : mActiveId0;
						int index = evt.FindPointerIndex(activeId);
						mFocusX = evt.GetX(index);
						mFocusY = evt.GetY(index);

						mListener.OnScaleEnd(this);
						Reset();
						mActiveId0 = activeId;
						mActive0MostRecent = true;
					}
				}
					break;

				case MotionEventActions.Cancel:
					mListener.OnScaleEnd(this);
					Reset();
					break;

				case MotionEventActions.Up:
					Reset();
					break;

				case MotionEventActions.Move: {
					SetContext(evt);

					// Only accept the evt if our relative pressure is within
					// a certain limit - this can help filter shaky data as a
					// finger is lifted.
					if (mCurrPressure / mPrevPressure > PRESSURE_THRESHOLD) {
						bool updatePrevious = mListener.OnScale(this);

						if (updatePrevious) {
							mPrevEvent.Recycle();
							mPrevEvent = MotionEvent.Obtain(evt);
						}
					}
				}
					break;
				}
			}

			return handled;
		}

		int FindNewActiveIndex(MotionEvent ev, int otherActiveId, int removedPointerIndex)
		{
			int pointerCount = ev.PointerCount;

			// It's ok if this isn't found and returns -1, it simply won't match.
			int otherActiveIndex = ev.FindPointerIndex(otherActiveId);

			// Pick a new id and update tracking state.
			for (int i = 0; i < pointerCount; i++) {
				if (i != removedPointerIndex && i != otherActiveIndex) {
					return i;
				}
			}
			return -1;
		}

		void SetContext (MotionEvent curr)
		{
			if (mCurrEvent != null) {
				mCurrEvent.Recycle();
			}
			mCurrEvent = MotionEvent.Obtain(curr);

			mCurrLen = -1;
			mPrevLen = -1;
			mScaleFactor = -1;

			MotionEvent prev = mPrevEvent;

			int prevIndex0 = prev.FindPointerIndex(mActiveId0);
			int prevIndex1 = prev.FindPointerIndex(mActiveId1);
			int currIndex0 = curr.FindPointerIndex(mActiveId0);
			int currIndex1 = curr.FindPointerIndex(mActiveId1);

			if (prevIndex0 < 0 || prevIndex1 < 0 || currIndex0 < 0 || currIndex1 < 0) {
				mInvalidGesture = true;
				if (mGestureInProgress) {
					mListener.OnScaleEnd(this);
				}
				return;
			}

			float px0 = prev.GetX(prevIndex0);
			float py0 = prev.GetY(prevIndex0);
			float px1 = prev.GetX(prevIndex1);
			float py1 = prev.GetY(prevIndex1);
			float cx0 = curr.GetX(currIndex0);
			float cy0 = curr.GetY(currIndex0);
			float cx1 = curr.GetX(currIndex1);
			float cy1 = curr.GetY(currIndex1);

			float pvx = px1 - px0;
			float pvy = py1 - py0;
			float cvx = cx1 - cx0;
			float cvy = cy1 - cy0;
			mPrevFingerDiffX = pvx;
			mPrevFingerDiffY = pvy;
			mCurrFingerDiffX = cvx;
			mCurrFingerDiffY = cvy;

			mFocusX = cx0 + cvx * 0.5f;
			mFocusY = cy0 + cvy * 0.5f;
			mTimeDelta = curr.EventTime - prev.EventTime;
			mCurrPressure = curr.GetPressure(currIndex0) + curr.GetPressure(currIndex1);
			mPrevPressure = prev.GetPressure(prevIndex0) + prev.GetPressure(prevIndex1);
		}

		void Reset ()
		{
			if (mPrevEvent != null) {
				mPrevEvent.Recycle();
				mPrevEvent = null;
			}
			if (mCurrEvent != null) {
				mCurrEvent.Recycle();
				mCurrEvent = null;
			}
			mGestureInProgress = false;
			mActiveId0 = -1;
			mActiveId1 = -1;
			mInvalidGesture = false;
		}

		public bool IsInProgress {
			get {
				return mGestureInProgress;
			}
		}

		public float FocusX {
			get {
				return mFocusX;
			}
		}

		public float FocusY {
			get {
				return mFocusY;
			}
		}

		public float CurrentSpan {
			get {
				if (mCurrLen == -1) {
					float cvx = mCurrFingerDiffX;
					float cvy = mCurrFingerDiffY;
					mCurrLen = (float)Math.Sqrt (cvx * cvx + cvy * cvy);
				}
				return mCurrLen;
			}
		}

		public float CurrentSpanX {
			get {
				return mCurrFingerDiffX;
			}
		}

		public float CurrentSpanY {
			get {
				return mCurrFingerDiffY;
			}
		}

		public float PreviousSpan {
			get {
				if (mPrevLen == -1) {
					float pvx = mPrevFingerDiffX;
					float pvy = mPrevFingerDiffY;
					mPrevLen = (float)Math.Sqrt (pvx * pvx + pvy * pvy);
				}
				return mPrevLen;
			}
		}

		public float PreviousSpanX {
			get {
				return mPrevFingerDiffX;
			}
		}

		public float PreviousSpanY {
			get {
				return mPrevFingerDiffY;
			}
		}

		public float ScaleFactor {
			get {
				if (mScaleFactor == -1) {
					mScaleFactor = CurrentSpan / PreviousSpan;
				}
				return mScaleFactor;
			}
		}

		public long TimeDelta {
			get {
				return mTimeDelta;
			}
		}

		public long EventTime {
			get {
				return mCurrEvent.EventTime;
			}
		}
	}
}

