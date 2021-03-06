﻿using Windows.ApplicationModel.Background;
using Microsoft.Toolkit.Uwp.Helpers;
using MoneyFox.Foundation.Interfaces;

namespace MoneyFox.Windows
{
    /// <inheritdoc />
    public class BackgroundTaskManager : IBackgroundTaskManager
    {
        /// <inheritdoc />
        public void StartBackupSyncTask(int interval)
        {
            BackgroundTaskHelper.Register("SyncBackgroundTask", new TimeTrigger((uint)interval, false), true, false, new SystemCondition(SystemConditionType.InternetAvailable));
        }

        /// <inheritdoc />
        public void StopBackupSyncTask()
        {
            BackgroundTaskHelper.Unregister(typeof(SyncBackupTask));
        }
    }
}