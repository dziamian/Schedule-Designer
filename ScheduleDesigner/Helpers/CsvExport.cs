using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static ScheduleDesigner.Helpers;

namespace ScheduleDesigner.Models
{
    public class CsvExport : FileResult
    {
        private readonly IEnumerable<IExportCsv> _data;

        public CsvExport(IEnumerable<IExportCsv> data, string fileDownloadName) : base("text/csv")
        {
            _data = data;
            FileDownloadName = fileDownloadName;
        }

        public async override Task ExecuteResultAsync(ActionContext context)
        {
            var response = context.HttpContext.Response;
            context.HttpContext.Response.Headers.Add("Content-Disposition", new[] { "attachment; filename=" + FileDownloadName });
            using var streamWriter = new StreamWriter(response.Body);
            var header = _data.FirstOrDefault().GetHeader();
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
