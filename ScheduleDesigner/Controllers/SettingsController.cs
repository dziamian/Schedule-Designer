using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScheduleDesigner.Attributes;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Interfaces;
using ScheduleDesigner.Repositories.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Controllers
{
    [ODataRoutePrefix("Settings")]
    public class SettingsController : ODataController
    {
        private readonly IUnitOfWork _unitOfWork;

        public SettingsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        private static bool IsDataValid(Settings settings)
        {
            return (settings.EndTime - settings.StartTime).TotalMinutes % settings.CourseDurationMinutes == 0;
        }

        private static int GetNumberOfSlots(Settings settings)
        {
            return (int)(settings.EndTime - settings.StartTime).TotalMinutes / settings.CourseDurationMinutes;
        }

        private async Task AddTimestamps(Settings settings)
        {
            var numberOfSlots = (settings.EndTime - settings.StartTime).TotalMinutes / settings.CourseDurationMinutes;
            var numberOfWeeks = settings.TermDurationWeeks;
            for (int k = 0; k < numberOfWeeks; ++k)
                for (int j = 0; j < 5; ++j)
                    for (int i = 0; i < numberOfSlots; ++i)
                    {
                        await _unitOfWork.Timestamps.Add(new Timestamp { PeriodIndex = i + 1, Day = j + 1, Week = k + 1 });
                    }
        }

        private void RemoveTimestamps()
        {
            _unitOfWork.Timestamps.GetAll().RemoveRange(_unitOfWork.Timestamps.GetAll().ToList());
        }

        [HttpPost]
        [ODataRoute("")]
        public async Task<IActionResult> CreateSettings([FromBody] Settings settings)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!IsDataValid(settings))
            {
                ModelState.AddModelError("CoursesAmount", "Couldn't calculate the valid amount of max courses per day.");
                return BadRequest(ModelState);
            }

            try
            {
                var _settings = await _unitOfWork.Settings.GetSettings();
                if (_settings != null)
                {
                    return Conflict("Settings already exists.");
                }

                _settings = await _unitOfWork.Settings.AddSettings(settings);
                if (_settings == null)
                {
                    return NotFound();
                }

                await AddTimestamps(_settings);

                await _unitOfWork.CompleteAsync();

                return Created(_settings);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet]
        [CustomEnableQuery]
        [ODataRoute("")]
        public async Task<IActionResult> GetSettings()
        {
            try
            {
                var _settings = await _unitOfWork.Settings.GetSettings();
                if (_settings == null)
                {
                    return NotFound();
                }

                return Ok(_settings);
            } 
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPeriods()
        {
            try
            {
                var _settings = await _unitOfWork.Settings.GetSettings();
                if (_settings == null)
                {
                    return NotFound();
                }

                var numberOfPeriods = GetNumberOfSlots(_settings) + 1;
                var currentPeriod = _settings.StartTime;
                var periodsStrings = new string[numberOfPeriods];
                var courseDuration = new TimeSpan(0, _settings.CourseDurationMinutes, 0);

                periodsStrings[0] = currentPeriod.ToString(@"hh\:mm");
                for (int i = 1; i < numberOfPeriods; ++i)
                {
                    currentPeriod = currentPeriod.Add(courseDuration);
                    periodsStrings[i] = currentPeriod.ToString(@"hh\:mm");
                }

                return Ok(periodsStrings);
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        [HttpPatch]
        [ODataRoute("")]
        public async Task<IActionResult> UpdateSettings([FromBody] Delta<Settings> delta)
        {
            /// KURSY MUSZĄ BYĆ PUSTE !!
            /// if course table has elements -> BadRequest

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var _settings = await _unitOfWork.Settings.GetSettings();
                if (_settings == null)
                {
                    return NotFound();
                }

                delta.Patch(_settings);

                if (!IsDataValid(_settings))
                {
                    ModelState.AddModelError("CoursesAmount", "Couldn't calculate the valid amount of max courses per day.");
                    return BadRequest(ModelState);
                }

                RemoveTimestamps();
                await AddTimestamps(_settings);

                await _unitOfWork.Settings.SaveChanges();

                return Ok(_settings);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpDelete]
        [ODataRoute("")]
        public async Task<IActionResult> DeleteSettings()
        {
            /// KURSY MUSZĄ BYĆ PUSTE !!
            /// if course table has elements -> BadRequest

            try
            {
                var result = await _unitOfWork.Settings.DeleteSettings();
                if (result < 0)
                {
                    return NotFound();
                }

                RemoveTimestamps();

                await _unitOfWork.CompleteAsync();

                return NoContent();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
