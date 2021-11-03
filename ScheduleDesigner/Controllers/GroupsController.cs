using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using ScheduleDesigner.Dtos;

namespace ScheduleDesigner.Controllers
{
    [ODataRoutePrefix("Groups")]
    public class GroupsController : ODataController
    {
        private readonly IGroupRepo _groupRepo;

        public GroupsController(IGroupRepo groupRepo)
        {
            _groupRepo = groupRepo;
        }

        [HttpPost]
        [ODataRoute("")]
        public async Task<IActionResult> CreateGroup([FromBody] Group group)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var _group = await _groupRepo.Add(group);

                if (_group != null)
                {
                    await _groupRepo.SaveChanges();
                    return Created(_group);
                }
                return NotFound();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet]
        [EnableQuery(PageSize = 20)]
        [ODataRoute("")]
        public IActionResult GetGroups()
        {
            return Ok(_groupRepo.GetAll());
        }

        [HttpGet]
        [EnableQuery]
        [ODataRoute("({key})")]
        public IActionResult GetGroup([FromODataUri] int key)
        {
            try
            {
                var _group = _groupRepo
                    .Get(e => e.GroupId == key);
                if (!_group.Any())
                {
                    return NotFound();
                }

                return Ok(SingleResult.Create(_group));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetGroupFullName([FromODataUri] int key)
        {
            try
            {
                var _group = _groupRepo
                    .Get(e => e.GroupId == key)
                    .Include(e => e.ParentGroup);

                if (!_group.Any())
                {
                    return NotFound();
                }

                var group = await _group.FirstAsync();
                var fullGroupName = group.Name;
                var groupIds = new List<int>() { group.GroupId };
                var levels = 0;

                while (group.ParentGroupId != null)
                {
                    ++levels;
                    _group = _group.ThenInclude(e => e.ParentGroup);
                    group = await _group.FirstAsync();
                    for (var i = 0; i < levels; ++i)
                    {
                        group = group.ParentGroup;
                    }
                    fullGroupName = group.Name + fullGroupName;
                    groupIds.Add(group.GroupId);
                }

                return Ok(new GroupFullName { FullName = fullGroupName, GroupsIds = groupIds, Levels = levels });
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetGroupsFullNames([FromODataUri] IEnumerable<int> GroupsIds)
        {
            try
            {
                var groupsFullNamesList = new List<GroupFullName>();
                var groupsFullNamesDictionary = new Dictionary<int, GroupFullName>();

                foreach (var _groupId in GroupsIds)
                {
                    if (groupsFullNamesDictionary.TryGetValue(_groupId, out var groupFullName))
                    {
                        groupsFullNamesList.Add(groupFullName);
                        continue;
                    }

                    var _group = _groupRepo
                    .Get(e => e.GroupId == _groupId)
                    .Include(e => e.ParentGroup);

                    if (!_group.Any())
                    {
                        return NotFound();
                    }

                    var group = await _group.FirstAsync();
                    var fullGroupName = group.Name;
                    var groupIds = new List<int>() { group.GroupId };
                    var levels = 0;

                    while (group.ParentGroupId != null)
                    {
                        ++levels;
                        _group = _group.ThenInclude(e => e.ParentGroup);
                        group = await _group.FirstAsync();
                        for (var i = 0; i < levels; ++i)
                        {
                            group = group.ParentGroup;
                        }
                        fullGroupName = group.Name + fullGroupName;
                        groupIds.Add(group.GroupId);
                    }

                    groupFullName = new GroupFullName {FullName = fullGroupName, GroupsIds = groupIds, Levels = levels};
                    groupsFullNamesDictionary.Add(groupIds[0], groupFullName);
                    groupsFullNamesList.Add(groupFullName);
                }

                return Ok(groupsFullNamesList);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPatch]
        [ODataRoute("({key})")]
        public async Task<IActionResult> UpdateGroup([FromODataUri] int key, [FromBody] Delta<Group> delta)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var _group = await _groupRepo
                    .GetFirst(e => e.GroupId == key);
                if (_group == null)
                {
                    return NotFound();
                }

                delta.Patch(_group);

                await _groupRepo.SaveChanges();

                return Ok(_group);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpDelete]
        [ODataRoute("({key})")]
        public async Task<IActionResult> DeleteGroup([FromODataUri] int key)
        {
            try
            {
                var result = await _groupRepo
                    .Delete(e => e.GroupId == key);
                if (result < 0)
                {
                    return NotFound();
                }

                await _groupRepo.SaveChanges();
                return NoContent();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
