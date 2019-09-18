using System;
using System.Collections.Immutable;
using System.Linq;
using Toggl.Core.UI.Collections;
using Toggl.Core.UI.Reactive;
using Toggl.Droid.Adapters;

namespace Toggl.Droid.Extensions.Reactive
{
    public static class RecyclerAdapterExtensions
    {
        public static Action<IImmutableList<T>> Items<T>(this IReactive<BaseRecyclerAdapter<T>> reactive)
            where T : class, IEquatable<T>
            => collection => reactive.Base.SubmitList(collection.ToList());

        public static Action<IImmutableList<SectionModel<TSection, TItem>>> Items<TSection, TItem>(this IReactive<BaseSectionedRecyclerAdapter<TSection, TItem>> reactive)
            where TSection : IEquatable<TSection>
            where TItem : IEquatable<TItem>
            => collection => reactive.Base.Items = collection;
    }
}
