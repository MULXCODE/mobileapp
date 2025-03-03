﻿using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Toggl.Core.Tests.Generators;
using Toggl.Core.Tests.TestExtensions;
using Toggl.Core.UI.ViewModels.Selectable;
using Toggl.Core.UI.ViewModels.Settings;
using Toggl.Shared;
using Xunit;

namespace Toggl.Core.Tests.UI.ViewModels
{
    public sealed class CalendarSettingsViewModelTests
    {
        public abstract class CalendarSettingsViewModelTest : BaseViewModelTests<CalendarSettingsViewModel, bool, string[]>
        {
            protected override CalendarSettingsViewModel CreateViewModel()
                => new CalendarSettingsViewModel(UserPreferences, InteractorFactory, NavigationService, RxActionFactory, PermissionsChecker);
        }

        public sealed class TheConstructor : CalendarSettingsViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useUserPreferences,
                bool useInteractorFactory,
                bool useNavigationService,
                bool useRxActionFactory,
                bool usePermissionsChecker)
            {
                Action tryingToConstructWithEmptyParameters =
                    () => new CalendarSettingsViewModel(
                        useUserPreferences ? UserPreferences : null,
                        useInteractorFactory ? InteractorFactory : null,
                        useNavigationService ? NavigationService : null,
                        useRxActionFactory ? RxActionFactory : null,
                        usePermissionsChecker ? PermissionsChecker : null
                    );

                tryingToConstructWithEmptyParameters.Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class ThePermissionGrantedProperty : CalendarSettingsViewModelTest
        {
            [Theory]
            [InlineData(true)]
            [InlineData(false)]
            public async Task GetsInitialisedToTheProperValue(bool permissionGranted)
            {
                UserPreferences.EnabledCalendarIds().Returns(new List<string>());
                PermissionsChecker.CalendarPermissionGranted.Returns(Observable.Return(permissionGranted));

                await ViewModel.Initialize(false);

                ViewModel.PermissionGranted.Should().Be(permissionGranted);
            }
        }

        public sealed class TheRequestAccessAction : CalendarSettingsViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void OpensAppSettings()
            {
                ViewModel.RequestAccess.Execute();

                View.Received().OpenAppSettings();
            }
        }

        public sealed class TheInitializeMethod : CalendarSettingsViewModelTest
        {
            [Property]
            public void SetsProperCalendarsAsSelected(
                NonEmptySet<NonEmptyString> strings0,
                NonEmptySet<NonEmptyString> strings1)
            {
                var enabledCalendarIds = strings0.Get.Select(str => str.Get).ToList();
                var unenabledCalendarIds = strings1.Get.Select(str => str.Get).ToList();
                var allCalendarIds = enabledCalendarIds.Concat(unenabledCalendarIds).ToList();
                UserPreferences.EnabledCalendarIds().Returns(enabledCalendarIds);
                var userCalendars = allCalendarIds
                    .Select(id => new UserCalendar(
                        id,
                        "Does not matter",
                        "Does not matter, pt.2"
                    ));
                InteractorFactory
                    .GetUserCalendars()
                    .Execute()
                    .Returns(Observable.Return(userCalendars));
                var viewModel = CreateViewModel();

                viewModel.Initialize(false).Wait();

                var calendars = viewModel.Calendars.FirstAsync().Wait();
                foreach (var calendarGroup in calendars)
                {
                    foreach (var calendar in calendarGroup.Items)
                    {
                        if (enabledCalendarIds.Contains(calendar.Id))
                            calendar.Selected.Should().BeTrue();
                    }
                }
            }

            [Fact]
            public void SetsTheEnabledCalendarsToNullWhenCalendarPermissionsWereNotGranted()
            {
                PermissionsChecker.CalendarPermissionGranted.Returns(Observable.Return(false));
                UserPreferences.EnabledCalendarIds().Returns(new List<string>());

                var viewModel = CreateViewModel();

                viewModel.Initialize(false).Wait();

                UserPreferences.Received().SetEnabledCalendars(Arg.Is<string[]>(strings => strings == null || strings.Length == 0));
            }

            [Fact]
            public void DoesNotSetTheEnabledCalendarsToNullWhenCalendarPermissionsWereGranted()
            {
                PermissionsChecker.CalendarPermissionGranted.Returns(Observable.Return(true));
                UserPreferences.EnabledCalendarIds().Returns(new List<string>());

                var viewModel = CreateViewModel();

                viewModel.Initialize(false).Wait();

                UserPreferences.DidNotReceive().SetEnabledCalendars(Arg.Is<string[]>(strings => strings == null || strings.Length == 0));
            }
        }

        public sealed class TheSelectCalendarAction : CalendarSettingsViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task StoresTheEnabledCalendarsInUserPreferences()
            {
                var firstCalendar = new UserCalendar("1", "1", "1");
                var secondCalendar = new UserCalendar("2", "2", "2");

