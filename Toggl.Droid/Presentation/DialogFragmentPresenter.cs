using Android.Support.V7.App;
using System;
using System.Collections.Generic;
using Toggl.Core.UI.ViewModels;
using Toggl.Core.UI.ViewModels.Calendar;
using Toggl.Core.UI.ViewModels.Settings;
using Toggl.Core.UI.Views;
using Toggl.Droid.Fragments;
using DialogFragment = Android.Support.V4.App.DialogFragment;
using Fragment = Android.Support.V4.App.Fragment;
using FragmentManager = Android.Support.V4.App.FragmentManager;

namespace Toggl.Droid.Presentation
{
    public sealed class DialogFragmentPresenter : AndroidPresenter
    {
        protected override HashSet<Type> AcceptedViewModels { get; } = new HashSet<Type>
        {
            typeof(CalendarPermissionDeniedViewModel),
            typeof(NoWorkspaceViewModel),
            typeof(SelectColorViewModel),
            typeof(SelectDefaultWorkspaceViewModel),
            typeof(SelectUserCalendarsViewModel),
            typeof(TermsOfServiceViewModel),
            typeof(UpcomingEventsNotificationSettingsViewModel)
        };

        protected override void PresentOnMainThread<TInput, TOutput>(ViewModel<TInput, TOutput> viewModel, IView sourceView)
        {
            var fragmentManager = tryGetFragmentManager(sourceView);
            if (fragmentManager == null)
                throw new Exception($"Parent ViewModel's view trying to present {viewModel.GetType().Name} doesn't have a FragmentManager");

            var dialog = createReactiveDialog(viewModel);

            AndroidDependencyContainer.Instance
                .ViewModelCache
                .Cache(viewModel);

            dialog.Show(fragmentManager, dialog.GetType().Name);
        }

        private DialogFragment createReactiveDialog<TInput, TOutput>(ViewModel<TInput, TOutput> viewModel)
        {
            switch (viewModel)
            {
                case CalendarPermissionDeniedViewModel _:
                    return new CalendarPermissionDeniedFragment();

                case NoWorkspaceViewModel _:
                    return new NoWorkspaceFragment { Cancelable = false };

                case SelectColorViewModel _:
                    return new SelectColorFragment();

                case SelectDefaultWorkspaceViewModel _:
                    return new SelectDefaultWorkspaceFragment { Cancelable = false };

                case SelectUserCalendarsViewModel _:
                    return new SelectUserCalendarsFragment();

                case TermsOfServiceViewModel _:
                    return new TermsOfServiceFragment();

                case UpcomingEventsNotificationSettingsViewModel _:
                    return new UpcomingEventsNotificationSettingsFragment();
            }

            throw new InvalidOperationException($"There's no reactive dialog implementation for {viewModel.GetType().Name}");
        }

        private FragmentManager tryGetFragmentManager(IView sourceView)
        {
            if (sourceView is AppCompatActivity activity)
                return activity.SupportFragmentManager;

            if (sourceView is Fragment fragment)
                return fragment.FragmentManager;

            return null;
        }
    }
}
