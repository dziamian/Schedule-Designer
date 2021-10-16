using Microsoft.AspNet.OData;
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

        public SettingsController(ISettingsRepo settingsRepo)
        {
            _settingsRepo = settingsRepo;
        }

        private static bool IsDataValid(Settings settings)
        {
            return (settings.EndTime - settings.StartTime).TotalMinutes % settings.CourseDurationMinutes == 0;
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
                if (_settings != null)
                {
                    return Created(_settings);
                }
                return NotFound();
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

                return NoContent();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
