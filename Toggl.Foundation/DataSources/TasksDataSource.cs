﻿using System;
using System.Collections.Generic;
using Toggl.Multivac;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.DataSources
{
    public sealed class TasksDataSource : ITasksSource
    {
        private readonly IRepository<IDatabaseTask> repository;

        public TasksDataSource(IRepository<IDatabaseTask> repository)
        {
            Ensure.Argument.IsNotNull(repository, nameof(repository));

            this.repository = repository;
        }

        public IObservable<IEnumerable<IDatabaseTask>> GetAll()
            => repository.GetAll();

        public IObservable<IEnumerable<IDatabaseTask>> GetAll(Func<IDatabaseTask, bool> predicate)
            => repository.GetAll(predicate);

        public IObservable<IDatabaseTask> GetById(long id)
            => repository.GetById(id);
    }
}
