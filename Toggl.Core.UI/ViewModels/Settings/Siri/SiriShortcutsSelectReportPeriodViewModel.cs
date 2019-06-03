using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Toggl.Core.DataSources;
using Toggl.Core.Interactors;
using Toggl.Core.Models;
using Toggl.Core.Models.Interfaces;
using Toggl.Core.Services;
using Toggl.Core.UI.Extensions;
using Toggl.Core.UI.Navigation;
using Toggl.Core.UI.Parameters;
using Toggl.Core.UI.ViewModels.Selectable;
using Toggl.Shared;
using Toggl.Shared.Extensions;
using Toggl.Shared.Extensions.Reactive;

namespace Toggl.Core.UI.ViewModels.Settings
{

    public sealed class SiriShortcutsSelectReportPeriodViewModel : ViewModel
    {
        private readonly IInteractorFactory interactorFactory;

        public readonly BehaviorRelay<IThreadSafeWorkspace> SelectedWorkspace = new BehaviorRelay<IThreadSafeWorkspace>(null);
        public readonly BehaviorRelay<ReportPeriod> SelectReportPeriod = new BehaviorRelay<ReportPeriod>(ReportPeriod.Today);
        public IObservable<IEnumerable<SelectableReportPeriodViewModel>> ReportPeriods { get; }
        public UIAction Close { get; }
        public UIAction PickWorkspace { get; }

        public IObservable<string> WorkspaceName { get; }

        public SiriShortcutsSelectReportPeriodViewModel(
            ITogglDataSource dataSource,
            IInteractorFactory interactorFactory,
            IRxActionFactory rxActionFactory,
            ISchedulerProvider schedulerProvider,
            INavigationService navigationService) : base(navigationService)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));
            Ensure.Argument.IsNotNull(schedulerProvider, nameof(schedulerProvider));

            this.interactorFactory = interactorFactory;

            Close = rxActionFactory.FromAsync(Finish);
            PickWorkspace = rxActionFactory.FromAsync(pickWorkspace);

            var reportPeriods = Enum.GetValues(typeof(ReportPeriod))
                .Cast<ReportPeriod>()
                .Where(p => p != ReportPeriod.Unknown)
                .ToImmutableList();

            ReportPeriods = SelectReportPeriod
                .Select(selectedPeriod => reportPeriods.Select(p => new SelectableReportPeriodViewModel(p, p == selectedPeriod)))
                .AsDriver(new SelectableReportPeriodViewModel[0], schedulerProvider);

            WorkspaceName = SelectedWorkspace
                .Where(ws => ws != null)
                .Select(workspace => workspace.Name)
                .AsDriver(schedulerProvider);
        }

        public override async Task Initialize()
        {
            await base.Initialize();

            var defaultWorkspace = await interactorFactory.GetDefaultWorkspace().Execute();
            SelectedWorkspace.Accept(defaultWorkspace);
        }

        private async Task pickWorkspace()
        {
            var defaultWorkspace = await interactorFactory.GetDefaultWorkspace()
                .TrackException<InvalidOperationException, IThreadSafeWorkspace>(
                    "SiriShortcutsSelectReportPeriodViewModel.PickWorkspace")
                .Execute();

            var selectWorkspaceParams = new SelectWorkspaceParameters(Resources.SelectWorkspace, SelectedWorkspace.Value?.Id ?? defaultWorkspace.Id);
            var selectedWorkspaceId =
                await Navigate<SelectWorkspaceViewModel, SelectWorkspaceParameters, long>(selectWorkspaceParams);
            var workspace = await interactorFactory.GetWorkspaceById(selectedWorkspaceId).Execute();

            SelectedWorkspace.Accept(workspace);
        }
    }
}
