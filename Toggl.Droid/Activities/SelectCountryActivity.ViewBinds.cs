using Android.Support.V7.Widget;
using Android.Widget;
using Toggl.Core.UI.ViewModels;
using Toggl.Droid.Adapters;
using Toggl.Droid.ViewHolders.Country;

namespace Toggl.Droid.Activities
{
    public partial class SelectCountryActivity
    {
        private readonly SimpleAdapter<SelectableCountryViewModel> recyclerAdapter =
            new SimpleAdapter<SelectableCountryViewModel>(
                Resource.Layout.SelectCountryActivityCountryCell,
                CountrySelectionViewHolder.Create);

        private EditText filterEditText;
        private RecyclerView recyclerView;

        protected override void InitializeViews()
        {
            filterEditText = FindViewById<EditText>(Resource.Id.FilterEditText);
            recyclerView = FindViewById<RecyclerView>(Resource.Id.RecyclerView);

            filterEditText.Hint = Shared.Resources.SelectCountry;
            recyclerView.SetLayoutManager(new LinearLayoutManager(this)
            {
                ItemPrefetchEnabled = true,
                InitialPrefetchItemCount = 4
            });
            recyclerView.SetAdapter(recyclerAdapter);
            
            SetupToolbar();
        }
    }
}
