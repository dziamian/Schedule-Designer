using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Data.SqlClient;
using System.Globalization;
using System.Threading;
using static ScheduleDesigner.Helpers;
using System;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using System.Collections.Generic;

namespace ScheduleDesigner.Services
{
    public class ScheduleBackupService : IHostedService
    {
        private readonly ScheduleBackup _scheduleBackup;
        private readonly DatabaseConnectionOptions _databaseConnectionOptions;

        private Timer _timer = null;

        private readonly List<string> tables = new List<string> { "SchedulePositions", "ScheduledMoves", "ScheduledMovePositions", "Messages" };

        public ScheduleBackupService(IOptions<ScheduleBackup> scheduleBackup, IOptions<DatabaseConnectionOptions> databaseConnectionOptions)
        {
            _scheduleBackup = scheduleBackup.Value;
            _databaseConnectionOptions = databaseConnectionOptions.Value;
        }

        private void DoBackup(object state)
        {
            var connectionBuilder = new SqlConnectionStringBuilder(_databaseConnectionOptions.SchedulingDatabase);

            var currentTime = DateTime.Now;
            var shortDateString = currentTime.ToShortDateString();
            var shortTimeString = currentTime.ToString("HH-mm");

            var connection = new ServerConnection(new SqlConnection(connectionBuilder.ConnectionString));
            var sqlServer = new Server(connection);
            var database = sqlServer.Databases[connectionBuilder.InitialCatalog];

            var scriptingOptions = new ScriptingOptions
            {
                ScriptData = true,
                ScriptDrops = false,
                FileName = $"{_scheduleBackup.Path}\\ScheduleScript_{shortDateString}_{shortTimeString}.sql",
                EnforceScriptingOptions = true,
                ScriptSchema = true,
                IncludeHeaders = true,
                AppendToFile = true,
                Indexes = true,
                WithDependencies = true
            };

            foreach (var table in tables)
            {
                database.Tables[table].EnumScript(scriptingOptions);
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            TimeSpan scheduleInterval = TimeSpan.FromSeconds(_scheduleBackup.IntervalHours);

            var nextRunTime = DateTime.Now.AddSeconds(_scheduleBackup.StartHour);
            while (nextRunTime < DateTime.Now)
            {
                nextRunTime = nextRunTime.Add(scheduleInterval);
            }
            var firstFullInterval = nextRunTime.Subtract(DateTime.Now);

            void action()
            {
                var task = Task.Delay(firstFullInterval);
                task.Wait();

                DoBackup(null);
                //_timer = new Timer(DoBackup, null, TimeSpan.Zero, scheduleInterval);
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
