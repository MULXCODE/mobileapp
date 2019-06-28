using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Toggl.Core.Extensions;
using Toggl.Core.Services;
using Toggl.Core.UI.Navigation;
using Toggl.Core.UI.ViewModels.Selectable;
using Toggl.Shared;
using Toggl.Shared.Extensions;
using Toggl.Storage.Settings;

namespace Toggl.Core.UI.ViewModels.Settings
{
    [Preserve(AllMembers = true)]
    public sealed class UpcomingEventsNotificationSettingsViewModel : ViewModel
    {
        private readonly IUserPreferences userPreferences;

        public IList<SelectableCalendarNotificationsOptionViewModel> AvailableOptions { get; }
        public InputAction<SelectableCalendarNotificationsOptionViewModel> SelectOption { get; }

        public UpcomingEventsNotificationSettingsViewModel(
            INavigationService navigationService,
            IUserPreferences userPreferences,
            IRxActionFactory rxActionFactory)
            : base(navigationService)
        {
            Ensure.Argument.IsNotNull(userPreferences, nameof(userPreferences));
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));

            this.userPreferences = userPreferences;

            var options = new[]
            {
                CalendarNotificationsOption.Disabled,
                CalendarNotificationsOption.WhenEventStarts,
                CalendarNotificationsOption.FiveMinutes,
                CalendarNotificationsOption.TenMinutes,
                CalendarNotificationsOption.FifteenMinutes,
                CalendarNotificationsOption.ThirtyMinutes,
                CalendarNotificationsOption.OneHour
            };

            AvailableOptions = options.Select(toSelectableOption).ToList();

            SelectOption = rxActionFactory.FromAction<SelectableCalendarNotificationsOptionViewModel>(onSelectOption);
        }

        public override async Task Initialize()
        {
            await base.Initialize();
            var selectedOption = await userPreferences.CalendarNotificationsSettings().FirstAsync();
            AvailableOptions.ForEach(opt => opt.Selected = opt.Option == selectedOption);
        }

        private void onSelectOption(SelectableCalendarNotificationsOptionViewModel selectableOption)
        {
            var enabled = selectableOption.Option != CalendarNotificationsOption.Disabled;

            userPreferences.SetCalendarNotificationsEnabled(enabled);

            if (enabled)
                userPreferences.SetTimeSpanBeforeCalendarNotifications(selectableOption.Option.Duration());

            Close();
        }

        private SelectableCalendarNotificationsOptionViewModel toSelectableOption(CalendarNotificationsOption option)
            => new SelectableCalendarNotificationsOptionViewModel(option, false);
    }
}
