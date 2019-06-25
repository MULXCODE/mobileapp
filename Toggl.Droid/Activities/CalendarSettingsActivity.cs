﻿using System;
using System.Linq;
using System.Reactive.Linq;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Support.V7.Widget;
using Android.Views;
using Toggl.Core.UI.ViewModels.Settings;
using Toggl.Droid.Adapters;
using Toggl.Droid.Extensions.Reactive;
using Toggl.Shared.Extensions;

namespace Toggl.Droid.Activities
{
    [Activity(Theme = "@style/Theme.Splash",
              WindowSoftInputMode = SoftInput.AdjustResize,
              ScreenOrientation = ScreenOrientation.Portrait,
              ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public sealed partial class CalendarSettingsActivity : ReactiveActivity<CalendarSettingsViewModel>
    {
        private UserCalendarsRecyclerAdapter userCalendarsAdapter;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            SetTheme(Resource.Style.AppTheme);
            base.OnCreate(savedInstanceState);
            if (ViewModelWasNotCached())
            {
                BailOutToSplashScreen();
                return;
            }
            SetContentView(Resource.Layout.CalendarSettingsActivity);

            InitializeViews();
            setupToolbar();

            setupRecyclerView();

            toggleCalendarsView.Rx().Tap()
                .Subscribe(ViewModel.TogglCalendarIntegration.Inputs)
                .DisposedBy(DisposeBag);

            ViewModel.CalendarListVisible
                .Subscribe(toggleCalendarsSwitch.Rx().CheckedObserver())
                .DisposedBy(DisposeBag);

            ViewModel.CalendarListVisible
                .Subscribe(calendarsContainer.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel
                .Calendars
                .Select(calendars => calendars.ToList())
                .Subscribe(userCalendarsAdapter.Rx().Items())
                .DisposedBy(DisposeBag);

            userCalendarsAdapter
                .ItemTapObservable
                .Subscribe(ViewModel.SelectCalendar.Inputs)
                .DisposedBy(DisposeBag);
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.CalendarSettingsMenu, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.Done:
                    ViewModel.Save.Execute();
                    return true;
                case Android.Resource.Id.Home:
                    ViewModel.CloseWithDefaultResult();
                    return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        private void setupToolbar()
        {
            SetSupportActionBar(toolbar);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);
        }

        private void setupRecyclerView()
        {
            userCalendarsAdapter = new UserCalendarsRecyclerAdapter();
            calendarsRecyclerView.SetAdapter(userCalendarsAdapter);
            calendarsRecyclerView.SetLayoutManager(new LinearLayoutManager(this));
        }
    }
}
