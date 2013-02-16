FriendTab
=========

FriendTab is a simple [Mono for Android](http://xamarin.com/monoforandroid) application to help you track what you owe to your friends and what they owe you. It's for those folks like me who like to keep things even.

Alternatively, it's a testbed for various technique available in Android and put down in C# form. Since this project is licensed under the term of the *Apache License 2.0*, you are most welcome to dive into it and reuse whichever parts you want in your own projects.

More info on the development on the introductory blog post: *TODO*

Privacy Notice
==============

FriendTab uses your email address and those of your contacts to uniquely identify persons. Since the system doesn't really care about those address, it **only** uses a MD5 hashed version of them. We don't mirror avatar pictures either.

In the end, the only clear bits that are stored in the database are the full name of your contacts and your geographical position when registering a new tab (and the location name if you put it).

When a new tab is registered, the only persons who can then access its information in the `Activity` tab are the other actor involved in the tab and yourself.

Compilation / install
=====================

FriendTab uses [Parse](https://parse.com/) as its backend. You can set up your own Parse account at https://parse.com/signup and use your credentials by setting the values in `FriendTab/FriendTab/ParseCredentials.cs` to have your own personalized instance.

The free account gives you a million API call per month which should be more than enough for most usage.

You can also download the following package to test with the development version backend (no guarantee on your data for now): [APK file](http://neteril.org/friendtab/FriendTab.apk).

How-To Use
==========

Color code
----------

Throughout the application, FriendTab uses the color <span style="color:red">red</span> to symbolize something *given to you* and the color <span style="color:green">green</span> to symbolize something *you gave*.

Add tab
-------

The main way to use the app is through the `Add` tab. On the first run it will look like this:

![main-view-unselected](http://neteril.org/friendtab/screenshots-with-device/main-view-unselected.png)

By tapping on the badge, you can select one of your contact through the normal Android picker. When you have selected someone, the bottom part with the list of available tab types will appear and, if you have already interacted with that user, a count is shown for concerned tab types to summarize your status with that contact:

![main-view-selected-user](http://neteril.org/friendtab/screenshots-with-device/main-view-selected-user.png)

Additionaly if counts are available, a karma bar will also be displayed under the contact information to give you an overall score of your relationship (red is bad, green is good).

To register a new tab, simply long-tap one of the tab type icon until a drag operation is started and the user badge changes form to show both you and your contact avatars. You can then drop that drag on either image to register a tab, dropping on your head will induce a *given* operation while dropping on your contact head will induce a *gave* operation:

![main-view-dragging](http://neteril.org/friendtab/screenshots-with-device/main-view-dragging.png)

After dropping the tab, a dialog will appear where you can set a name for the location you are so that you can later remember the occasion for that operation:

![main-view-location-dialog](http://neteril.org/friendtab/screenshots-with-device/main-view-location-dialog.png)

That's all

Activity tab
------------

This tab simply shows you what's the last operations involving you (either from you or from one of your friend):

![activity-view-normal](http://neteril.org/friendtab/screenshots-with-device/activity-view-normal.png)

You can use a two-fingers swipe down and up on a list item to reveal a Google map of the area where that tab was registered:

![activity-view-mapextended](http://neteril.org/friendtab/screenshots-with-device/activity-view-mapextended.png)

