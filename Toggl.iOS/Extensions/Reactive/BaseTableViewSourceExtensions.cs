using CoreGraphics;
using System;
using System.Reactive.Linq;
using Toggl.Core.UI.Collections;
using Toggl.Core.UI.Reactive;
using Toggl.iOS.ViewSources;

namespace Toggl.iOS.Extensions.Reactive
{
    public static class BaseTableViewSourceExtensions
    {
        public static IObservable<TModel> ModelSelected<TSection, THeader, TModel>(
            this IReactive<BaseTableViewSource<TSection, THeader, TModel>> reactive)
        where TSection : ISectionModel<THeader, TModel>, new()
            => Observable
                .FromEventPattern<TModel>(e => reactive.Base.OnItemTapped += e, e => reactive.Base.OnItemTapped -= e)
                .Select(e => e.EventArgs);

        public static IObservable<CGPoint> Scrolled<TSection, THeader, TModel>(
            this IReactive<BaseTableViewSource<TSection, THeader, TModel>> reactive)
            where TSection : ISectionModel<THeader, TModel>, new()
            => Observable
                .FromEventPattern<CGPoint>(e => reactive.Base.OnScrolled += e, e => reactive.Base.OnScrolled -= e)
                .Select(e => e.EventArgs);
    }
}
