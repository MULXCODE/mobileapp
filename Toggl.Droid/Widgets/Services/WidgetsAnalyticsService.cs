using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Support.V4.App;
using Toggl.Droid.Services;

namespace Toggl.Droid.Widgets.Services
{
    [Service(Permission = "android.permission.BIND_JOB_SERVICE", Exported = true)]
    class WidgetsAnalyticsService : JobIntentService
    {
        public const string StateParameterName = nameof(StateParameterName);

        public static void EnqueueTrackTimerWidgetInstallState(Context context, Intent intent)
        {
            var serviceClass = Java.Lang.Class.FromType(typeof(WidgetsAnalyticsService));
            EnqueueWork(context, serviceClass, JobServicesConstants.TimerWidgetInstallStateReportingJobId, intent);
        }

        protected override void OnHandleWork(Intent intent)
        {
            handleTrackTimerWidgetInstallState(intent);
        }

        private void handleTrackTimerWidgetInstallState(Intent intent)
        {
            var installationState = intent.GetBooleanExtra(StateParameterName, false);
            var analyticsService = AndroidDependencyContainer.Instance.AnalyticsService;
            analyticsService.TimerWidgetInstallStateChange.Track(installationState);
        }
    }
}
