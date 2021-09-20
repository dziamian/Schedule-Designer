using Microsoft.AspNetCore.Mvc;
using ScheduleDesigner.Converters;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SettingsController : ControllerBase
    {
        private readonly ISettingsRepo _settingsRepo;

        public SettingsController(ISettingsRepo settingsRepo)
        {
            _settingsRepo = settingsRepo;
        }

        private bool IsDataValid(Settings settings)
        {
            return (settings.EndTime - settings.StartTime).TotalMinutes % settings.CourseDurationMinutes == 0;
        }

        [HttpPost]
        public async Task<ActionResult> CreateSettings([FromBody] Settings settings)
        {
            if (!IsDataValid(settings))
            {
                ModelState.AddModelError("CoursesAmount", "Couldn't calculate the valid amount of max courses per day.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var _settings = await _settingsRepo.GetSettings();
                if (_settings != null)
                {
                    return Conflict("Settings already exists.");
                }

                var id = await _settingsRepo.AddSettings(settings);
                if (id > 0)
                {
                    return Ok();
                }
                return NotFound();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet]
        public async Task<ActionResult> GetSettings()
        {
            try
            {
                var _settings = await _settingsRepo.GetSettings();
                if (_settings == null)
                {
                    return NotFound();
                }
                
                return Ok(SettingsConverter.ToSettingsReadDto(_settings));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpDelete]
        public async Task<ActionResult> DeleteSettings()
        {
            /// PLAN ZAJĘĆ MUSI BYĆ PUSTY !!
            /// if schedule table has elements -> BadRequest
            
            try
            {
                var result = await _settingsRepo.DeleteSettings();
                if (result == 0)
                {
                    return NotFound();
                }

                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPut]
        public async Task<ActionResult> UpdateSettings([FromBody] Settings settings)
        {
            /// PLAN ZAJĘĆ MUSI BYĆ PUSTY !!
            /// if schedule table has elements -> BadRequest

            if (!IsDataValid(settings))
            {
                ModelState.AddModelError("CoursesAmount", "Couldn't calculate the valid amount of max courses per day.");
            }

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

                _settings.UsosBaseUrl = settings.UsosBaseUrl;
                _settings.CourseDurationMinutes = settings.CourseDurationMinutes;
                _settings.StartTime = settings.StartTime;
                _settings.EndTime = settings.EndTime;
                _settings.TermDurationWeeks = settings.TermDurationWeeks;

                await _settingsRepo.UpdateSettings(_settings);

                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
