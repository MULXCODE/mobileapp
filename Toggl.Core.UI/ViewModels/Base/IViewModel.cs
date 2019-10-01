using Toggl.Core.UI.Views;

namespace Toggl.Core.UI.ViewModels
{
    public interface IViewModel
    {
        IView View { get; }

        void AttachView(IView viewToAttach);
        void DetachView();

        void ViewAppeared();
        void ViewAppearing();
        void ViewDisappearing();
        void ViewDisappeared();
        void ViewDestroyed();

        void CloseWithDefaultResult();
    }
}
