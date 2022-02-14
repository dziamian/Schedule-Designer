using Microsoft.AspNetCore.Mvc;
using ScheduleDesigner.Helpers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Models
{
    /// <summary>
    /// Klasa przeznaczona do eksportowania danych do plików CSV.
    /// </summary>
    public class CsvExport : FileResult
    {
        /// <summary>
        /// Dane do wyeksportowania.
        /// </summary>
        private readonly IEnumerable<IExportCsv> _data;

        /// <summary>
        /// Konstruktor klasy.
        /// </summary>
        /// <param name="data">Dane do wyeksportowania</param>
        /// <param name="fileDownloadName">Nazwa pliku CSV</param>
        public CsvExport(IEnumerable<IExportCsv> data, string fileDownloadName) : base("text/csv")
        {
            _data = data;
            FileDownloadName = fileDownloadName;
        }

        /// <summary>
        /// Funkcja wypisująca dane do ciała kontekstu będącego częścią żądania HTTP.
        /// </summary>
        /// <param name="context">Kontekst będący częścią żądania HTTP</param>
        /// <returns>Asynchroniczną operację</returns>
        public async override Task ExecuteResultAsync(ActionContext context)
        {
            var response = context.HttpContext.Response;
            context.HttpContext.Response.Headers.Add("Content-Disposition", new[] { "attachment; filename=" + FileDownloadName });
            using var streamWriter = new StreamWriter(response.Body, System.Text.Encoding.UTF8);
            var firstRecord = _data.FirstOrDefault();
            if (firstRecord == null)
            {
                await streamWriter.WriteAsync(" ");
                await streamWriter.FlushAsync();
                return;
            }

            var header = firstRecord.GetHeader();
            await streamWriter.WriteAsync(header);
            foreach (var p in _data)
            {
                await streamWriter.WriteAsync(p.GetRow());
                await streamWriter.FlushAsync();
            }
            await streamWriter.FlushAsync();
        }
    }
}
