﻿using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Mvc;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Controllers
{
    [ODataRoutePrefix("Settings")]
    public class SettingsController : ODataController
    {
        private readonly ISettingsRepo _settingsRepo;
        private readonly ITimestampRepo _timestampRepo;

        public SettingsController(ISettingsRepo settingsRepo, ITimestampRepo timestampRepo)
        {
            _settingsRepo = settingsRepo;
            _timestampRepo = timestampRepo;
        }

        private static bool IsDataValid(Settings settings)
        {
            return (settings.EndTime - settings.StartTime).TotalMinutes % settings.CourseDurationMinutes == 0;
        }

        private async Task AddTimestamps(Settings settings)
        {
            var numberOfSlots = (settings.EndTime - settings.StartTime).TotalMinutes / settings.CourseDurationMinutes;
            Console.WriteLine(numberOfSlots);
            var numberOfWeeks = settings.TermDurationWeeks;
            for (int k = 0; k < numberOfWeeks; ++k)
                for (int j = 0; j < 5; ++j)
                    for (int i = 0; i < numberOfSlots; ++i)
                    {
                        await _timestampRepo.Add(new Timestamp { PeriodIndex = i + 1, Day = j + 1, Week = k + 1 });
                    }
        }

        private void RemoveTimestamps()
        {
            _timestampRepo.GetAll().RemoveRange(_timestampRepo.GetAll().ToList());
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
                var _settings = await _settingsRepo.GetSettings();
                if (_settings != null)
                {
                    return Conflict("Settings already exists.");
                }

                _settings = await _settingsRepo.AddSettings(settings);
                if (_settings == null)
                {
                    return NotFound();
                }

                await AddTimestamps(_settings);

                await _timestampRepo.SaveChanges();

                return Created(_settings);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet]
        [EnableQuery]
        [ODataRoute("")]
        public async Task<IActionResult> GetSettings()
        {
            try
            {
                var _settings = await _settingsRepo.GetSettings();
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
                var _settings = await _settingsRepo.GetSettings();
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

                await _settingsRepo.SaveChanges();

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
                var result = await _settingsRepo.DeleteSettings();
                if (result < 0)
                {
                    return NotFound();
                }

                RemoveTimestamps();

                await _timestampRepo.SaveChanges();

                return NoContent();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
