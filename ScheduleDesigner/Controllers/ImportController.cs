using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ScheduleDesigner.Dtos;
using ScheduleDesigner.Helpers;
using ScheduleDesigner.Hubs;
using ScheduleDesigner.Repositories.UnitOfWork;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

        [Authorize(Policy = "AdministratorOnly")]
        [HttpPost("schedulePositions"), DisableRequestSizeLimit]
        public IActionResult ImportSchedulePositions([FromForm] IFormFile file, [FromQuery] string connectionId)
        {
            if (file == null)
            {
                return BadRequest("File is not included.");
            }

            if (connectionId == null)
            {
                return BadRequest("Could not find connection id.");
            }

            try
            {
                var records = BulkImport<SchedulePositionDto>.ReadCsv(file).Result;

                if (records == null || !records.Any())
                {
                    return BadRequest("No records has been read from the file.");
                }

                if (!Monitor.TryEnter(SchedulePositionsController.ScheduleLock, SchedulePositionsController.LockTimeout))
                {
                    return BadRequest("Schedule is locked right now. Please try again later.");
                }
                try
                {
                    var settings = _unitOfWork.Settings.GetFirst(e => true).Result;

                    if (settings == null)
                    {
                        return BadRequest("Application settings are not specified.");
                    }

                    var userId = int.Parse(HttpContext.User.Claims.FirstOrDefault(x => x.Type == "user_id")?.Value!);

                    var courseEditionQueues = new SortedList<CourseEditionKey, ConcurrentQueue<object>>();

                    var courseEditions = _unitOfWork.CourseEditions.GetAll()
                        .ToList();

                    var courseEditionKeys = courseEditions.Select(e => new CourseEditionKey
                    {
                        CourseId = e.CourseId,
                        CourseEditionId = e.CourseEditionId
                    }).ToList();

                    if (!courseEditionKeys.Any())
                    {
                        return BadRequest("There is no course editions in database. Please import or add them first.");
                    }

                    lock (ScheduleHub.CourseEditionLocks)
                    {
                        ScheduleHub.AddCourseEditionsLocks(courseEditionKeys, courseEditionQueues);
                    }

                    ScheduleHub.EnterQueues(courseEditionQueues.Values);
                    try
                    {
                        var schedulePositions = _unitOfWork.SchedulePositions.GetAll().Any();

                        if (schedulePositions)
                        {
                            return BadRequest("Schedule must be empty in order to import it.");
                        }

                        var notLockedCourseEditions = _unitOfWork.CourseEditions
                            .Get(e => e.LockUserId != userId || e.LockUserConnectionId != connectionId);

                        if (notLockedCourseEditions.Any())
                        {
                            return BadRequest("You did not lock all course editions.");
                        }

                        var acceptedPositions = new Dictionary<int, List<SchedulePositionDto>>();
                        var courseEditionUnits = new Dictionary<CourseEditionKey, int>();

                        var maxCourseUnits = Methods.GetMaxCourseUnits(_unitOfWork.Courses, settings.CourseDurationMinutes);
                        var courseEditionCoordinators = Methods.GetCoordinatorCourseEditions(_unitOfWork.CoordinatorCourseEditions);
                        var courseEditionGroups = Methods.GetGroupCourseEditions(_unitOfWork.GroupCourseEditions);
                        var groups = Methods.GetGroups(_unitOfWork.Groups);

                        foreach (var record in records)
                        {
                            var courseEditionKey = new CourseEditionKey
                            {
                                CourseId = record.CourseId,
                                CourseEditionId = record.CourseEditionId
                            };

                            if (!courseEditionCoordinators.TryGetValue(courseEditionKey, out var currentCoordinators))
                            {
                                return BadRequest($"No coordinators have been found for course edition ({courseEditionKey.CourseId}," +
                                    $"{courseEditionKey.CourseEditionId}).");
                            }

                            if (!courseEditionGroups.TryGetValue(courseEditionKey, out var currentGroups))
                            {
                                return BadRequest($"No groups have been found for course edition ({courseEditionKey.CourseId}," +
                                    $"{courseEditionKey.CourseEditionId}).");
                            }
                            var currentGroupsIds = new List<int>();
                            foreach (var currentGroup in currentGroups)
                            {
                                if (!groups.TryGetValue(currentGroup, out var groupsIds))
                                {
                                    return BadRequest($"Group ({currentGroup}) could not be found.");
                                }
                                currentGroupsIds.Add(currentGroup);
                                currentGroupsIds.AddRange(groupsIds.Parents);
                                currentGroupsIds.AddRange(groupsIds.Childs);
                            }
                            currentGroupsIds = currentGroupsIds.Distinct().ToList();

                            //can be added
                            if (!courseEditionUnits.TryGetValue(courseEditionKey, out var units))
                            {
                                courseEditionUnits[courseEditionKey] = 0;
                            }

                            if (!maxCourseUnits.TryGetValue(courseEditionKey.CourseId, out var max))
                            {
                                return BadRequest($"Could not find specific course ({courseEditionKey.CourseId}) in database.");
                            }

                            if (courseEditionUnits[courseEditionKey] + 1 > max)
                            {
                                return BadRequest($"Could not add another unit for course edition ({courseEditionKey.CourseId}," +
                                    $"{courseEditionKey.CourseEditionId}) because max has been reached ({max}).");
                            }

                            //no conflicts
                            acceptedPositions.TryGetValue(record.TimestampId, out var positions);
                            if (positions != null)
                            {
                                foreach (var position in positions)
                                {
                                    var key = new CourseEditionKey
                                    {
                                        CourseId = position.CourseId,
                                        CourseEditionId = position.CourseEditionId
                                    };
                                    //room
                                    if (record.RoomId == position.RoomId)
                                    {
                                        return BadRequest($"Conflict (Room) detected for Timestamp ({record.TimestampId}) and Room ({record.RoomId})");
                                    }
                                    //coordinators
                                    if (!courseEditionCoordinators.TryGetValue(key, out var positionCoordinators))
                                    {
                                        return BadRequest($"No coordinators have been found for course edition ({courseEditionKey.CourseId}," +
                                            $"{courseEditionKey.CourseEditionId}).");
                                    }
                                    
                                    if (positionCoordinators.Any(c => currentCoordinators.Contains(c))) 
                                    {
                                        return BadRequest($"Conflict (Coordinator) detected for Timestamp ({record.TimestampId}) and Room ({record.RoomId})");
                                    }
                                    //groups
                                    if (!courseEditionGroups.TryGetValue(key, out var positionGroups))
                                    {
                                        return BadRequest($"No groups have been found for course edition ({courseEditionKey.CourseId}," +
                                            $"{courseEditionKey.CourseEditionId}).");
                                    }

                                    if (positionGroups.Any(c => currentGroupsIds.Contains(c)))
                                    {
                                        return BadRequest($"Conflict (Group) detected for Timestamp ({record.TimestampId}) and Room ({record.RoomId})");
                                    }
                                }
                            }

                            //accept
                            courseEditionUnits[courseEditionKey] += 1;
                            if (acceptedPositions.TryGetValue(record.TimestampId, out var acceptedPosition))
                            {
                                acceptedPosition.Add(record);
                            }
                            else
                            {
                                acceptedPositions.Add(record.TimestampId, new List<SchedulePositionDto>() { record });
                            }
                        }

                        var connectionString = _unitOfWork.Context.Database.GetConnectionString();

                        if (BulkImport<SchedulePositionDto>.Execute(connectionString, "dbo.SchedulePositions", records) <= 0)
                        {
                            return BadRequest("Could not import schedule to database. Maybe you forgot to import some other data first.");
                        }

                        return Ok();
                    }
                    finally
                    {
                        ScheduleHub.RemoveCourseEditionsLocks(courseEditionQueues);
                        ScheduleHub.ExitQueues(courseEditionQueues.Values);
                    }
                }
                finally 
                {
                    Monitor.Exit(SchedulePositionsController.ScheduleLock);
                }
            }
            catch (SqlException e)
            {
                return BadRequest("Violation of primary key. Please check your CSV file.");
            }
            catch (AggregateException e)
            {
                return BadRequest("Could not find one of the required headers. Please check your CSV file.");
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpPost("courseEditions"), DisableRequestSizeLimit]
        public IActionResult ImportCourseEditions(
            [FromForm] IFormFile courseEditionsFile, 
            [FromForm] IFormFile coordinatorsFile,
            [FromForm] IFormFile groupsFile)
        {
            if (courseEditionsFile == null)
            {
                return BadRequest("File with courseEditions is not included.");
            }

            if (coordinatorsFile == null)
            {
                return BadRequest("File with coordinators is not included.");
            }

            if (groupsFile == null)
            {
                return BadRequest("File with groups is not included.");
            }

            try
            {
                var courseEditions = BulkImport<CourseEditionDto>.ReadCsv(courseEditionsFile).Result;
                var coordinatorCourseEditions = BulkImport<CoordinatorCourseEditionDto>.ReadCsv(coordinatorsFile).Result;
                var groupCourseEditions = BulkImport<GroupCourseEditionDto>.ReadCsv(groupsFile).Result;

                if (courseEditions == null || !courseEditions.Any())
                {
                    return BadRequest("No records has been read from the course editions file.");
                }

                if (coordinatorCourseEditions == null || !coordinatorCourseEditions.Any())
                {
                    return BadRequest("No records has been read from the coordinators file.");
                }

                if (groupCourseEditions == null || !groupCourseEditions.Any())
                {
                    return BadRequest("No records has been read from the groups file.");
                }

                if (!Monitor.TryEnter(SchedulePositionsController.ScheduleLock, SchedulePositionsController.LockTimeout))
                {
                    return BadRequest("Schedule is locked right now. Please try again later.");
                }
                try
                {
                    var existingCourseEditions = _unitOfWork.CourseEditions.GetAll().Any();

                    if (existingCourseEditions)
                    {
                        return BadRequest("You need to clear course editions in order to import them.");
                    }

                    var acceptedGroups = new Dictionary<CourseEditionKey, List<int>>();

                    var groups = Methods.GetGroups(_unitOfWork.Groups);

                    foreach (var record in groupCourseEditions)
                    {
                        var courseEditionKey = new CourseEditionKey
                        {
                            CourseId = record.CourseId,
                            CourseEditionId = record.CourseEditionId
                        };

                        if (!acceptedGroups.TryGetValue(courseEditionKey, out var existingGroupIds))
                        {
                            existingGroupIds = new List<int>();
                        }
                        var currentGroupId = record.GroupId;

                        //check groups
                        foreach (var existingGroupId in existingGroupIds)
                        {
                            groups.TryGetValue(existingGroupId, out var g);
                            if (g.Parents.Contains(currentGroupId))
                            {
                                return BadRequest($"Group ({currentGroupId}) is parent of Group ({existingGroupId}) " +
                                    $"which is already assigned to course edition ({courseEditionKey.CourseId},{courseEditionKey.CourseEditionId}).");
                            }

                            if (g.Childs.Contains(currentGroupId))
                            {
                                return BadRequest($"Group ({currentGroupId}) is child of Group ({existingGroupId}) " +
                                    $"which is already assigned to course edition ({courseEditionKey.CourseId},{courseEditionKey.CourseEditionId}).");
                            }
                        }

                        //accept
                        existingGroupIds.Add(currentGroupId);
                        if (!acceptedGroups.ContainsKey(courseEditionKey))
                        {
                            acceptedGroups[courseEditionKey] = existingGroupIds;
                        }
                    }

                    var connectionString = _unitOfWork.Context.Database.GetConnectionString();

                    if (BulkImport<CourseEditionDto>.Execute(connectionString, "dbo.CourseEditions", courseEditions) <= 0)
                    {
                        return BadRequest("Could not import course editions to database. Maybe you forgot to import some other data first.");
                    }

                    if (BulkImport<CoordinatorCourseEditionDto>.Execute(connectionString, "dbo.CoordinatorCourseEditions", coordinatorCourseEditions) <= 0)
                    {
                        return BadRequest("Could not import coordinators to database. Maybe you forgot to import some other data first.");
                    }

                    if (BulkImport<GroupCourseEditionDto>.Execute(connectionString, "dbo.GroupCourseEditions", groupCourseEditions) <= 0)
                    {
                        return BadRequest("Could not import groups to database. Maybe you forgot to import some other data first.");
                    }

                    return Ok();
                }
                finally
                {
                    Monitor.Exit(SchedulePositionsController.ScheduleLock);
                }
            }
            catch (SqlException e)
            {
                return BadRequest("Violation of primary key. Please check your CSV file.");
            }
            catch (AggregateException e)
            {
                return BadRequest("Could not find one of the required headers. Please check your CSV file.");
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpPost("groups"), DisableRequestSizeLimit]
        public IActionResult ImportGroups([FromForm] IFormFile file)
        {
            if (file == null)
            {
                return BadRequest("File is not included.");
            }

            try
            {
                var records = BulkImport<GroupDto>.ReadCsv(file).Result;

                if (records == null || !records.Any())
                {
                    return BadRequest("No records has been read from the file.");
                }

                if (!Monitor.TryEnter(SchedulePositionsController.ScheduleLock, SchedulePositionsController.LockTimeout))
                {
                    return BadRequest("Schedule is locked right now. Please try again later.");
                }
                try
                {
                    var existingGroups = _unitOfWork.Groups.GetAll().Any();

                    if (existingGroups)
                    {
                        return BadRequest("You need to clear groups in order to import them.");
                    }

                    foreach (var record in records)
                    {
                        if (record.GroupId == record.ParentGroupId)
                        {
                            return BadRequest($"Group ({record.GroupId}) cannot be a parent to itself.");
                        }
                    }

                    var connectionString = _unitOfWork.Context.Database.GetConnectionString();

                    if (BulkImport<GroupDto>.Execute(connectionString, "dbo.Groups", records) <= 0)
                    {
                        return BadRequest("Could not import groups to database. Maybe you forgot to import some other data first.");
                    }

                    return Ok();
                }
                finally
                {
                    Monitor.Exit(SchedulePositionsController.ScheduleLock);
                }
            }
            catch (SqlException e)
            {
                return BadRequest("Violation of primary key. Please check your CSV file.");
            }
            catch (AggregateException e)
            {
                return BadRequest("Could not find one of the required headers. Please check your CSV file.");
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpPost("courses"), DisableRequestSizeLimit]
        public IActionResult ImportCourses(
            [FromForm] IFormFile coursesFile,
            [FromForm] IFormFile roomsFile)
        {
            if (coursesFile == null)
            {
                return BadRequest("File with courses is not included.");
            }

            if (roomsFile == null)
            {
                return BadRequest("File with rooms is not included.");
            }

            try
            {
                var courseRecords = BulkImport<CourseDto>.ReadCsv(coursesFile).Result;
                var roomsRecords = BulkImport<CourseRoomDto>.ReadCsv(roomsFile).Result;

                if (courseRecords == null || !courseRecords.Any())
                {
                    return BadRequest("No records has been read from the course file.");
                }

                if (roomsRecords == null || !roomsRecords.Any())
                {
                    return BadRequest("No records has been read from the room file.");
                }

                if (!Monitor.TryEnter(SchedulePositionsController.ScheduleLock, SchedulePositionsController.LockTimeout))
                {
                    return BadRequest("Schedule is locked right now. Please try again later.");
                }
                try
                {
                    var settings = _unitOfWork.Settings.GetFirst(e => true).Result;

                    if (settings == null)
                    {
                        return BadRequest("Application settings are not specified.");
                    }

                    var existingCourses = _unitOfWork.Courses.GetAll().Any();

                    if (existingCourses)
                    {
                        return BadRequest("You need to clear courses in order to import them.");
                    }

                    foreach (var record in courseRecords)
                    {
                        if (!Methods.AreUnitsMinutesValid(record.UnitsMinutes, settings))
                        {
                            return BadRequest($"Units minutes of course ({record.CourseId}) does not match the settings.");
                        }
                    }

                    var connectionString = _unitOfWork.Context.Database.GetConnectionString();

                    if (BulkImport<CourseDto>.Execute(connectionString, "dbo.Courses", courseRecords) <= 0)
                    {
                        return BadRequest("Could not import courses to database. Maybe you forgot to import some other data first.");
                    }

                    if (BulkImport<CourseRoomDto>.Execute(connectionString, "dbo.CourseRooms", roomsRecords) <= 0)
                    {
                        return BadRequest("Could not import course rooms to database. Maybe you forgot to import some other data first.");
                    }

                    return Ok();
                }
                finally
                {
                    Monitor.Exit(SchedulePositionsController.ScheduleLock);
                }
            }
            catch (SqlException e)
            {
                return BadRequest("Violation of primary key. Please check your CSV file.");
            }
            catch (AggregateException e)
            {
                return BadRequest("Could not find one of the required headers. Please check your CSV file.");
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpPost("courseTypes"), DisableRequestSizeLimit]
        public IActionResult ImportCourseTypes([FromForm] IFormFile file)
        {
            if (file == null)
            {
                return BadRequest("File is not included.");
            }

            try
            {
                var records = BulkImport<CourseTypeDto>.ReadCsv(file).Result;

                if (records == null || !records.Any())
                {
                    return BadRequest("No records has been read from the file.");
                }

                var connectionString = _unitOfWork.Context.Database.GetConnectionString();

                if (BulkImport<CourseTypeDto>.Execute(connectionString, "dbo.CourseTypes", records) <= 0)
                {
                    return BadRequest("Could not import course types to database. Maybe you forgot to import some other data first.");
                }

                return Ok();
            }
            catch (SqlException e)
            {
                return BadRequest("Violation of primary key. Please check your CSV file.");
            }
            catch (AggregateException e)
            {
                return BadRequest("Could not find one of the required headers. Please check your CSV file.");
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpPost("roomTypes"), DisableRequestSizeLimit]
        public IActionResult ImportRoomTypes([FromForm] IFormFile file)
        {
            if (file == null)
            {
                return BadRequest("File is not included.");
            }

            try
            {
                var records = BulkImport<RoomTypeDto>.ReadCsv(file).Result;

                if (records == null || !records.Any())
                {
                    return BadRequest("No records has been read from the file.");
                }

                var connectionString = _unitOfWork.Context.Database.GetConnectionString();

                if (BulkImport<RoomTypeDto>.Execute(connectionString, "dbo.RoomTypes", records) <= 0)
                {
                    return BadRequest("Could not import room types to database. Maybe you forgot to import some other data first.");
                }

                return Ok();
            }
            catch (SqlException e)
            {
                return BadRequest("Violation of primary key. Please check your CSV file.");
            }
            catch (AggregateException e)
            {
                return BadRequest("Could not find one of the required headers. Please check your CSV file.");
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpPost("rooms"), DisableRequestSizeLimit]
        public IActionResult ImportRooms([FromForm] IFormFile file)
        {
            if (file == null)
            {
                return BadRequest("File is not included.");
            }

            try
            {
                var records = BulkImport<RoomDto>.ReadCsv(file).Result;

                if (records == null || !records.Any())
                {
                    return BadRequest("No records has been read from the file.");
                }

                var connectionString = _unitOfWork.Context.Database.GetConnectionString();

                if (BulkImport<RoomDto>.Execute(connectionString, "dbo.Rooms", records) <= 0)
                {
                    return BadRequest("Could not import rooms to database. Maybe you forgot to import some other data first.");
                }

                return Ok();
            }
            catch (SqlException e)
            {
                return BadRequest("Violation of primary key. Please check your CSV file.");
            }
            catch (AggregateException e)
            {
                return BadRequest("Could not find one of the required headers. Please check your CSV file.");
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpPost("studentGroups"), DisableRequestSizeLimit]
        public IActionResult ImportStudentGroups([FromForm] IFormFile file)
        {
            if (file == null)
            {
                return BadRequest("File is not included.");
            }

            try
            {
                var records = BulkImport<StudentGroupDto>.ReadCsv(file).Result;

                if (records == null || !records.Any())
                {
                    return BadRequest("No records has been read from the file.");
                }

                var connectionString = _unitOfWork.Context.Database.GetConnectionString();

                if (BulkImport<StudentGroupDto>.Execute(connectionString, "dbo.StudentGroups", records) <= 0)
                {
                    return BadRequest("Could not import student groups to database. Maybe you forgot to import some other data first.");
                }

                return Ok();
            }
            catch (SqlException e)
            {
                return BadRequest("Violation of primary key. Please check your CSV file.");
            }
            catch (AggregateException e)
            {
                return BadRequest("Could not find one of the required headers. Please check your CSV file.");
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }
    }
}
