using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Support.V4.App;
using static Toggl.Droid.Services.JobServicesConstants;

namespace Toggl.Droid.Widgets.Services
{
    [Service(Permission = "android.permission.BIND_JOB_SERVICE", Exported = true)]
    class WidgetsAnalyticsService : JobIntentService
    {
        public const string TrackTimerWidgetInstallAction = nameof(TrackTimerWidgetInstallAction);
        public const string TrackTimerWidgetResizeAction = nameof(TrackTimerWidgetResizeAction);

        public const string TimerWidgetInstallStateParameter = nameof(TimerWidgetInstallStateParameter);
        public const string TimerWidgetSizeParameter = nameof(TimerWidgetSizeParameter);

        public static void EnqueueTrackTimerWidgetInstallState(Context context, Intent intent)
        {
            var serviceClass = Java.Lang.Class.FromType(typeof(WidgetsAnalyticsService));
            EnqueueWork(context, serviceClass, TimerWidgetInstallStateReportingJobId, intent);
        }

        public static void EnqueueTrackTimerWidgetResize(Context context, Intent intent)
        {
            var serviceClass = Java.Lang.Class.FromType(typeof(WidgetsAnalyticsService));
            EnqueueWork(context, serviceClass, TimerWidgetResizeReportingJobId, intent);
        }

        protected override void OnHandleWork(Intent intent)
        {
            var action = intent.Action;
            switch (action)
            {
                case TrackTimerWidgetInstallAction:
                    handleTrackTimerWidgetInstallState(intent);
                    break;
                case TrackTimerWidgetResizeAction:
                    handleTrackTimerWidgetResize(intent);
                    break;
                default:
                    throw new InvalidOperationException($"Cannot handle intent with action {action}");
            }
        }

        private void handleTrackTimerWidgetInstallState(Intent intent)
        {
            var installationState = intent.GetBooleanExtra(TimerWidgetInstallStateParameter, false);
            var analyticsService = AndroidDependencyContainer.Instance.AnalyticsService;
            analyticsService.TimerWidgetInstallStateChange.Track(installationState);
        }

        private void handleTrackTimerWidgetResize(Intent intent)
        {
            var widgetSize = intent.GetIntExtra(TimerWidgetSizeParameter, 1);
            var analyticsService = AndroidDependencyContainer.Instance.AnalyticsService;
            analyticsService.TimerWidgetSizeChanged.Track(widgetSize);
        }
    }
}
