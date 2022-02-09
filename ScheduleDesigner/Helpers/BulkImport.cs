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
    public static class BulkImport<T> where T : class
    {
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
