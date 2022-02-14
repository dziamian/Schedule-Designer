using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Data.SqlClient;
using System.Threading;
using ScheduleDesigner.Helpers;
using System;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using System.Threading.Tasks;

namespace ScheduleDesigner.Services
{
    /// <summary>
    /// Klasa serwisu uruchamiającego utworzenie różnicowej kopii zapasowej bazy danych.
    /// </summary>
    public class DifferentialBackupService : IHostedService
    {
        /// <summary>
        /// Informacje o wykonaniu różnicowej kopii zapasowej.
        /// </summary>
        private readonly DifferentialBackup _differentialBackup;

        /// <summary>
        /// Informacje o połączeniu z bazą danych.
        /// </summary>
        private readonly DatabaseConnectionOptions _databaseConnectionOptions;

        /// <summary>
        /// Mechanizm wykorzystany do uruchamiania głównej metody serwisu <see cref="DoBackup(object)"/> 
        /// w odpowiednich przedziałach czasowych zapisanych w stałej <see cref="_differentialBackup"/>. 
        /// </summary>
        private Timer _timer = null;

        /// <summary>
        /// Konstruktor serwisu wykorzystujący wstrzykiwanie zależności.
        /// </summary>
        /// <param name="differentialBackup">Wstrzyknięta instancja konfiguracji tworzenia różnicowej kopii zapasowej</param>
        /// <param name="databaseConnectionOptions">Wstrzyknięta instancja konfiguracji połączenia z bazą danych</param>
        public DifferentialBackupService(IOptions<DifferentialBackup> differentialBackup, IOptions<DatabaseConnectionOptions> databaseConnectionOptions)
        {
            _differentialBackup = differentialBackup.Value;
            _databaseConnectionOptions = databaseConnectionOptions.Value;
        }

        /// <summary>
        /// Metoda odpowiadająca za wykonanie różnicowej kopii zapasowej bazy danych.
        /// </summary>
        /// <param name="state">Obiekt przechowujący informacje do użycia przez metodę - jego wartość jest zawsze równa null</param>
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
                BackupSetDescription = $"Differential backup of {databaseName} on {shortDateString} {shortTimeString}",
                Database = databaseName
            };

            var destinationPath = _differentialBackup.Path;
            var backupFileName = $"{databaseName}_{shortDateString}_{shortTimeString}.bak";
            var backupDeviceItem = new BackupDeviceItem(destinationPath + "\\" + backupFileName, DeviceType.File);

            var connection = new ServerConnection(new SqlConnection(connectionBuilder.ConnectionString));
            var sqlServer = new Server(connection);

            sqlServer.ConnectionContext.StatementTimeout = 60 * 60;

            sqlBackup.Initialize = false;
            sqlBackup.Devices.Add(backupDeviceItem);
            sqlBackup.Incremental = true;
            sqlBackup.ExpirationDate = currentTime.AddDays(7);

            sqlBackup.SqlBackupAsync(sqlServer);
        }

        /// <summary>
        /// Funkcja odpowiadająca za wystartowanie serwisu.
        /// </summary>
        /// <param name="cancellationToken">Wskazuje, że rozpoczęty proces został przerwany</param>
        /// <returns>Asynchroniczną operację</returns>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            TimeSpan scheduleInterval = TimeSpan.FromHours(_differentialBackup.IntervalHours);

            var nextRunTime = DateTime.Today.AddHours(_differentialBackup.StartHour);
            while (nextRunTime < DateTime.Now)
            {
                nextRunTime = nextRunTime.Add(scheduleInterval);
            }
            var firstFullInterval = nextRunTime.Subtract(DateTime.Now);

            void action()
            {
                var task = Task.Delay(firstFullInterval);
                task.Wait();

                _timer = new Timer(DoBackup, null, TimeSpan.Zero, scheduleInterval);
            }

            Task.Run(action);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Funkcja odpowiadająca za zatrzymanie serwisu.
        /// </summary>
        /// <param name="cancellationToken">Wskazuje, że rozpoczęty proces został przerwany</param>
        /// <returns>Asynchroniczną operację</returns>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }
    }
}
