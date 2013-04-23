using System;
using Android.App;
using Android.Runtime;
using Parse;

namespace FriendTab
{
	[Application]
	public class App : Application
	{
		public App (IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
		{
		}

		public override void OnCreate ()
		{
			base.OnCreate ();
			ParseClient.Initialize (ParseCredentials.ApplicationID, ParseCredentials.ClientKey);
		}
	}
}

