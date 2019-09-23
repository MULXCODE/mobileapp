using Android.Runtime;
using Android.Support.V7.Util;
using Android.Support.V7.Widget;
using Android.Support.V7.RecyclerView.Extensions;
using Android.Views;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Toggl.Core.UI.Interfaces;
using Toggl.Droid.Adapters.DiffingStrategies;
using Toggl.Droid.Extensions;
using Toggl.Droid.ViewHolders;
using Toggl.Shared.Extensions;
using JavaObject = Java.Lang.Object;

namespace Toggl.Droid.Adapters
{
    public abstract class BaseRecyclerAdapter<T> : ListAdapter
        where T : class, IEquatable<T>
    {
        public IObservable<T> ItemTapObservable => itemTapSubject.AsObservable();

        private readonly Subject<T> itemTapSubject = new Subject<T>();

        public IImmutableList<T> Items
        {
            set => SubmitList(value.ToList());
        }

        protected BaseRecyclerAdapter(IDiffingStrategy<T> diffingStrategy = null)
            : base(itemCallbackFrom(diffingStrategy))
        {
        }

        protected BaseRecyclerAdapter(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var inflater = LayoutInflater.From(parent.Context);
            var viewHolder = CreateViewHolder(parent, inflater, viewType);
            viewHolder.TappedSubject = itemTapSubject;
            return viewHolder;
        }

        protected abstract BaseRecyclerViewHolder<T> CreateViewHolder(
            ViewGroup parent,
            LayoutInflater inflater,
            int viewType);

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            ((BaseRecyclerViewHolder<T>)holder).Item = GetTypedItem(position);
        }

        protected T GetTypedItem(int position)
        {
            return GetItem(position).Cast<T>();
        }

        private static DiffingStrategyItemCallback<T> itemCallbackFrom(IDiffingStrategy<T> diffingStrategy)
        {
            if (diffingStrategy == null)
            {
                if (typeof(T).ImplementsOrDerivesFrom<IDiffableByIdentifier<T>>())
                {
                    diffingStrategy = new IdentifierEqualityDiffingStrategy<T>();
                }
                else
                {
                    diffingStrategy = new EquatableDiffingStrategy<T>();
                }
            }

            return new DiffingStrategyItemCallback<T>(diffingStrategy);
        }
    }

    public sealed class DiffingStrategyItemCallback<T> : DiffUtil.ItemCallback
        where T : class, IEquatable<T>
    {
        private readonly IDiffingStrategy<T> diffingStrategy;

        public DiffingStrategyItemCallback(IDiffingStrategy<T> diffingStrategy)
        {
            this.diffingStrategy = diffingStrategy;
        }

        public override bool AreContentsTheSame(JavaObject oldItem, JavaObject newItem)
            => diffingStrategy.AreContentsTheSame(oldItem.Cast<T>(), newItem.Cast<T>());

        public override bool AreItemsTheSame(JavaObject oldItem, JavaObject newItem)
            => diffingStrategy.AreItemsTheSame(oldItem.Cast<T>(), newItem.Cast<T>());
    }
}
