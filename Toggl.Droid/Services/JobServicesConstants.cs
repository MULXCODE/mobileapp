namespace Toggl.Droid.Services
{
    public static class JobServicesConstants
    {
        public const int SyncJobServiceJobId = 1111;
        public const int BackgroundSyncJobServiceJobId = 1;
        public const int TimerWidgetInstallStateReportingJobId = 1001;
        public const int TimerWidgetStartTimeEntryJobId = 1002;
        public const int TimerWidgetStopRunningTimeEntryJobId = 1003;
        public const int TimerWidgetResizeReportingJobId = 1004;

        public const string HasPendingSyncJobServiceScheduledKey = "HasPendingSyncJobServiceScheduledKey";
        public const string LastSyncJobScheduledAtKey = "LastSyncJobScheduledAtKey";
    }
}
