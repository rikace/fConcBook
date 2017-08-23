using System;
using System.Collections.Generic;
using System.ComponentModel;
using Android.App;
using Android.Util;
using Android.Views;
using Android.Widget;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Droid = Android;
using DevExpress.GridDemo;

[assembly: ExportRenderer(typeof(TabbedPage), typeof(GridDemoApp.Android.TabRenderer))]

namespace GridDemoApp.Android {

    public class TabRenderer: TabbedRenderer {

        Activity activity;
        DisplayMetricsDensity dpi;

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e) {
            base.OnElementPropertyChanged(sender, e);

            activity = Context as Activity;
            dpi = Resources.DisplayMetrics.DensityDpi;
            if (e.PropertyName == "Title") {
                activity.ActionBar.Title = Element.Title;
                activity.ActionBar.SetDisplayHomeAsUpEnabled(false);
                SetupTabs(activity.ActionBar);
            }
        }

        protected override void OnLayout(bool changed, int l, int t, int r, int b) {
            base.OnLayout(changed, l, t, r, b);

            ActionBar actionBar = activity.ActionBar;
            if (actionBar.TabCount > 0) {
                actionBar.Title = Element.Title;
                SetupTabs(actionBar);
                actionBar.SetDisplayHomeAsUpEnabled(false);
                activity.ActionBar.SetIcon(Resource.Drawable.menu);
            }
        }

        void SetupTabs(ActionBar actionBar) {
            for (int i = 0; i < actionBar.TabCount; i++) {
                ActionBar.Tab tab = actionBar.GetTabAt(i);
                if (TabIsEmpty(tab)) 
                    TabSetup(tab, GetTabIconId(i));
            }
        }

        int GetTabIconId(int tabIndex) {
            int tabIconId = 0;
            if (tabIndex == 0)
                tabIconId = Resource.Drawable.page_demo;
            else if (tabIndex == 1)
                tabIconId = Resource.Drawable.page_description;
            else if (tabIndex == 2)
                tabIconId = Resource.Drawable.page_csharp;
            else if (tabIndex == 3)
                tabIconId = Resource.Drawable.page_xaml;
            return tabIconId;
        }

        bool TabIsEmpty(ActionBar.Tab tab) {
            if (tab != null)
                if (tab.CustomView == null)
                    return true;
            return false;
        }

        void TabSetup(ActionBar.Tab tab, int resourceId) {
            if (resourceId != 0) {
                //if (Droid.OS.Build.VERSION.SdkInt > Droid.OS.BuildVersionCodes.JellyBeanMr1) {
                //    ImageView iv = new ImageView(activity);
                //    iv.SetImageResource(resourceId);
                //    iv.SetPadding(-25, 8, -25, 16);

                //    tab.SetCustomView(iv);
                //} else {
                    tab.SetIcon(resourceId);
                    tab.SetText("");
                //}
            }
            tab.TabSelected -= TabOnTabSelected;
            tab.TabSelected += TabOnTabSelected;
        }

        void TabOnTabSelected(object sender, ActionBar.TabEventArgs tabEventArgs) {
            activity.ActionBar.Title = Element.Title;
            activity.ActionBar.SetIcon(Resource.Drawable.menu);
            activity.ActionBar.SetDisplayHomeAsUpEnabled(false);
            activity.ActionBar.SetIcon(Resource.Drawable.menu);
        }
    }
}