                var observer = TestScheduler.CreateObserver<Unit>();
                ViewModel.SelectCalendar.ExecuteSequentally(
                        new SelectableUserCalendarViewModel(firstCalendar, false),
                        new SelectableUserCalendarViewModel(secondCalendar, false)
                    )
                    .Subscribe(observer);


                TestScheduler.Start();

                Received.InOrder(() =>
                {
                    UserPreferences.SetEnabledCalendars(new string[] { "1" });
                    UserPreferences.SetEnabledCalendars(new string[] { "1", "2" });
                });
            }
        }

        public sealed class TheCloseWithDefaultResultMethod : CalendarSettingsViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task SavesThePreviouslySelectedCalendarIds()
            {
                var initialSelectedIds = new List<string> { "0", "1", "2", "3" };
                UserPreferences.EnabledCalendarIds().Returns(initialSelectedIds);
                PermissionsChecker.CalendarPermissionGranted.Returns(Observable.Return(true));

                var userCalendars = Enumerable
                    .Range(0, 9)
                    .Select(id => new UserCalendar(
                        id.ToString(),
                        $"Calendar #{id}",
                        $"Source #{id % 3}",
                        false));

                InteractorFactory
                    .GetUserCalendars()
                    .Execute()
                    .Returns(Observable.Return(userCalendars));
                await ViewModel.Initialize(false);
                var selectedIds = new[] { "0", "2", "4", "7" };

                var calendars = userCalendars
                    .Where(calendar => selectedIds.Contains(calendar.Id))
                    .Select(calendar => new SelectableUserCalendarViewModel(calendar, false));

                ViewModel.SelectCalendar.ExecuteSequentally(calendars).Subscribe();
                ViewModel.CloseWithDefaultResult();

                TestScheduler.Start();

                UserPreferences.Received().SetEnabledCalendars(initialSelectedIds.ToArray());
            }
        }

        public sealed class TheDoneAction : CalendarSettingsViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task SavesTheSelectedCalendarIds()
            {
                var initialSelectedIds = new List<string> { "0" };
                UserPreferences.EnabledCalendarIds().Returns(initialSelectedIds);
                PermissionsChecker.CalendarPermissionGranted.Returns(Observable.Return(true));

                var userCalendars = Enumerable
                    .Range(0, 9)
                    .Select(id => new UserCalendar(
                        id.ToString(),
                        $"Calendar #{id}",
                        $"Source #{id % 3}",
                        false));

                InteractorFactory
                    .GetUserCalendars()
                    .Execute()
                    .Returns(Observable.Return(userCalendars));

                await ViewModel.Initialize(false);
                var selectedIds = new[] { "2", "4", "7" };

                var calendars = userCalendars
                    .Where(calendar => selectedIds.Contains(calendar.Id))
                    .Select(calendar => new SelectableUserCalendarViewModel(calendar, false));

                ViewModel.SelectCalendar.ExecuteSequentally(calendars)
                    .PrependAction(ViewModel.Save)
                    .Subscribe();

                TestScheduler.Start();

                Received.InOrder(() =>
                {
                    UserPreferences.SetEnabledCalendars(new[] { "0", "2" });
                    UserPreferences.SetEnabledCalendars(new[] { "0", "2", "4" });
                    UserPreferences.SetEnabledCalendars(new[] { "0", "2", "4", "7" });
                    UserPreferences.SetEnabledCalendars(new[] { "0", "2", "4", "7" });
                });
            }

            [Fact, LogIfTooSlow]
            public async Task DeselectsAllCalendarAfterDisablingIntegration()
            {
                var initialSelectedIds = new List<string> { "0" };
                UserPreferences.EnabledCalendarIds().Returns(initialSelectedIds);
                PermissionsChecker.CalendarPermissionGranted.Returns(Observable.Return(true));

                var userCalendars = Enumerable
                    .Range(0, 9)
                    .Select(id => new UserCalendar(
                        id.ToString(),
                        $"Calendar #{id}",
                        $"Source #{id % 3}",
                        false));

                InteractorFactory
                    .GetUserCalendars()
                    .Execute()
                    .Returns(Observable.Return(userCalendars));

                await ViewModel.Initialize(false);
                var selectedIds = new[] { "2", "4", "7" };

                var calendars = userCalendars
                    .Where(calendar => selectedIds.Contains(calendar.Id))
                    .Select(calendar => new SelectableUserCalendarViewModel(calendar, false));

                ViewModel.SelectCalendar.ExecuteSequentally(calendars)
                    .PrependAction(ViewModel.TogglCalendarIntegration)
                    .PrependAction(ViewModel.Save)
                    .Subscribe();

                TestScheduler.Start();

                Received.InOrder(() =>
                {
                    UserPreferences.SetEnabledCalendars(new[] { "0", "2" });
                    UserPreferences.SetEnabledCalendars(new[] { "0", "2", "4" });
                    UserPreferences.SetEnabledCalendars(new[] { "0", "2", "4", "7" });
                    UserPreferences.SetEnabledCalendars();
                });
            }
        }
    }
}
