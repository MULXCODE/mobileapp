﻿using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Toggl.Core.DataSources.Interfaces;
using Toggl.Core.Extensions;
using Toggl.Core.Models;
using Toggl.Core.Models.Interfaces;
using Toggl.Core.Sync;
using Toggl.Shared.Extensions;
using Toggl.Storage.Models;

namespace Toggl.Core.Interactors
{
    public class SoftDeleteMultipleTimeEntriesInteractor : IInteractor<IObservable<Unit>>
    {
        private readonly IInteractorFactory interactorFactory;
        private readonly IDataSource<IThreadSafeTimeEntry, IDatabaseTimeEntry> dataSource;
        private readonly ISyncManager syncManager;
        private readonly long[] ids;

        public SoftDeleteMultipleTimeEntriesInteractor(
            IDataSource<IThreadSafeTimeEntry, IDatabaseTimeEntry> dataSource,
            ISyncManager syncManager,
            IInteractorFactory interactorFactory,
            long[] ids)
        {
            this.interactorFactory = interactorFactory;
            this.dataSource = dataSource;
            this.syncManager = syncManager;
            this.ids = ids;
        }

        public IObservable<Unit> Execute()
            => interactorFactory.GetMultipleTimeEntriesById(ids)
                .Execute()
                .Select(timeEntries => timeEntries.Select(TimeEntry.DirtyDeleted))
                .SelectMany(dataSource.BatchUpdate)
                .SingleAsync()
                .Do(syncManager.InitiatePushSync)
                .SelectUnit();
    }
}
