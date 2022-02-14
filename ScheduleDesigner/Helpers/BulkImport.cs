using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Helpers
{
    /// <summary>
    /// Klasa przeznaczona do importowania dużej ilości danych do bazy.
    /// </summary>
    /// <typeparam name="T">Klasa, dla której dane będą importowane</typeparam>
    public static class BulkImport<T> where T : class
    {
        /// <summary>
        /// Funkcja odczytująca dane z pliku CSV.
        /// </summary>
        /// <param name="file">Reprezentacja pliku CSV</param>
        /// <param name="delimiter">Ogranicznik między poszczególnymi danymi w wierszu pliku</param>
        /// <returns>Asynchroniczną operację przechowującą listę odczytanych rekordów z pliku</returns>
        public static async Task<List<T>> ReadCsv(IFormFile file, string delimiter = "|")
        {
            using var memoryStream = new MemoryStream(new byte[file.Length]);
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            using var reader = new StreamReader(memoryStream);
            using var csvReader = new CsvReader(reader, new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
            {
                Delimiter = delimiter
            });
            return csvReader.GetRecords<T>().ToList();
        }

        /// <summary>
        /// Funkcja importująca dużą ilość danych do bazy.
        /// </summary>
        /// <param name="connectionString">Wyrażenie wymagane do połączenia się z bazą danych</param>
        /// <param name="destinationTable">Nazwa tabeli w bazie danych, do której mają zostać załadowane dane</param>
        /// <param name="records">Lista rekordów do zaimportowania</param>
        /// <returns>Liczbę zaimportowanych rekordów do bazy</returns>
        public static int Execute(string connectionString, string destinationTable, List<T> records)
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                using var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.KeepIdentity, transaction);
                bulkCopy.DestinationTableName = destinationTable;

                var properties = records.FirstOrDefault()?.GetType()?.GetProperties();
                if (properties == null)
                {
                    return 0;
                }

                var dataTable = new DataTable();
                foreach (var property in properties)
                {
                    var propertyName = property.Name;
                    
                    bulkCopy.ColumnMappings.Add(propertyName, propertyName);
                    
                    dataTable.Columns.Add(new DataColumn(propertyName, Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType));
                }

                foreach (var record in records)
                {
                    var row = dataTable.NewRow();
                    foreach (var property in properties)
                    {
                        row[property.Name] = property.GetValue(record) ?? DBNull.Value;
                    }
                    dataTable.Rows.Add(row);
                }
                bulkCopy.WriteToServer(dataTable);
                transaction.Commit();

                return records.Count;
            }
            catch (Exception e)
            {
                transaction.Rollback();
                throw e;
            }
        }
    }
}
