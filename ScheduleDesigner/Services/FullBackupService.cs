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
    /// <summary>
    /// Klasa serwisu uruchamiającego utworzenie pełnej kopii zapasowej bazy danych.
    /// </summary>
    public class FullBackupService : IHostedService
    {
        /// <summary>
        /// Informacje o wykonaniu pełnej kopii zapasowej.
        /// </summary>
        private readonly FullBackup _fullBackup;
        
        /// <summary>
        /// Informacje o połączeniu z bazą danych.
        /// </summary>
        private readonly DatabaseConnectionOptions _databaseConnectionOptions;

        /// <summary>
        /// Mechanizm wykorzystany do uruchamiania głównej metody serwisu <see cref="DoBackup(object)"/> 
        /// w odpowiednich przedziałach czasowych zapisanych w stałej <see cref="_fullBackup"/>. 
        /// </summary>
        private Timer _timer = null;

        /// <summary>
        /// Konstruktor serwisu wykorzystujący wstrzykiwanie zależności.
        /// </summary>
        /// <param name="fullBackup">Wstrzyknięta instancja konfiguracji tworzenia pełnej kopii zapasowej</param>
        /// <param name="databaseConnectionOptions">Wstrzyknięta instancja konfiguracji połączenia z bazą danych</param>
        public FullBackupService(IOptions<FullBackup> fullBackup, IOptions<DatabaseConnectionOptions> databaseConnectionOptions)
        {
            _fullBackup = fullBackup.Value;
            _databaseConnectionOptions = databaseConnectionOptions.Value;
        }

        /// <summary>
        /// Metoda odpowiadająca za wykonanie pełnej kopii zapasowej bazy danych.
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

        /// <summary>
        /// Funkcja odpowiadająca za wystartowanie serwisu.
        /// </summary>
        /// <param name="cancellationToken">Wskazuje, że rozpoczęty proces został przerwany</param>
        /// <returns>Asynchroniczną operację</returns>
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
