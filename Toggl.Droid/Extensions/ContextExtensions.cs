using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Support.V4.Content;

namespace Toggl.Droid.Extensions
{
    public static class ContextExtensions
    {
        public static VectorDrawable GetVectorDrawable(this Context context, int resourceId)
            => ContextCompat.GetDrawable(context, resourceId) as VectorDrawable;

        public static Color SafeGetColor(this Context context, int resourceId)
            => new Color(ContextCompat.GetColor(context, resourceId));        
    }
}
