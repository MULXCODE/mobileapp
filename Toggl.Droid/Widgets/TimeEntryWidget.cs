using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.OS;
using Android.Widget;
using Toggl.Droid.Services;
using Toggl.Droid.Widgets.Services;

namespace Toggl.Droid.Widgets
{
    [BroadcastReceiver(Label = "Toggl Time Entry Widget", Exported = true)]
    [IntentFilter(new string[] { AppWidgetManager.ActionAppwidgetUpdate })]
    [MetaData("android.appwidget.provider", Resource = "@xml/timeentrywidgetprovider")]
    public class TimeEntryWidget : AppWidgetProvider
    {
        public override void OnReceive(Context context, Intent intent)
        {
            base.OnReceive(context, intent);

            var action = intent.Action;
            if (action == TimerBackgroundService.StartTimeEntryAction)
            {
                TimerBackgroundService.EnqueueStartTimeEntry(context, intent);
            }
            else if (action == TimerBackgroundService.StopRunningTimeEntryAction)
            {
                TimerBackgroundService.EnqueueStopTimeEntry(context, intent);
            }
        }

        public override void OnDeleted(Context context, int[] appWidgetIds)
        {
            reportInstallationState(context, false);
            base.OnDeleted(context, appWidgetIds);
        }

        public override void OnEnabled(Context context)
        {
            reportInstallationState(context, true);
            base.OnEnabled(context);
        }

        public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
        {
            base.OnUpdate(context, appWidgetManager, appWidgetIds);
        }

        public override void OnAppWidgetOptionsChanged(Context context, AppWidgetManager appWidgetManager, int appWidgetId, Bundle newOptions)
        {
            base.OnAppWidgetOptionsChanged(context, appWidgetManager, appWidgetId, newOptions);
            var minWidth = newOptions.GetInt(AppWidgetManager.OptionAppwidgetMinWidth);
            appWidgetManager.UpdateAppWidget(appWidgetId, getRemoteViews(context, minWidth));
        }

        private void reportInstallationState(Context context, bool installed)
        {
            var intent = new Intent(context, typeof(WidgetsAnalyticsService));
            intent.PutExtra(WidgetsAnalyticsService.StateParameterName, installed);
            WidgetsAnalyticsService.EnqueueTrackTimerWidgetInstallState(context, intent);
        }

        private RemoteViews getRemoteViews(Context context, int minWidth)
        {
            var remoteViews = minWidth < 110
                ? new RemoteViews(context.PackageName, Resource.Layout.TimeEntryWidgetSmall)
                : new RemoteViews(context.PackageName, Resource.Layout.TimeEntryWidget);

            remoteViews.SetOnClickPendingIntent(Resource.Id.StartButton, startTimeEntryPendingIntent(context));
            remoteViews.SetOnClickPendingIntent(Resource.Id.StopButton, stopTimeEntryPendingIntent(context));

            return remoteViews;
        }

        private PendingIntent startTimeEntryPendingIntent(Context context)
        {
            var intent = new Intent(context, typeof(TimeEntryWidget));
            intent.SetAction(TimerBackgroundService.StartTimeEntryAction);
            return PendingIntent.GetBroadcast(context, 0, intent, PendingIntentFlags.UpdateCurrent);
        }

        private PendingIntent stopTimeEntryPendingIntent(Context context)
        {
            var intent = new Intent(context, typeof(TimeEntryWidget));
            intent.SetAction(TimerBackgroundService.StopRunningTimeEntryAction);
            return PendingIntent.GetBroadcast(context, 0, intent, PendingIntentFlags.UpdateCurrent);
        }
    }
}
