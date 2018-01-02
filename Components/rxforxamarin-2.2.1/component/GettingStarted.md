Rx is published as an open source project and you can pick up sources from CodePlex (http://rx.codeplex.com). There is a dedicated web page section for Rx on MSDN website too: http://msdn.microsoft.com/en-us/data/gg577609.aspx

This component contains Rx libraries that are suited for Xamarin.Android and Xamarin.iOS.

# Adjusting Library References

After adding this component to your project, you would notice that there are several dlls in this package. You would however most likely need to use the following four assemblies:

* System.Reactive.Interfaces.dll
* System.Reactive.Core.dll
* System.Reactive.Linq.dll
* System.Reactive.PlatformServices.dll

All other assemblies are optional and you would like to use them only in certain scenarios. On the other hand, those four assemblies are essential. So far let's remove other assemblies in this package.

![typical Rx assembly references](https://raw.github.com/mono/rx/rx-oss-v2.1/xpkg/ProjectReferences.png)

(Note that Rx version 2.x is very different from Rx v1.0 in terms of assemblies; Rx 1.0 consists only of System.Reactive.dll, which does not exist in Rx v2.x.)

# Sample: Transforming touch events into another event

Here we show an example use of `Observable.FromEventPattern()` and `Observable.ToEventPattern()` methods to turn `View.Touch` event into "notify only when three fingers are moving" event (here we'll call it "TripleTouch").

Let's begin with a simple application project. After you created one, you will need some using statements for Rx:

    using System.Reactive;
    using System.Reactive.Linq;

The "TripleTouch" event is defined and implemented as follows:

    IEventPatternSource<View.TouchEventArgs> triple_touch_source;

    public event EventHandler<View.TouchEventArgs> TripleTouch {
	    add { triple_touch_source.OnNext += value; }
        remove { triple_touch_source.OnNext -= value; }
    }

This event is populated when the View is set up. In the simple application sample, I wrote this in the `Activity`'s `OnCreate()`:

    ...
    // this "surface" is the target View here.
    // It can be "this" when you implement a custom component.
    var surface = FindViewById<View> (Resource.Id.theToucheable);
	
    triple_touch_source = Observable.FromEventPattern<View.TouchEventArgs> (surface, "Touch")
        .Where (ev => ev.EventArgs.Event.Action == MotionEventActions.Move)
        .Where (ev => ev.EventArgs.Event.PointerCount == 3)
        .ToEventPattern ();
	...

Then it could be consumed by the `View` users (in the sample, the first line of code is in the `OnCreate()` method):

    ...
    TripleTouch += (sender, ev) => this.RunOnUiThread (() => text.Text = GetEventDescription (ev.Event));
    ...
    
    static string GetEventDescription (MotionEvent e)
    {
        return string.Format ("({0}, {1})", e.RawX, e.RawY);
    }

In the sample app project, we defined very simple UI in Main.axml:

    <LinearLayout xmlns:android="http://schemas.android.com/apk/res/android"
        android:orientation="vertical"
        android:layout_width="fill_parent"
        android:layout_height="fill_parent">
        <View
            android:id="@+id/theToucheable"
            android:layout_width="fill_parent"
            android:layout_height="440.7dp"
            android:layout_marginBottom="0.0dp" />
        <TextView
            android:id="@+id/theText"
            android:layout_width="fill_parent"
            android:layout_height="wrap_content"
            android:text="(touch corrdinates shown here)" />
    </LinearLayout>

The sample is all done. Build and run the app on device. Then touch one finger. Nothing happens. Touch with one more finger. Still nothing happens. Add another finger. Then it starts showing the coordinate (of the first finger; this is just a sample so it doesn't give complicated action).

What implements such behavior? Let's see the `Observable` part:

    triple_touch_source = Observable.FromEventPattern<View.TouchEventArgs> (surface, "Touch")

This converts a `View.Touch` event into an `IObservable`.

    .Where (ev => ev.EventArgs.Event.Action == MotionEventActions.Move)

This filters out events that are not move events.

    .Where (ev => ev.EventArgs.Event.PointerCount == 3)

This filters out events that don't detect three fingers. Now that we have only three-fingered events, we want to convert this observables into another event source:

    .ToEventPattern ();

Once it's done, we use it to process the actual event. Note that since we are going to control UI, we need to invoke via `RunOnUiThread()`:

    TripleTouch += (sender, ev) => this.RunOnUiThread (() => text.Text = GetEventDescription (ev.Event));

Actually, if you don't convert the filtered observables into another event, you might want to use `SynchronizationContext` instead (we didn't do that in this example because having event processing all within the UI thread is not good):

    (...).SubscribeOn (Android.App.Application.SynchronizationContext) (...)
