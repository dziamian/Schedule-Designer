using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using Microsoft.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using ScheduleDesigner.Helpers;

namespace ScheduleDesigner.Services
{
    public class FullBackupService : IHostedService
    {
        private readonly FullBackup _fullBackup;
        private readonly DatabaseConnectionOptions _databaseConnectionOptions;

        private Timer _timer = null;

        public FullBackupService(IOptions<FullBackup> fullBackup, IOptions<DatabaseConnectionOptions> databaseConnectionOptions)
        {
            _fullBackup = fullBackup.Value;
            _databaseConnectionOptions = databaseConnectionOptions.Value;
        }

        private void DoBackup(object state)
        {
            var connectionBuilder = new SqlConnectionStringBuilder(_databaseConnectionOptions.SchedulingDatabase);

            string databaseName = connectionBuilder.InitialCatalog;

            var currentTime = DateTime.Now;
            var shortDateString = currentTime.ToString("dd_M_yyyy");
            var shortTimeString = currentTime.ToString("HHmm");

            var sqlBackup = new Backup
            {
                Action = BackupActionType.Database,
                BackupSetName = "ScheduleDesigner Backup",
                BackupSetDescription = $"Full backup of {databaseName} on {shortDateString} {shortTimeString}",
                Database = databaseName
            };

            var destinationPath = _fullBackup.Path;
            var backupFileName = $"{databaseName}_{shortDateString}_{shortTimeString}.bak";
            var backupDeviceItem = new BackupDeviceItem(destinationPath + "\\" + backupFileName, DeviceType.File);

            var connection = new ServerConnection(new SqlConnection(connectionBuilder.ConnectionString));
            var sqlServer = new Server(connection);
            
            sqlServer.ConnectionContext.StatementTimeout = 60 * 60;
            
            sqlBackup.Initialize = false;
            sqlBackup.Devices.Add(backupDeviceItem);
            sqlBackup.Incremental = false;
            sqlBackup.ExpirationDate = currentTime.AddDays(7);
            sqlBackup.LogTruncation = BackupTruncateLogType.Truncate;
            
            sqlBackup.SqlBackupAsync(sqlServer);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            TimeSpan fullInterval = TimeSpan.FromHours(_fullBackup.IntervalHours);
            
            var nextRunTime = DateTime.Today.AddHours(_fullBackup.StartHour);
            while (nextRunTime < DateTime.Now)
            {
                nextRunTime = nextRunTime.Add(fullInterval);
            }
            var firstFullInterval = nextRunTime.Subtract(DateTime.Now);

            void action()
            {
                var task = Task.Delay(firstFullInterval);
                task.Wait();

                _timer = new Timer(DoBackup, null, TimeSpan.Zero, fullInterval);
            }

            Task.Run(action);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            
            return Task.CompletedTask;
        }
    }
}
