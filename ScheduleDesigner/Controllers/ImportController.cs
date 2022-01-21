using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ScheduleDesigner.Repositories.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static ScheduleDesigner.Dtos.ImportDtos;

namespace ScheduleDesigner.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImportController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public ImportController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        //Authorize - Administrator
        [HttpPost("schedulePositions"), DisableRequestSizeLimit]
        public async Task<IActionResult> ImportSchedulePositions([FromForm] IFormFile file)
        {
            try
            {
                if (file == null)
                {
                    return BadRequest("File is not included.");
                }
                
                using var memoryStream = new MemoryStream(new byte[file.Length]);
                await file.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                var records = new List<SchedulePositionDto>();

                using (var reader = new StreamReader(memoryStream))
                using (var csvReader = new CsvReader(reader, new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture) 
                {
                    Delimiter = "|"
                }))
                {
                    records = csvReader.GetRecords<SchedulePositionDto>().ToList();
                }

                using (var connection = new SqlConnection(_unitOfWork.Context.Database.GetConnectionString()))
                {
                    connection.Open();
                    using var transaction = connection.BeginTransaction();
                    try
                    {
                        using var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.KeepIdentity, transaction);
                        bulkCopy.DestinationTableName = "dbo.SchedulePositions";
                        bulkCopy.ColumnMappings.Add("RoomId", "RoomId");
                        bulkCopy.ColumnMappings.Add("TimestampId", "TimestampId");
                        bulkCopy.ColumnMappings.Add("CourseId", "CourseId");
                        bulkCopy.ColumnMappings.Add("CourseEditionId", "CourseEditionId");

                        var dt = new DataTable();
                        dt.Columns.Add(new DataColumn("RoomId", typeof(int)));
                        dt.Columns.Add(new DataColumn("TimestampId", typeof(int)));
                        dt.Columns.Add(new DataColumn("CourseId", typeof(int)));
                        dt.Columns.Add(new DataColumn("CourseEditionId", typeof(int)));
                        foreach (var record in records)
                        {
                            var row = dt.NewRow();
                            row["RoomId"] = record.RoomId;
                            row["TimestampId"] = record.TimestampId;
                            row["CourseId"] = record.CourseId;
                            row["CourseEditionId"] = record.CourseEditionId;
                            dt.Rows.Add(row);
                        }
                        bulkCopy.WriteToServer(dt);
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                    }
                }

                return Ok();
            } 
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
