using Android.Support.V7.Widget;
using Android.Widget;
using Toggl.Droid.Adapters;

namespace Toggl.Droid.Activities
{
    public partial class SelectClientActivity
    {
        private readonly SelectClientRecyclerAdapter selectClientRecyclerAdapter = new SelectClientRecyclerAdapter();

        private EditText filterEditText;
        private RecyclerView selectClientRecyclerView;

        protected override void InitializeViews()
        {
            filterEditText = FindViewById<EditText>(Resource.Id.FilterEditText);
            selectClientRecyclerView = FindViewById<RecyclerView>(Resource.Id.SelectClientRecyclerView);

            filterEditText.Hint = Shared.Resources.AddClient;
            selectClientRecyclerView.SetLayoutManager(new LinearLayoutManager(this)
            {
                ItemPrefetchEnabled = true,
                InitialPrefetchItemCount = 4
            });
            selectClientRecyclerView.SetAdapter(selectClientRecyclerAdapter);
            
            SetupToolbar();
        }
    }
}
