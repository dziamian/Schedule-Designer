using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using ScheduleDesigner.Converters;
using ScheduleDesigner.Hubs;
using ScheduleDesigner.Hubs.Interfaces;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Interfaces;
using ScheduleDesigner.Services;
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
        private readonly IHubContext<ScheduleHub, IScheduleClient> _hubContext;

        public SettingsController(ISettingsRepo settingsRepo, IHubContext<ScheduleHub, IScheduleClient> hubContext)
        {
            _settingsRepo = settingsRepo;
            _hubContext = hubContext;
        }

        private bool IsDataValid(Settings settings)
        {
            return (settings.EndTime - settings.StartTime).TotalMinutes % settings.CourseDurationMinutes == 0;
        }

        [HttpPost]
        [ODataRoute("")]
        public async Task<IActionResult> CreateSettings([FromBody] Settings settings)
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
                var _settings = await _settingsRepo.GetSettingsAsync();
                if (_settings != null)
                {
                    return Conflict("Settings already exists.");
                }

                var id = await _settingsRepo.AddSettingsAsync(settings);
                if (id > 0)
                {
                    await _hubContext.Clients.All.Test("test");
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
        [EnableQuery]
        [ODataRoute("")]
        public async Task<IActionResult> GetSettings()
        {
            try
            {
                var _settings = await _settingsRepo.GetSettingsAsync();
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
            /// PLAN ZAJĘĆ MUSI BYĆ PUSTY !!
            /// if schedule table has elements -> BadRequest

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var _settings = await _settingsRepo.GetSettingsAsync();
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

                await _settingsRepo.SaveChangesAsync();

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
            /// PLAN ZAJĘĆ MUSI BYĆ PUSTY !!
            /// if schedule table has elements -> BadRequest

            try
            {
                var result = await _settingsRepo.DeleteSettingsAsync();
                if (result == 0)
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
