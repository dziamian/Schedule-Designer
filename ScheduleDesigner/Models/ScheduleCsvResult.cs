using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ScheduleDesigner.Models
{
    public class ScheduleCsvResult : FileResult
    {
        private readonly IEnumerable<SchedulePosition> _scheduleData;

        public ScheduleCsvResult(IEnumerable<SchedulePosition> scheduleData, string fileDownloadName) : base("text/csv")
        {
            _scheduleData = scheduleData;
            FileDownloadName = fileDownloadName;
        }

        public async override Task ExecuteResultAsync(ActionContext context)
        {
            var response = context.HttpContext.Response;
            context.HttpContext.Response.Headers.Add("Content-Disposition", new[] { "attachment; filename=" + FileDownloadName });
            using var streamWriter = new StreamWriter(response.Body);
            await streamWriter.WriteAsync(
              $"RoomId, TimestampId, CourseId, CourseEditionId\n"
            );
            foreach (var p in _scheduleData)
            {
                await streamWriter.WriteAsync(
                  $"{p.RoomId}, {p.TimestampId}, {p.CourseId}, {p.CourseEditionId}\n"
                );
                await streamWriter.FlushAsync();
            }
            await streamWriter.FlushAsync();
        }
    }
}
