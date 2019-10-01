using Foundation;
using ObjCRuntime;
using System;
using UIKit;

namespace Toggl.iOS.Views
{
    public sealed partial class TimeEntriesEmptyLogView : UIView
    {
        public TimeEntriesEmptyLogView(IntPtr handle) : base(handle)
        {
        }

        public static TimeEntriesEmptyLogView Create()
        {
            var arr = NSBundle.MainBundle.LoadNib(nameof(TimeEntriesEmptyLogView), null, null);
            return Runtime.GetNSObject<TimeEntriesEmptyLogView>(arr.ValueAt(0));
        }
    }
}
