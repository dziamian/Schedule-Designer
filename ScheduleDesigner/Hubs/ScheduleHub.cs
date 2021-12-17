using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ScheduleDesigner.Hubs.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ScheduleDesigner.Controllers;
using ScheduleDesigner.Helpers;
using ScheduleDesigner.Hubs.Helpers;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Interfaces;

namespace ScheduleDesigner.Hubs
{
    [Authorize]
    public class ScheduleHub : Hub<IScheduleClient>
    {
        private readonly ISettingsRepo _settingsRepo;
        private readonly ITimestampRepo _timestampRepo;
        private readonly ICourseEditionRepo _courseEditionRepo;
        private readonly ICourseRoomTimestampRepo _courseRoomTimestampRepo;
        private readonly ISchedulePositionRepo _schedulePositionRepo;
        private readonly IScheduledMoveRepo _scheduledMoveRepo;
        private readonly IGroupRepo _groupRepo;

        private static readonly ConcurrentDictionary<CourseEditionKey, ConcurrentQueue<object>>
            CourseEditionLocks = new ConcurrentDictionary<CourseEditionKey, ConcurrentQueue<object>>();

        private static readonly ConcurrentDictionary<SchedulePositionKey, ConcurrentQueue<object>>
            SchedulePositionLocksL1 = new ConcurrentDictionary<SchedulePositionKey, ConcurrentQueue<object>>();

        private static readonly ConcurrentDictionary<SchedulePositionKey, ConcurrentQueue<object>>
            SchedulePositionLocksL2 = new ConcurrentDictionary<SchedulePositionKey,ConcurrentQueue<object>>();

        private static readonly ConcurrentDictionary<CoordinatorPositionKey, ConcurrentQueue<object>>
            CoordinatorPositionLocks = new ConcurrentDictionary<CoordinatorPositionKey, ConcurrentQueue<object>>();

        private static readonly ConcurrentDictionary<GroupPositionKey, ConcurrentQueue<object>>
            GroupPositionLocks = new ConcurrentDictionary<GroupPositionKey, ConcurrentQueue<object>>();

        private void RemoveCourseEditionLock(ConcurrentQueue<object> courseEditionQueue, CourseEditionKey courseEditionKey)
        {
            courseEditionQueue.TryDequeue(out _);
            if (courseEditionQueue.IsEmpty)
            {
                CourseEditionLocks.TryRemove(courseEditionKey, out _);
            }
        }

        private void RemoveSchedulePositionLockL1(SortedList<SchedulePositionKey, ConcurrentQueue<object>> schedulePositions, SchedulePositionKey key)
        {
            var contains = schedulePositions.TryGetValue(key, out var queue);
            if (contains)
            {
                queue.TryDequeue(out _);
                if (queue.IsEmpty)
                {
                    SchedulePositionLocksL1.TryRemove(key, out _);
                }
            }
        }

        private void RemoveSchedulePositionsLocksL1(List<ConcurrentQueue<object>> schedulePositionQueues, List<SchedulePositionKey> schedulePositionKeys)
        {
            for (var i = 0; i < schedulePositionQueues.Count; ++i)
            {
                schedulePositionQueues[i].TryDequeue(out _);
                if (schedulePositionQueues[i].IsEmpty)
                {
                    SchedulePositionLocksL1.TryRemove(schedulePositionKeys[i], out _);
                }
            }
        }

        private void RemoveSchedulePositionsLocksL1(SortedList<SchedulePositionKey, ConcurrentQueue<object>> schedulePositions)
        {
            foreach (var schedulePosition in schedulePositions)
            {
                schedulePosition.Value.TryDequeue(out _);
                if (schedulePosition.Value.IsEmpty)
                {
                    SchedulePositionLocksL1.TryRemove(schedulePosition.Key, out _);
                }
            }
        }

        private void RemoveSchedulePositionLockL2(SortedList<SchedulePositionKey, ConcurrentQueue<object>> schedulePositions, SchedulePositionKey key)
        {
            var contains = schedulePositions.TryGetValue(key, out var queue);
            if (contains)
            {
                queue.TryDequeue(out _);
                if (queue.IsEmpty)
                {
                    SchedulePositionLocksL2.TryRemove(key, out _);
                }
            }
        }

        private void RemoveSchedulePositionsLocksL2(List<ConcurrentQueue<object>> schedulePositionQueues, List<SchedulePositionKey> schedulePositionKeys)
        {
            for (var i = 0; i < schedulePositionQueues.Count; ++i)
            {
                schedulePositionQueues[i].TryDequeue(out _);
                if (schedulePositionQueues[i].IsEmpty)
                {
                    SchedulePositionLocksL2.TryRemove(schedulePositionKeys[i], out _);
                }
            }
        }

        private void RemoveSchedulePositionsLocksL2(SortedList<SchedulePositionKey, ConcurrentQueue<object>> schedulePositions)
        {
            foreach (var schedulePosition in schedulePositions)
            {
                schedulePosition.Value.TryDequeue(out _);
                if (schedulePosition.Value.IsEmpty)
                {
                    SchedulePositionLocksL2.TryRemove(schedulePosition.Key, out _);
                }
            }
        }

        private void RemoveCoordinatorPositionsLocks(List<ConcurrentQueue<object>> coordinatorPositionQueues, List<CoordinatorPositionKey> coordinatorPositionKeys)
        {
            for (var i = 0; i < coordinatorPositionQueues.Count; ++i)
            {
                coordinatorPositionQueues[i].TryDequeue(out _);
                if (coordinatorPositionQueues[i].IsEmpty)
                {
                    CoordinatorPositionLocks.TryRemove(coordinatorPositionKeys[i], out _);
                }
            }
        }

        private void RemoveGroupPositionsLocks(List<ConcurrentQueue<object>> groupPositionQueues, List<GroupPositionKey> groupPositionKeys)
        {
            for (var i = 0; i < groupPositionQueues.Count; ++i)
            {
                groupPositionQueues[i].TryDequeue(out _);
                if (groupPositionQueues[i].IsEmpty)
                {
                    GroupPositionLocks.TryRemove(groupPositionKeys[i], out _);
                }
            }
        }

        private int? GetPossibleMove(List<SchedulePositionKey> source, List<int> skippedMovesIds, 
            out Tuple<SchedulePositionKey[], SchedulePositionKey[]> possibleMove)
        {
            possibleMove = null;
            try
            {
                if (!source.Any())
                {
                    return null;
                }

                var timestampsIds = source.Select(e => e.TimestampId).ToList();

                var movesIds = _scheduledMoveRepo
                    .Get(e => e.IsConfirmed && timestampsIds.Contains(e.TimestampId_2)
                        && !skippedMovesIds.Contains(e.MoveId))
                    .OrderBy(e => e.ScheduleOrder)
                    .GroupBy(e => e.MoveId)
                    .Select(e => e.Key).ToList();

                if (!movesIds.Any())
                {
                    return null;
                }

                var moveId = movesIds.First();

                var _scheduledMove = _scheduledMoveRepo
                    .Get(e => e.MoveId == moveId);

                var length = _scheduledMove.Count();
                var possibleMoveSource = new SchedulePositionKey[length];
                var possibleMoveDestination = new SchedulePositionKey[length];

                var index = 0;
                _scheduledMove.ToList().ForEach((move) =>
                {
                    possibleMoveSource[index] = new SchedulePositionKey
                    {
                        RoomId = move.RoomId_1,
                        TimestampId = move.TimestampId_1
                    };
                    possibleMoveDestination[index] = new SchedulePositionKey
                    {
                        RoomId = move.RoomId_2,
                        TimestampId = move.TimestampId_2
                    };
                    ++index;
                });

                possibleMove = new Tuple<SchedulePositionKey[], SchedulePositionKey[]>(possibleMoveSource, possibleMoveDestination);

                return moveId;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void MakeScheduledMoves(List<SchedulePositionKey> sourceSchedulePositionKeys, 
            ref SortedList<SchedulePositionKey, ConcurrentQueue<object>> L1schedulePositionAllQueues, ref List<SchedulePositionKey> L1KeysToRemove)
        {
            var coordinatorPositionKeys = new List<CoordinatorPositionKey>();
            var groupPositionKeys = new List<GroupPositionKey>();
            var coordinatorPositionQueues = new List<ConcurrentQueue<object>>();
            var groupPositionQueues = new List<ConcurrentQueue<object>>();

            var L1scheduledMovesAllQueues = new SortedList<SchedulePositionKey, ConcurrentQueue<object>>();
            var L1scheduledMovesAllQueuesNotLocked = new SortedList<SchedulePositionKey, ConcurrentQueue<object>>();
            var L2scheduledMovesAllQueues = new SortedList<SchedulePositionKey, ConcurrentQueue<object>>();

            var _sources = new Queue<List<SchedulePositionKey>>();
            var initialSource = new List<SchedulePositionKey>(sourceSchedulePositionKeys);
            initialSource.Sort();
            _sources.Enqueue(initialSource);
            
            while (_sources.TryDequeue(out var _source))
            {
                try
                {
                    var _skippedMovesIds = new List<int>();
                    int? currentMoveId = null;

                    while ((currentMoveId = GetPossibleMove(_source, _skippedMovesIds, out var possibleMove)) != null)
                    {
                        L2scheduledMovesAllQueues.Clear();
                        L1scheduledMovesAllQueuesNotLocked.Clear();
                        L1scheduledMovesAllQueues.Clear();
                        coordinatorPositionKeys.Clear();
                        groupPositionKeys.Clear();
                        coordinatorPositionQueues.Clear();
                        groupPositionQueues.Clear();

                        var isQueued = false;
                        var candidateSourceKeys = new List<SchedulePositionKey>();
                        var localKeysToRemove = new List<SchedulePositionKey>();

                        var candidateSourceTimestamps = possibleMove.Item1.Select(e => e.TimestampId).ToList();
                        var candidateDestTimestamps = possibleMove.Item2.Select(e => e.TimestampId).ToList();
                        var candidateSourceRoomId = possibleMove.Item1.FirstOrDefault().RoomId;
                        var candidateDestRoomId = possibleMove.Item2.FirstOrDefault().RoomId;

                        var _srcTimestamps = _timestampRepo
                            .Get(e => candidateSourceTimestamps.Contains(e.TimestampId));

                        var _destTimestamps = _timestampRepo
                            .Get(e => candidateDestTimestamps.Contains(e.TimestampId));

                        lock (SchedulePositionLocksL1)
                        lock (SchedulePositionLocksL2)
                        {
                            foreach (var key in possibleMove.Item1)
                            {
                                var queueL1 = SchedulePositionLocksL1.GetOrAdd(key, new ConcurrentQueue<object>());
                                var queueL2 = SchedulePositionLocksL2.GetOrAdd(key, new ConcurrentQueue<object>());

                                queueL1.Enqueue(new object());
                                L1scheduledMovesAllQueues.Add(key, queueL1);

                                queueL2.Enqueue(new object());
                                L2scheduledMovesAllQueues.Add(key, queueL2);
                            }

                            foreach (var key in possibleMove.Item2)
                            {
                                var queueL1 = SchedulePositionLocksL1.GetOrAdd(key, new ConcurrentQueue<object>());
                                var queueL2 = SchedulePositionLocksL2.GetOrAdd(key, new ConcurrentQueue<object>());

                                if (!_source.Contains(key))
                                {
                                    queueL1.Enqueue(new object());
                                    L1scheduledMovesAllQueues.Add(key, queueL1);
                                }
                                else
                                {
                                    candidateSourceKeys.Add(key);
                                }

                                queueL2.Enqueue(new object());
                                L2scheduledMovesAllQueues.Add(key, queueL2);
                            }
                        }

                        foreach (var scheduledMoveQueue in L1scheduledMovesAllQueues)
                        {
                            if (possibleMove.Item1.Contains(scheduledMoveQueue.Key))
                            {
                                Monitor.Enter(scheduledMoveQueue.Value);
                            }
                            else
                            {
                                bool lockTaken = false;
                                Monitor.TryEnter(scheduledMoveQueue.Value, ref lockTaken);
                                if (!lockTaken)
                                {
                                    L1scheduledMovesAllQueuesNotLocked.Add(scheduledMoveQueue.Key, scheduledMoveQueue.Value);
                                    localKeysToRemove.Add(scheduledMoveQueue.Key);
                                }
                            }
                        }
                        foreach (var schedulePositionQueue in L2scheduledMovesAllQueues.Values)
                        {
                            Monitor.Enter(schedulePositionQueue);
                        }
                        foreach (var schedulePositionQueue in L1scheduledMovesAllQueuesNotLocked)
                        {
                            bool lockTaken = false;
                            Monitor.TryEnter(schedulePositionQueue.Value, ref lockTaken);
                            if (lockTaken)
                            {
                                localKeysToRemove.Remove(schedulePositionQueue.Key);
                            }
                        }
                        try
                        {
                            var _sourceSchedulePositions = _schedulePositionRepo
                            .Get(e => candidateSourceTimestamps.Contains(e.TimestampId) && e.RoomId == candidateSourceRoomId);

                            if (!_sourceSchedulePositions.Any() || _sourceSchedulePositions.Count() != possibleMove.Item1.Length)
                            {
                                _skippedMovesIds.Add((int)currentMoveId);

                                continue;
                            }

                            var schedulePosition = _sourceSchedulePositions.FirstOrDefault();

                            var _courseEdition = _courseEditionRepo
                                .Get(e => e.CourseId == schedulePosition.CourseId &&
                                            e.CourseEditionId == schedulePosition.CourseEditionId)
                                .Include(e => e.Coordinators)
                                .Include(e => e.Groups)
                                    .ThenInclude(e => e.Group);

                            var includableCourseEdition = _courseEdition.FirstOrDefault();
                            if (includableCourseEdition == null)
                            {
                                _skippedMovesIds.Add((int)currentMoveId);

                                continue;
                            }

                            var _scheduledMove = _scheduledMoveRepo
                                .Get(e => e.MoveId == currentMoveId);

                            if (_scheduledMove.Count() != possibleMove.Item2.Length)
                            {
                                _skippedMovesIds.Add((int)currentMoveId);

                                continue;
                            }

                            var coordinatorsIds = includableCourseEdition.Coordinators.Select(e => e.CoordinatorId).ToArray();
                            var groupsIds = CourseEditionsController.GetNestedGroupsIds(includableCourseEdition, _groupRepo).ToArray();
                            var returnableGroupsIds = new int[groupsIds.Length];

                            Array.Sort(coordinatorsIds);
                            Array.Copy(groupsIds, returnableGroupsIds, groupsIds.Length);
                            Array.Sort(groupsIds);

                            lock (CoordinatorPositionLocks)
                            lock (GroupPositionLocks)
                            {
                                foreach (var timestampId in candidateDestTimestamps)
                                {
                                    foreach (var coordinatorId in coordinatorsIds)
                                    {
                                        var key = new CoordinatorPositionKey
                                        { CoordinatorId = coordinatorId, TimestampId = timestampId };
                                        coordinatorPositionKeys.Add(key);
                                        var queue = CoordinatorPositionLocks.GetOrAdd(key, new ConcurrentQueue<object>());
                                        coordinatorPositionQueues.Add(queue);
                                        queue.Enqueue(new object());
                                    }

                                    foreach (var groupId in groupsIds)
                                    {
                                        var key = new GroupPositionKey
                                        { GroupId = groupId, TimestampId = timestampId };
                                        groupPositionKeys.Add(key);
                                        var queue = GroupPositionLocks.GetOrAdd(key, new ConcurrentQueue<object>());
                                        groupPositionQueues.Add(queue);
                                        queue.Enqueue(new object());
                                    }
                                }
                            }

                            foreach (var coordinatorPositionQueue in coordinatorPositionQueues)
                            {
                                Monitor.Enter(coordinatorPositionQueue);
                            }
                            foreach (var groupPositionQueue in groupPositionQueues)
                            {
                                Monitor.Enter(groupPositionQueue);
                            }
                            try
                            {
                                var _destSchedulePositions = candidateSourceTimestamps.SequenceEqual(candidateDestTimestamps)
                                ?
                                _schedulePositionRepo
                                        .Get(e => candidateDestTimestamps.Contains(e.TimestampId) && e.RoomId == candidateDestRoomId)
                                        .Include(e => e.CourseEdition)
                                        .ThenInclude(e => e.Coordinators)
                                        .Include(e => e.CourseEdition)
                                        .ThenInclude(e => e.Groups)
                                        .Select(e => new { e.TimestampId, e.RoomId })
                                :
                                _schedulePositionRepo
                                .Get(e => candidateDestTimestamps.Contains(e.TimestampId)
                                            && (e.RoomId == candidateDestRoomId || e.CourseEdition.Coordinators
                                                                        .Select(e => e.CoordinatorId)
                                                                        .Any(e => coordinatorsIds.Contains(e))
                                                                    || e.CourseEdition.Groups.Select(e => e.GroupId)
                                                                        .Any(e => groupsIds.Contains(e))))
                                    .Include(e => e.CourseEdition)
                                    .ThenInclude(e => e.Coordinators)
                                    .Include(e => e.CourseEdition)
                                    .ThenInclude(e => e.Groups)
                                    .Select(e => new { e.TimestampId, e.RoomId });

                                if (_destSchedulePositions.Any())
                                {
                                    _skippedMovesIds.Add((int)currentMoveId);

                                    continue;
                                }

                                var _courseRoomTimestamps = _courseRoomTimestampRepo
                                    .Get(e => e.RoomId == candidateDestRoomId && candidateDestTimestamps.Contains(e.TimestampId) &&
                                                e.CourseId == includableCourseEdition.CourseId)
                                    .Select(e => e.TimestampId)
                                    .OrderBy(e => e);

                                var destSchedulePositions = candidateDestTimestamps.Select(timestampId => new SchedulePosition
                                {
                                    RoomId = candidateDestRoomId,
                                    TimestampId = timestampId,
                                    CourseId = includableCourseEdition.CourseId,
                                    CourseEditionId = includableCourseEdition.CourseEditionId,
                                    CourseRoomTimestamp = !_courseRoomTimestamps.Contains(timestampId) ? new CourseRoomTimestamp
                                    {
                                        RoomId = candidateDestRoomId,
                                        TimestampId = timestampId,
                                        CourseId = includableCourseEdition.CourseId
                                    } : null
                                }).ToList();

                                var movesIds = _scheduledMoveRepo
                                    .Get(e => e.RoomId_1 == candidateSourceRoomId && candidateSourceTimestamps.Contains(e.TimestampId_1)
                                        && e.CourseId == includableCourseEdition.CourseId)
                                    .GroupBy(e => e.MoveId)
                                    .Select(e => e.Key)
                                    .OrderBy(e => e).ToList();

                                var srcPeriodIndex = _srcTimestamps.FirstOrDefault().PeriodIndex;
                                var srcDay = _srcTimestamps.FirstOrDefault().Day;
                                var srcWeeks = _srcTimestamps.Select(e => e.Week).OrderBy(e => e).ToArray();
                                
                                var destPeriodIndex = _destTimestamps.FirstOrDefault().PeriodIndex;
                                var destDay = _destTimestamps.FirstOrDefault().Day;
                                var destWeeks = _destTimestamps.Select(e => e.Week).OrderBy(e => e).ToArray();

                                _scheduledMoveRepo.DeleteMany(e => movesIds.Contains(e.MoveId));
                                _schedulePositionRepo.GetAll().RemoveRange(_sourceSchedulePositions);
                                _schedulePositionRepo.GetAll().AddRange(destSchedulePositions);

                                var result1a = _scheduledMoveRepo.SaveChanges().Result;
                                var result1b = _schedulePositionRepo.SaveChanges().Result;
                                
                                var result2b = Clients.All.ModifiedSchedulePositions(
                                    includableCourseEdition.CourseId, includableCourseEdition.CourseEditionId,
                                    returnableGroupsIds, includableCourseEdition.Groups.Count, coordinatorsIds,
                                    candidateSourceRoomId, candidateDestRoomId,
                                    srcPeriodIndex, destPeriodIndex,
                                    srcDay, destDay,
                                    srcWeeks, destWeeks,
                                    movesIds.ToArray()
                                );

                                _skippedMovesIds.Add((int)currentMoveId);

                                _sources.Enqueue(new List<SchedulePositionKey>(possibleMove.Item1));
                                isQueued = true;

                                foreach (var candidateSourceKey in candidateSourceKeys)
                                {
                                    _source.Remove(candidateSourceKey);
                                }
                            }
                            finally
                            {
                                RemoveGroupPositionsLocks(groupPositionQueues, groupPositionKeys);
                                RemoveCoordinatorPositionsLocks(coordinatorPositionQueues, coordinatorPositionKeys);
                                foreach (var groupPositionQueue in groupPositionQueues)
                                {
                                    Monitor.Exit(groupPositionQueue);
                                }
                                foreach (var coordinatorPositionQueue in coordinatorPositionQueues)
                                {
                                    Monitor.Exit(coordinatorPositionQueue);
                                }
                            }
                        }
                        finally
                        {
                            RemoveSchedulePositionsLocksL2(L2scheduledMovesAllQueues);
                            foreach (var scheduledMoveQueue in L2scheduledMovesAllQueues.Values)
                            {
                                Monitor.Exit(scheduledMoveQueue);
                            }
                            if (!isQueued)
                            {
                                foreach (var keyToRemove in localKeysToRemove)
                                {
                                    L1scheduledMovesAllQueues.Remove(keyToRemove);
                                }

                                RemoveSchedulePositionsLocksL1(L1scheduledMovesAllQueues);
                                foreach (var scheduledMoveQueue in L1scheduledMovesAllQueues.Values)
                                {
                                    Monitor.Exit(scheduledMoveQueue);
                                }
                            }
                            else
                            {
                                foreach (var scheduledMoveQueue in L1scheduledMovesAllQueues)
                                {
                                    if (possibleMove.Item1.Contains(scheduledMoveQueue.Key))
                                    {
                                        L1schedulePositionAllQueues.Add(scheduledMoveQueue.Key, scheduledMoveQueue.Value);
                                        localKeysToRemove.Add(scheduledMoveQueue.Key);
                                    }
                                }
                                foreach (var candidateSourceKey in candidateSourceKeys)
                                {
                                    if (L1schedulePositionAllQueues.Remove(candidateSourceKey, out var queue))
                                    {
                                        L1scheduledMovesAllQueues.Add(candidateSourceKey, queue);
                                    }
                                }
                                foreach (var keyToRemove in localKeysToRemove)
                                {
                                    L1scheduledMovesAllQueues.Remove(keyToRemove);
                                }

                                RemoveSchedulePositionsLocksL1(L1scheduledMovesAllQueues);
                                foreach (var scheduledMoveQueue in L1scheduledMovesAllQueues.Values)
                                {
                                    Monitor.Exit(scheduledMoveQueue);
                                }
                            }
                        }
                    }
                }
                finally
                {
                    foreach (var schedulePositionQueue in L1schedulePositionAllQueues)
                    {
                        if (_source.Contains(schedulePositionQueue.Key))
                        {
                            RemoveSchedulePositionLockL1(L1schedulePositionAllQueues, schedulePositionQueue.Key);
                            Monitor.Exit(schedulePositionQueue.Value);
                            L1KeysToRemove.Add(schedulePositionQueue.Key);
                        }
                    }
                }
            }
        }

        public ScheduleHub(
            ISettingsRepo settingsRepo,
            ITimestampRepo timestampRepo,
            ICourseEditionRepo courseEditionRepo,
            ICourseRoomTimestampRepo courseRoomTimestampRepo,
            ISchedulePositionRepo schedulePositionRepo,
            IScheduledMoveRepo scheduledMoveRepo,
            IGroupRepo groupRepo
            )
        {
            _settingsRepo = settingsRepo;
            _timestampRepo = timestampRepo;
            _courseEditionRepo = courseEditionRepo;
            _courseRoomTimestampRepo = courseRoomTimestampRepo;
            _schedulePositionRepo = schedulePositionRepo;
            _scheduledMoveRepo = scheduledMoveRepo;
            _groupRepo = groupRepo;
        }

        [Authorize(Policy = "Coordinator")]
        public MessageObject LockCourseEdition(int courseId, int courseEditionId)
        {
            CourseEditionKey courseEditionKey = null;
            ConcurrentQueue<object> courseEditionQueue = null;
            
            var enqueued = false;
            
            try
            {
                var userId = int.Parse(Context.User.Claims.FirstOrDefault(x => x.Type == "user_id")?.Value!);

                courseEditionKey = new CourseEditionKey { CourseId = courseId, CourseEditionId = courseEditionId };
                courseEditionQueue = CourseEditionLocks.GetOrAdd(courseEditionKey, new ConcurrentQueue<object>());

                courseEditionQueue.Enqueue(new object());

                enqueued = true;

                lock (courseEditionQueue)
                {
                    var _courseEdition = _courseEditionRepo
                        .Get(e => e.Coordinators.Any(e => e.CoordinatorId == userId) && e.CourseId == courseId &&
                                  e.CourseEditionId == courseEditionId)
                        .Include(e => e.Coordinators);

                    if (!_courseEdition.Any())
                    {
                        RemoveCourseEditionLock(courseEditionQueue, courseEditionKey);
                        
                        return new MessageObject {StatusCode = 404};
                    }

                    var courseEdition = _courseEdition.FirstOrDefault();
                    if (!(courseEdition is {LockUserId: null}))
                    {
                        RemoveCourseEditionLock(courseEditionQueue, courseEditionKey);
                        
                        return new MessageObject {StatusCode = 400, Message = "Someone has locked this course before you." };
                    }

                    courseEdition.LockUserId = userId;
                    courseEdition.LockUserConnectionId = Context.ConnectionId;
                    _courseEditionRepo.Update(courseEdition);

                    var result1 = _courseEditionRepo.SaveChanges().Result;
                    var result2 = Clients.All.LockCourseEdition(courseEdition.CourseId, courseEdition.CourseEditionId);

                    RemoveCourseEditionLock(courseEditionQueue, courseEditionKey);

                    return new MessageObject { StatusCode = 200 };
                }
            }
            catch (Exception e)
            {
                if (!enqueued)
                {
                    return new MessageObject {StatusCode = 400, Message = e.Message};
                }

                lock (courseEditionQueue)
                {
                    RemoveCourseEditionLock(courseEditionQueue, courseEditionKey);
                }

                return new MessageObject { StatusCode = 400, Message = e.Message };
            }
        }

        [Authorize(Policy = "Coordinator")]
        public MessageObject LockSchedulePositions(int roomId, int periodIndex, int day, int[] weeks)
        {
            var schedulePositionKeys = new List<SchedulePositionKey>();
            var schedulePositionQueuesL1 = new List<ConcurrentQueue<object>>();
            var schedulePositionQueuesL2 = new List<ConcurrentQueue<object>>();

            try
            {
                var userId = int.Parse(Context.User.Claims.FirstOrDefault(x => x.Type == "user_id")?.Value!);

                var _timestamps = _timestampRepo
                        .Get(e => e.PeriodIndex == periodIndex && e.Day == day && weeks.Contains(e.Week))
                        .Select(e => e.TimestampId)
                        .OrderBy(e => e).ToList();

                if (_timestamps.Count != weeks.Length)
                {
                    return new MessageObject { StatusCode = 404, Message = "Could not find requested time periods." };
                }

                lock (SchedulePositionLocksL1)
                {
                    foreach (var timestampId in _timestamps)
                    {
                        var key = new SchedulePositionKey { RoomId = roomId, TimestampId = timestampId };
                        schedulePositionKeys.Add(key);
                        var queue = SchedulePositionLocksL1.GetOrAdd(key, new ConcurrentQueue<object>());
                        schedulePositionQueuesL1.Add(queue);
                        queue.Enqueue(new object());
                    }
                }

                foreach (var schedulePositionQueue in schedulePositionQueuesL1)
                {
                    Monitor.Enter(schedulePositionQueue);
                }
                try
                {
                    lock (SchedulePositionLocksL2)
                    {
                        foreach (var key in schedulePositionKeys)
                        {
                            var queue = SchedulePositionLocksL2.GetOrAdd(key, new ConcurrentQueue<object>());
                            schedulePositionQueuesL2.Add(queue);
                            queue.Enqueue(new object());
                        }
                    }

                    foreach (var schedulePositionQueue in schedulePositionQueuesL2)
                    {
                        Monitor.Enter(schedulePositionQueue);
                    }
                    try
                    {
                        var _schedulePositions = _schedulePositionRepo
                        .Get(e => _timestamps.Contains(e.TimestampId) && e.RoomId == roomId &&
                                  e.CourseEdition.Coordinators.Any(e => e.CoordinatorId == userId))
                        .Include(e => e.CourseEdition)
                        .ThenInclude(e => e.Coordinators);

                        if (_schedulePositions.Count() != weeks.Length)
                        {
                            return new MessageObject { StatusCode = 404, Message = "Could not find requested positions in schedule." };
                        }

                        if (Enumerable.Any(_schedulePositions, schedulePosition => schedulePosition.LockUserId != null))
                        {
                            return new MessageObject { StatusCode = 400, Message = "Someone has locked these positions in schedule before you." };
                        }

                        var courseEdition = _schedulePositions.FirstOrDefault()?.CourseEdition;
                        if (courseEdition == null)
                        {
                            return new MessageObject { StatusCode = 400, Message = "There was an unexpected error." };
                        }

                        foreach (var schedulePosition in _schedulePositions)
                        {
                            schedulePosition.LockUserId = userId;
                            schedulePosition.LockUserConnectionId = Context.ConnectionId;
                        }

                        _schedulePositionRepo.GetAll().UpdateRange(_schedulePositions);

                        var result1 = _schedulePositionRepo.SaveChanges().Result;
                        var result2 = Clients.Others.LockSchedulePositions(
                            courseEdition.CourseId, courseEdition.CourseEditionId,
                            roomId, periodIndex,
                            day, weeks
                        );

                        return new MessageObject { StatusCode = 200 };
                    }
                    finally
                    {
                        RemoveSchedulePositionsLocksL2(schedulePositionQueuesL2, schedulePositionKeys);
                        foreach (var schedulePositionQueue in schedulePositionQueuesL2)
                        {
                            Monitor.Exit(schedulePositionQueue);
                        }
                    }
                }
                finally
                {
                    RemoveSchedulePositionsLocksL1(schedulePositionQueuesL1, schedulePositionKeys);
                    foreach (var schedulePositionQueue in schedulePositionQueuesL1)
                    {
                        Monitor.Exit(schedulePositionQueue);
                    }
                }
            }
            catch (Exception e)
            {
                return new MessageObject { StatusCode = 400, Message = e.Message };
            }
        }

        [Authorize(Policy = "Coordinator")]
        public MessageObject UnlockCourseEdition(int courseId, int courseEditionId)
        {
            CourseEditionKey courseEditionKey = null;
            ConcurrentQueue<object> courseEditionQueue = null;
            
            var enqueued = false;
            
            try
            {
                var userId = int.Parse(Context.User.Claims.FirstOrDefault(x => x.Type == "user_id")?.Value!);

                courseEditionKey = new CourseEditionKey { CourseId = courseId, CourseEditionId = courseEditionId };
                courseEditionQueue = CourseEditionLocks.GetOrAdd(courseEditionKey, new ConcurrentQueue<object>());

                courseEditionQueue.Enqueue(new object());

                enqueued = true;

                lock (courseEditionQueue)
                {
                    var _courseEdition = _courseEditionRepo
                        .Get(e => e.Coordinators.Any(e => e.CoordinatorId == userId) && e.CourseId == courseId &&
                                  e.CourseEditionId == courseEditionId)
                        .Include(e => e.Coordinators);

                    if (!_courseEdition.Any())
                    {
                        RemoveCourseEditionLock(courseEditionQueue, courseEditionKey);

                        return new MessageObject { StatusCode = 404 };
                    }

                    var courseEdition = _courseEdition.FirstOrDefault();
                    if (courseEdition.LockUserId == null)
                    {
                        RemoveCourseEditionLock(courseEditionQueue, courseEditionKey);

                        return new MessageObject { StatusCode = 400, Message = "This course edition is already unlocked." };
                    }

                    if (courseEdition.LockUserId != userId || courseEdition.LockUserConnectionId != Context.ConnectionId)
                    {
                        RemoveCourseEditionLock(courseEditionQueue, courseEditionKey);

                        return new MessageObject { StatusCode = 400, Message = "You cannot unlock this course edition." };
                    }

                    courseEdition.LockUserId = null;
                    courseEdition.LockUserConnectionId = null;
                    _courseEditionRepo.Update(courseEdition);

                    var result1 = _courseEditionRepo.SaveChanges().Result;
                    var result2 = Clients.All.UnlockCourseEdition(courseEdition.CourseId, courseEdition.CourseEditionId);

                    RemoveCourseEditionLock(courseEditionQueue, courseEditionKey);

                    return new MessageObject { StatusCode = 200 };
                }
            }
            catch (Exception e)
            {
                if (!enqueued)
                {
                    return new MessageObject { StatusCode = 400, Message = e.Message };
                }

                lock (courseEditionQueue)
                {
                    RemoveCourseEditionLock(courseEditionQueue, courseEditionKey);
                }

                return new MessageObject { StatusCode = 400, Message = e.Message };
            }
        }

        [Authorize(Policy = "Coordinator")]
        public MessageObject UnlockSchedulePositions(int roomId, int periodIndex, int day, int[] weeks)
        {
            var schedulePositionKeys = new List<SchedulePositionKey>();
            var schedulePositionQueuesL1 = new List<ConcurrentQueue<object>>();
            var schedulePositionQueuesL2 = new List<ConcurrentQueue<object>>();

            try
            {
                var userId = int.Parse(Context.User.Claims.FirstOrDefault(x => x.Type == "user_id")?.Value!);

                var _timestamps = _timestampRepo
                        .Get(e => e.PeriodIndex == periodIndex && e.Day == day && weeks.Contains(e.Week))
                        .Select(e => e.TimestampId)
                        .OrderBy(e => e).ToList();

                if (_timestamps.Count != weeks.Length)
                {
                    return new MessageObject { StatusCode = 404, Message = "Could not find requested time periods." };
                }

                lock (SchedulePositionLocksL1)
                {
                    foreach (var timestampId in _timestamps)
                    {
                        var key = new SchedulePositionKey { RoomId = roomId, TimestampId = timestampId };
                        schedulePositionKeys.Add(key);
                        var queue = SchedulePositionLocksL1.GetOrAdd(key, new ConcurrentQueue<object>());
                        schedulePositionQueuesL1.Add(queue);
                        queue.Enqueue(new object());
                    }
                }

                foreach (var schedulePositionQueue in schedulePositionQueuesL1)
                {
                    Monitor.Enter(schedulePositionQueue);
                }
                try
                {
                    lock (SchedulePositionLocksL2)
                    {
                        foreach (var key in schedulePositionKeys)
                        {
                            var queue = SchedulePositionLocksL2.GetOrAdd(key, new ConcurrentQueue<object>());
                            schedulePositionQueuesL2.Add(queue);
                            queue.Enqueue(new object());
                        }
                    }

                    foreach (var schedulePositionQueue in schedulePositionQueuesL2)
                    {
                        Monitor.Enter(schedulePositionQueue);
                    }
                    try
                    {
                        var _schedulePositions = _schedulePositionRepo
                        .Get(e => _timestamps.Contains(e.TimestampId) && e.RoomId == roomId &&
                                  e.CourseEdition.Coordinators.Any(e => e.CoordinatorId == userId))
                        .Include(e => e.CourseEdition)
                        .ThenInclude(e => e.Coordinators);

                        if (_schedulePositions.Count() != weeks.Length)
                        {
                            return new MessageObject { StatusCode = 404, Message = "Could not find requested positions in schedule." };
                        }

                        foreach (var schedulePosition in _schedulePositions)
                        {
                            if (schedulePosition.LockUserId == null)
                            {
                                return new MessageObject { StatusCode = 400, Message = "Someone has locked these positions in schedule before you." };
                            }

                            if (schedulePosition.LockUserId != userId || schedulePosition.LockUserConnectionId != Context.ConnectionId)
                            {
                                return new MessageObject { StatusCode = 400, Message = "You cannot unlock some of positions in the schedule." };
                            }
                        }

                        var courseEdition = _schedulePositions.FirstOrDefault()?.CourseEdition;
                        if (courseEdition == null)
                        {
                            return new MessageObject { StatusCode = 400, Message = "There was an unexpected error." };
                        }

                        foreach (var schedulePosition in _schedulePositions)
                        {
                            schedulePosition.LockUserId = null;
                            schedulePosition.LockUserConnectionId = null;
                        }

                        _schedulePositionRepo.GetAll().UpdateRange(_schedulePositions);

                        var result1 = _schedulePositionRepo.SaveChanges().Result;
                        var result2 = Clients.All.UnlockSchedulePositions(
                            courseEdition.CourseId, courseEdition.CourseEditionId,
                            roomId, periodIndex,
                            day, weeks);

                        return new MessageObject { StatusCode = 200 };
                    }
                    finally
                    {
                        RemoveSchedulePositionsLocksL2(schedulePositionQueuesL2, schedulePositionKeys);
                        foreach (var schedulePositionQueue in schedulePositionQueuesL2)
                        {
                            Monitor.Exit(schedulePositionQueue);
                        }
                    }
                }
                finally
                {
                    RemoveSchedulePositionsLocksL1(schedulePositionQueuesL1, schedulePositionKeys);
                    foreach (var schedulePositionQueue in schedulePositionQueuesL1)
                    {
                        Monitor.Exit(schedulePositionQueue);
                    }
                }
            }
            catch (Exception e)
            {
                return new MessageObject { StatusCode = 400, Message = e.Message };
            }
        }
        
        [Authorize(Policy = "Coordinator")]
        public void AddSchedulePositions(int courseId, int courseEditionId, int roomId, int periodIndex, int day, int[] weeks)
        {
            CourseEditionKey courseEditionKey = null;
            var schedulePositionKeys = new List<SchedulePositionKey>();
            var coordinatorPositionKeys = new List<CoordinatorPositionKey>();
            var groupPositionKeys = new List<GroupPositionKey>();
            ConcurrentQueue<object> courseEditionQueue = null;
            var schedulePositionQueuesL1 = new List<ConcurrentQueue<object>>();
            var schedulePositionQueuesL2 = new List<ConcurrentQueue<object>>();
            var coordinatorPositionQueues = new List<ConcurrentQueue<object>>();
            var groupPositionQueues = new List<ConcurrentQueue<object>>();

            weeks = weeks.Distinct().ToArray();
            Array.Sort(weeks);

            try
            {
                var userId = int.Parse(Context.User.Claims.FirstOrDefault(x => x.Type == "user_id")?.Value!);

                var _timestamps = _timestampRepo
                            .Get(e => e.PeriodIndex == periodIndex && e.Day == day && weeks.Contains(e.Week))
                            .Select(e => e.TimestampId)
                            .OrderBy(e => e).ToList();

                if (_timestamps.Count != weeks.Length)
                {
                    Clients.Caller.SendResponse(new MessageObject { StatusCode = 404, Message = "Could not find requested time periods." });
                    return;
                }

                courseEditionKey = new CourseEditionKey { CourseId = courseId, CourseEditionId = courseEditionId };
                courseEditionQueue = CourseEditionLocks.GetOrAdd(courseEditionKey, new ConcurrentQueue<object>());
                courseEditionQueue.Enqueue(new object());

                lock (SchedulePositionLocksL1)
                {
                    foreach (var timestampId in _timestamps)
                    {
                        var key = new SchedulePositionKey { RoomId = roomId, TimestampId = timestampId };
                        schedulePositionKeys.Add(key);
                        var queue = SchedulePositionLocksL1.GetOrAdd(key, new ConcurrentQueue<object>());
                        schedulePositionQueuesL1.Add(queue);
                        queue.Enqueue(new object());
                    }
                }

                Monitor.Enter(courseEditionQueue);
                foreach (var schedulePositionQueue in schedulePositionQueuesL1)
                {
                    Monitor.Enter(schedulePositionQueue);
                }
                try
                {
                    var _courseEdition = _courseEditionRepo
                        .Get(e => e.CourseId == courseId && e.CourseEditionId == courseEditionId)
                        .Include(e => e.Coordinators)
                        .Include(e => e.Groups)
                        .ThenInclude(e => e.Group)
                        .Include(e => e.SchedulePositions)
                        .Include(e => e.Course)
                        .ThenInclude(e => e.Rooms);

                    if (!_courseEdition.Any())
                    {
                        Clients.Caller.SendResponse(new MessageObject { StatusCode = 404 });
                        return;
                    }

                    var courseEdition = _courseEdition.FirstOrDefault();
                    if (courseEdition.LockUserId != userId || courseEdition.LockUserConnectionId != Context.ConnectionId)
                    {
                        Clients.Caller.SendResponse(new MessageObject { StatusCode = 400, Message = "You didn't lock this course edition." });
                        return;
                    }

                    var _settings = _settingsRepo.GetSettings().Result;
                    if (_settings == null)
                    {
                        Clients.Caller.SendResponse(new MessageObject { StatusCode = 400, Message = "Application settings has not been specified." });
                        return;
                    }

                    if (!courseEdition.Course.Rooms.Select(e => e.RoomId).Contains(roomId))
                    {
                        Clients.Caller.SendResponse(new MessageObject { StatusCode = 400, Message = "Chosen room does not exist or has not been assigned to this course." });
                        return;
                    }

                    var courseDurationMinutes = _settings.CourseDurationMinutes;
                    if (Math.Ceiling(courseEdition.Course.UnitsMinutes / (courseDurationMinutes * 1.0)) -
                        courseEdition.SchedulePositions.Count < weeks.Length)
                    {
                        Clients.Caller.SendResponse(new MessageObject { StatusCode = 400, Message = "You cannot add this amount of units to the schedule." });
                        return;
                    }

                    var coordinatorsIds = courseEdition.Coordinators.Select(e => e.CoordinatorId).ToArray();
                    var groupsIds = CourseEditionsController.GetNestedGroupsIds(courseEdition, _groupRepo).ToArray();
                    var returnableGroupsIds = new int[groupsIds.Length];

                    Array.Sort(coordinatorsIds);
                    Array.Copy(groupsIds, returnableGroupsIds, groupsIds.Length);
                    Array.Sort(groupsIds);

                    lock (SchedulePositionLocksL2)
                    lock (CoordinatorPositionLocks)
                    lock (GroupPositionLocks)
                    {
                        foreach (var key in schedulePositionKeys)
                        {
                            var queue = SchedulePositionLocksL2.GetOrAdd(key, new ConcurrentQueue<object>());
                            schedulePositionQueuesL2.Add(queue);
                            queue.Enqueue(new object());
                        }

                        foreach (var timestampId in _timestamps)
                        {
                            foreach (var coordinatorId in coordinatorsIds)
                            {
                                var key = new CoordinatorPositionKey
                                    { CoordinatorId = coordinatorId, TimestampId = timestampId };
                                coordinatorPositionKeys.Add(key);
                                var queue = CoordinatorPositionLocks.GetOrAdd(key, new ConcurrentQueue<object>());
                                coordinatorPositionQueues.Add(queue);
                                queue.Enqueue(new object());
                            }

                            foreach (var groupId in groupsIds)
                            {
                                var key = new GroupPositionKey
                                    { GroupId = groupId, TimestampId = timestampId };
                                groupPositionKeys.Add(key);
                                var queue = GroupPositionLocks.GetOrAdd(key, new ConcurrentQueue<object>());
                                groupPositionQueues.Add(queue);
                                queue.Enqueue(new object());
                            }
                        }
                    }
                    
                    foreach (var schedulePositionQueue in schedulePositionQueuesL2)
                    {
                        Monitor.Enter(schedulePositionQueue);
                    }
                    foreach (var coordinatorPositionQueue in coordinatorPositionQueues)
                    {
                        Monitor.Enter(coordinatorPositionQueue);
                    }
                    foreach (var groupPositionQueue in groupPositionQueues)
                    {
                        Monitor.Enter(groupPositionQueue);
                    }
                    try
                    {
                        var _schedulePositions = _schedulePositionRepo
                            .Get(e => _timestamps.Contains(e.TimestampId)
                                      && (e.RoomId == roomId || e.CourseEdition.Coordinators
                                                                 .Select(e => e.CoordinatorId)
                                                                 .Any(e => coordinatorsIds.Contains(e))
                                                             || e.CourseEdition.Groups.Select(e => e.GroupId)
                                                                 .Any(e => groupsIds.Contains(e))))
                            .Include(e => e.CourseEdition)
                            .ThenInclude(e => e.Coordinators)
                            .Include(e => e.CourseEdition)
                            .ThenInclude(e => e.Groups)
                            .Select(e => new { e.TimestampId, e.RoomId });

                        if (_schedulePositions.Any())
                        {
                            Clients.Caller.SendResponse(new MessageObject { StatusCode = 400, Message = "Some conflicts with other courses occurred." });
                            return;
                        }

                        var _courseRoomTimestamps = _courseRoomTimestampRepo
                            .Get(e => e.RoomId == roomId && _timestamps.Contains(e.TimestampId) &&
                                      e.CourseId == courseEdition.CourseId)
                            .Select(e => e.TimestampId)
                            .OrderBy(e => e);

                        var schedulePositions = _timestamps.Select(timestampId => new SchedulePosition
                        {
                            RoomId = roomId,
                            TimestampId = timestampId,
                            CourseId = courseEdition.CourseId,
                            CourseEditionId = courseEdition.CourseEditionId,
                            CourseRoomTimestamp = !_courseRoomTimestamps.Contains(timestampId) ? new CourseRoomTimestamp
                            {
                                RoomId = roomId,
                                TimestampId = timestampId,
                                CourseId = courseEdition.CourseId
                            } : null
                        }).ToList();

                        _schedulePositionRepo.GetAll().AddRange(schedulePositions);

                        var result1 = _schedulePositionRepo.SaveChanges().Result;
                        var result2 = Clients.Others.AddedSchedulePositions(
                            courseEdition.CourseId, courseEdition.CourseEditionId,
                            returnableGroupsIds, courseEdition.Groups.Count, coordinatorsIds,
                            roomId, periodIndex,
                            day, weeks
                        );

                        Clients.Caller.SendResponse(new MessageObject { StatusCode = 200 });
                        return;
                    }
                    finally
                    {
                        RemoveGroupPositionsLocks(groupPositionQueues, groupPositionKeys);
                        RemoveCoordinatorPositionsLocks(coordinatorPositionQueues, coordinatorPositionKeys);
                        RemoveSchedulePositionsLocksL2(schedulePositionQueuesL2, schedulePositionKeys);
                        foreach (var groupPositionQueue in groupPositionQueues)
                        {
                            Monitor.Exit(groupPositionQueue);
                        }
                        foreach (var coordinatorPositionQueue in coordinatorPositionQueues)
                        {
                            Monitor.Exit(coordinatorPositionQueue);
                        }
                        foreach (var schedulePositionQueue in schedulePositionQueuesL2)
                        {
                            Monitor.Exit(schedulePositionQueue);
                        }
                    }
                }
                finally
                {
                    RemoveSchedulePositionsLocksL1(schedulePositionQueuesL1, schedulePositionKeys);
                    RemoveCourseEditionLock(courseEditionQueue, courseEditionKey);
                    foreach (var schedulePositionQueue in schedulePositionQueuesL1)
                    {
                        Monitor.Exit(schedulePositionQueue);
                    }
                    Monitor.Exit(courseEditionQueue);
                }
            }
            catch (Exception e)
            {
                Clients.Caller.SendResponse(new MessageObject { StatusCode = 400, Message = e.Message });
                return;
            }
        }

        [Authorize(Policy = "Coordinator")]
        public void ModifySchedulePositions(
            int roomId, int periodIndex, int day, int[] weeks, 
            int destRoomId, int destPeriodIndex, int destDay, int[] destWeeks
        )
        {
            var schedulePositionKeys1 = new List<SchedulePositionKey>();
            var schedulePositionKeys2 = new List<SchedulePositionKey>();
            var coordinatorPositionKeys = new List<CoordinatorPositionKey>();
            var groupPositionKeys = new List<GroupPositionKey>();
            var L1schedulePositionQueues1 = new List<ConcurrentQueue<object>>();
            var L1schedulePositionQueues2 = new List<ConcurrentQueue<object>>();
            var L2schedulePositionQueues1 = new List<ConcurrentQueue<object>>();
            var L2schedulePositionQueues2 = new List<ConcurrentQueue<object>>();
            var L1schedulePositionAllQueues = new SortedList<SchedulePositionKey, ConcurrentQueue<object>>();
            var L2schedulePositionAllQueues = new SortedList<SchedulePositionKey, ConcurrentQueue<object>>();
            var coordinatorPositionQueues = new List<ConcurrentQueue<object>>();
            var groupPositionQueues = new List<ConcurrentQueue<object>>();

            weeks = weeks.Distinct().ToArray();
            Array.Sort(weeks);
            destWeeks = destWeeks.Distinct().ToArray();
            Array.Sort(destWeeks);

            if (weeks.Length != destWeeks.Length)
            {
                Clients.Caller.SendResponse(new MessageObject { StatusCode = 400, Message = "Amount of weeks must be equal." });
                return;
            }

            try
            {
                var userId = int.Parse(Context.User.Claims.FirstOrDefault(x => x.Type == "user_id")?.Value!);

                var _sourceTimestamps = _timestampRepo
                        .Get(e => e.PeriodIndex == periodIndex && e.Day == day && weeks.Contains(e.Week))
                        .Select(e => e.TimestampId)
                        .OrderBy(e => e).ToList();

                if (_sourceTimestamps.Count != weeks.Length)
                {
                    Clients.Caller.SendResponse(new MessageObject { StatusCode = 404, Message = "Could not find requested source time periods." });
                    return;
                }

                var _destTimestamps = _timestampRepo
                        .Get(e => e.PeriodIndex == destPeriodIndex && e.Day == destDay && destWeeks.Contains(e.Week))
                        .Select(e => e.TimestampId)
                        .OrderBy(e => e).ToList();

                if (_destTimestamps.Count != destWeeks.Length)
                {
                    Clients.Caller.SendResponse(new MessageObject { StatusCode = 404, Message = "Could not find requested destination time periods." });
                    return;
                }

                lock (SchedulePositionLocksL1)
                {
                    foreach (var timestampId in _sourceTimestamps)
                    {
                        var key = new SchedulePositionKey { RoomId = roomId, TimestampId = timestampId };
                        schedulePositionKeys1.Add(key);
                        var queue = SchedulePositionLocksL1.GetOrAdd(key, new ConcurrentQueue<object>());
                        L1schedulePositionQueues1.Add(queue);
                        queue.Enqueue(new object());

                        L1schedulePositionAllQueues.Add(key, queue);
                    }

                    foreach (var timestampId in _destTimestamps)
                    {
                        var key = new SchedulePositionKey { RoomId = destRoomId, TimestampId = timestampId };
                        schedulePositionKeys2.Add(key);
                        var queue = SchedulePositionLocksL1.GetOrAdd(key, new ConcurrentQueue<object>());
                        L1schedulePositionQueues2.Add(queue);
                        queue.Enqueue(new object());

                        L1schedulePositionAllQueues.Add(key, queue);
                    }
                }
                var L1KeysToRemove = new List<SchedulePositionKey>();

                foreach (var schedulePositionQueue in L1schedulePositionAllQueues.Values)
                {
                    Monitor.Enter(schedulePositionQueue);
                }
                try
                {
                    lock (SchedulePositionLocksL2)
                    {
                        foreach (var key in schedulePositionKeys1)
                        {
                            var queue = SchedulePositionLocksL2.GetOrAdd(key, new ConcurrentQueue<object>());
                            L2schedulePositionQueues1.Add(queue);
                            queue.Enqueue(new object());

                            L2schedulePositionAllQueues.Add(key, queue);
                        }

                        foreach (var key in schedulePositionKeys2)
                        {
                            var queue = SchedulePositionLocksL2.GetOrAdd(key, new ConcurrentQueue<object>());
                            L2schedulePositionQueues2.Add(queue);
                            queue.Enqueue(new object());

                            L2schedulePositionAllQueues.Add(key, queue);
                        }
                    }

                    foreach (var schedulePositionQueue in L2schedulePositionAllQueues.Values)
                    {
                        Monitor.Enter(schedulePositionQueue);
                    }
                    try
                    {
                        var _sourceSchedulePositions = _schedulePositionRepo
                        .Get(e => _sourceTimestamps.Contains(e.TimestampId) && e.RoomId == roomId &&
                                  e.CourseEdition.Coordinators.Any(e => e.CoordinatorId == userId))
                        .Include(e => e.CourseEdition)
                            .ThenInclude(e => e.Coordinators);

                        if (_sourceSchedulePositions.Count() != _sourceTimestamps.Count)
                        {
                            Clients.Caller.SendResponse(new MessageObject { StatusCode = 404, Message = "Could not find requested positions in schedule." });
                            return;
                        }

                        if (Enumerable.Any(_sourceSchedulePositions, schedulePosition => schedulePosition.LockUserId != userId || schedulePosition.LockUserConnectionId != Context.ConnectionId))
                        {
                            Clients.Caller.SendResponse(new MessageObject { StatusCode = 400, Message = "You didn't lock some positions in schedule." });
                            return;
                        }
                        
                        var schedulePosition = _sourceSchedulePositions.FirstOrDefault();

                        var _courseEdition = _courseEditionRepo
                            .Get(e => e.CourseId == schedulePosition.CourseId &&
                                      e.CourseEditionId == schedulePosition.CourseEditionId)
                            .Include(e => e.Course)
                                .ThenInclude(e => e.Rooms)
                            .Include(e => e.Coordinators)
                            .Include(e => e.Groups)
                                .ThenInclude(e => e.Group);

                        var includableCourseEdition = _courseEdition.FirstOrDefault();
                        if (includableCourseEdition == null)
                        {
                            Clients.Caller.SendResponse(new MessageObject { StatusCode = 400, Message = "Could not find course edition for requested positions in schedule." });
                            return;
                        }

                        if (!includableCourseEdition.Course.Rooms.Select(e => e.RoomId).Contains(destRoomId))
                        {
                            Clients.Caller.SendResponse(new MessageObject { StatusCode = 400, Message = "Chosen room does not exist or has not been assigned to chosen course." });
                            return;
                        }

                        var coordinatorsIds = includableCourseEdition.Coordinators.Select(e => e.CoordinatorId).ToArray();
                        var groupsIds = CourseEditionsController.GetNestedGroupsIds(includableCourseEdition, _groupRepo).ToArray();
                        var returnableGroupsIds = new int[groupsIds.Length];

                        Array.Sort(coordinatorsIds);
                        Array.Copy(groupsIds, returnableGroupsIds, groupsIds.Length);
                        Array.Sort(groupsIds);

                        lock (CoordinatorPositionLocks)
                        lock (GroupPositionLocks)
                        {
                            foreach (var timestampId in _destTimestamps)
                            {
                                foreach (var coordinatorId in coordinatorsIds)
                                {
                                    var key = new CoordinatorPositionKey
                                        { CoordinatorId = coordinatorId, TimestampId = timestampId };
                                    coordinatorPositionKeys.Add(key);
                                    var queue = CoordinatorPositionLocks.GetOrAdd(key, new ConcurrentQueue<object>());
                                    coordinatorPositionQueues.Add(queue);
                                    queue.Enqueue(new object());
                                }

                                foreach (var groupId in groupsIds)
                                {
                                    var key = new GroupPositionKey
                                        { GroupId = groupId, TimestampId = timestampId };
                                    groupPositionKeys.Add(key);
                                    var queue = GroupPositionLocks.GetOrAdd(key, new ConcurrentQueue<object>());
                                    groupPositionQueues.Add(queue);
                                    queue.Enqueue(new object());
                                }
                            }
                        }

                        foreach (var coordinatorPositionQueue in coordinatorPositionQueues)
                        {
                            Monitor.Enter(coordinatorPositionQueue);
                        }
                        foreach (var groupPositionQueue in groupPositionQueues)
                        {
                            Monitor.Enter(groupPositionQueue);
                        }
                        try
                        {
                            var _destSchedulePositions = _sourceTimestamps.SequenceEqual(_destTimestamps)
                                ?
                                _schedulePositionRepo
                                        .Get(e => _destTimestamps.Contains(e.TimestampId) && e.RoomId == destRoomId)
                                        .Include(e => e.CourseEdition)
                                        .ThenInclude(e => e.Coordinators)
                                        .Include(e => e.CourseEdition)
                                        .ThenInclude(e => e.Groups)
                                        .Select(e => new { e.TimestampId, e.RoomId })
                                :
                                _schedulePositionRepo
                                .Get(e => _destTimestamps.Contains(e.TimestampId)
                                          && (e.RoomId == destRoomId || e.CourseEdition.Coordinators
                                                                     .Select(e => e.CoordinatorId)
                                                                     .Any(e => coordinatorsIds.Contains(e))
                                                                 || e.CourseEdition.Groups.Select(e => e.GroupId)
                                                                     .Any(e => groupsIds.Contains(e))))
                                .Include(e => e.CourseEdition)
                                .ThenInclude(e => e.Coordinators)
                                .Include(e => e.CourseEdition)
                                .ThenInclude(e => e.Groups)
                                .Select(e => new { e.TimestampId, e.RoomId });

                            if (_destSchedulePositions.Any())
                            {
                                Clients.Caller.SendResponse(new MessageObject { StatusCode = 400, Message = "Some conflicts with other courses occurred." });
                                return;
                            }

                            var _courseRoomTimestamps = _courseRoomTimestampRepo
                                .Get(e => e.RoomId == destRoomId && _destTimestamps.Contains(e.TimestampId) &&
                                          e.CourseId == includableCourseEdition.CourseId)
                                .Select(e => e.TimestampId)
                                .OrderBy(e => e);

                            var destSchedulePositions = _destTimestamps.Select(timestampId => new SchedulePosition
                            {
                                RoomId = destRoomId,
                                TimestampId = timestampId,
                                CourseId = includableCourseEdition.CourseId,
                                CourseEditionId = includableCourseEdition.CourseEditionId,
                                CourseRoomTimestamp = !_courseRoomTimestamps.Contains(timestampId) ? new CourseRoomTimestamp
                                {
                                    RoomId = destRoomId,
                                    TimestampId = timestampId,
                                    CourseId = includableCourseEdition.CourseId
                                } : null
                            }).ToList();

                            var movesIds = _scheduledMoveRepo
                                .Get(e => e.RoomId_1 == roomId && _sourceTimestamps.Contains(e.TimestampId_1)
                                    && e.CourseId == includableCourseEdition.CourseId)
                                .GroupBy(e => e.MoveId)
                                .Select(e => e.Key)
                                .OrderBy(e => e).ToList();

                            _scheduledMoveRepo.DeleteMany(e => movesIds.Contains(e.MoveId));
                            _schedulePositionRepo.GetAll().RemoveRange(_sourceSchedulePositions);
                            _schedulePositionRepo.GetAll().AddRange(destSchedulePositions);

                            var result1a = _scheduledMoveRepo.SaveChanges().Result;
                            var result1b = _schedulePositionRepo.SaveChanges().Result;
                            
                            var result2b = Clients.Others.ModifiedSchedulePositions(
                                includableCourseEdition.CourseId, includableCourseEdition.CourseEditionId,
                                returnableGroupsIds, includableCourseEdition.Groups.Count, coordinatorsIds,
                                roomId, destRoomId,
                                periodIndex, destPeriodIndex,
                                day, destDay,
                                weeks, destWeeks,
                                movesIds.ToArray()
                            );

                            var result2a = Clients.Caller.SendResponse(new MessageObject { StatusCode = 200 });
                        }
                        finally
                        {
                            RemoveCoordinatorPositionsLocks(coordinatorPositionQueues, coordinatorPositionKeys);
                            RemoveGroupPositionsLocks(groupPositionQueues, groupPositionKeys);
                            foreach (var groupPositionQueue in groupPositionQueues)
                            {
                                Monitor.Exit(groupPositionQueue);
                            }
                            foreach (var coordinatorPositionQueue in coordinatorPositionQueues)
                            {
                                Monitor.Exit(coordinatorPositionQueue);
                            }
                        }
                    }
                    finally
                    {
                        RemoveSchedulePositionsLocksL2(L2schedulePositionAllQueues);
                        foreach (var schedulePositionQueue in L2schedulePositionAllQueues.Values)
                        {
                            Monitor.Exit(schedulePositionQueue);
                        }
                    }

                    foreach (var schedulePositionQueue in L1schedulePositionAllQueues)
                    {
                        if (schedulePositionKeys2.Contains(schedulePositionQueue.Key))
                        {
                            RemoveSchedulePositionLockL1(L1schedulePositionAllQueues, schedulePositionQueue.Key);
                            Monitor.Exit(schedulePositionQueue.Value);
                            L1KeysToRemove.Add(schedulePositionQueue.Key);
                        }
                    }

                    MakeScheduledMoves(schedulePositionKeys1, ref L1schedulePositionAllQueues, ref L1KeysToRemove);
                    return;
                }
                finally
                {
                    foreach (var keyToRemove in L1KeysToRemove)
                    {
                        L1schedulePositionAllQueues.Remove(keyToRemove);
                    }

                    RemoveSchedulePositionsLocksL1(L1schedulePositionAllQueues);
                    foreach (var schedulePositionQueue in L1schedulePositionAllQueues.Values)
                    {
                        Monitor.Exit(schedulePositionQueue);
                    }
                }
            }
            catch (Exception e)
            {
                Clients.Caller.SendResponse(new MessageObject { StatusCode = 400, Message = e.Message });
                return;
            }
        }

        [Authorize(Policy = "Coordinator")]
        public void RemoveSchedulePositions(int roomId, int periodIndex, int day, int[] weeks)
        {
            var schedulePositionKeys = new List<SchedulePositionKey>();
            var L1schedulePositionQueues = new SortedList<SchedulePositionKey, ConcurrentQueue<object>>();
            var L2schedulePositionQueues = new SortedList<SchedulePositionKey, ConcurrentQueue<object>>();

            weeks = weeks.Distinct().ToArray();
            Array.Sort(weeks);

            try
            {
                var userId = int.Parse(Context.User.Claims.FirstOrDefault(x => x.Type == "user_id")?.Value!);

                var _timestamps = _timestampRepo
                        .Get(e => e.PeriodIndex == periodIndex && e.Day == day && weeks.Contains(e.Week))
                        .Select(e => e.TimestampId)
                        .OrderBy(e => e).ToList();

                if (_timestamps.Count != weeks.Length)
                {
                    Clients.Caller.SendResponse(new MessageObject { StatusCode = 404, Message = "Could not find requested source time periods." });
                    return;
                }

                lock (SchedulePositionLocksL1)
                {
                    foreach (var timestampId in _timestamps)
                    {
                        var key = new SchedulePositionKey { RoomId = roomId, TimestampId = timestampId };
                        schedulePositionKeys.Add(key);
                        
                        var queueL1 = SchedulePositionLocksL1.GetOrAdd(key, new ConcurrentQueue<object>());

                        L1schedulePositionQueues.Add(key, queueL1);
                        queueL1.Enqueue(new object());
                    }
                }
                var L1KeysToRemove = new List<SchedulePositionKey>();

                foreach (var schedulePositionQueue in L1schedulePositionQueues.Values)
                {
                    Monitor.Enter(schedulePositionQueue);
                }
                try
                {
                    lock (SchedulePositionLocksL2)
                    {
                        foreach (var key in schedulePositionKeys)
                        {
                            var queue = SchedulePositionLocksL2.GetOrAdd(key, new ConcurrentQueue<object>());
                            queue.Enqueue(new object());

                            L2schedulePositionQueues.Add(key, queue);
                        }
                    }

                    foreach (var schedulePositionQueue in L2schedulePositionQueues.Values)
                    {
                        Monitor.Enter(schedulePositionQueue);
                    }
                    try
                    {
                        var _schedulePositions = _schedulePositionRepo
                        .Get(e => _timestamps.Contains(e.TimestampId) && e.RoomId == roomId &&
                                  e.CourseEdition.Coordinators.Any(e => e.CoordinatorId == userId))
                        .Include(e => e.CourseEdition)
                            .ThenInclude(e => e.Coordinators);

                        if (_schedulePositions.Count() != _timestamps.Count)
                        {
                            Clients.Caller.SendResponse(new MessageObject { StatusCode = 404, Message = "Could not find requested positions in schedule." });
                            return;
                        }

                        if (Enumerable.Any(_schedulePositions, schedulePosition => schedulePosition.LockUserId != userId || schedulePosition.LockUserConnectionId != Context.ConnectionId))
                        {
                            Clients.Caller.SendResponse(new MessageObject { StatusCode = 400, Message = "You didn't lock some positions in schedule." });
                            return;
                        }

                        var schedulePosition = _schedulePositions.FirstOrDefault();

                        var _courseEdition = _courseEditionRepo
                            .Get(e => e.CourseId == schedulePosition.CourseId &&
                                      e.CourseEditionId == schedulePosition.CourseEditionId)
                            .Include(e => e.Groups)
                            .ThenInclude(e => e.Group);

                        var courseEdition = _courseEdition.FirstOrDefault();
                        if (courseEdition == null)
                        {
                            Clients.Caller.SendResponse(new MessageObject { StatusCode = 400, Message = "Could not find course edition for requested positions in schedule." });
                            return;
                        }

                        var coordinatorsIds = schedulePosition.CourseEdition.Coordinators.Select(e => e.CoordinatorId).ToArray();
                        var groupsIds = CourseEditionsController.GetNestedGroupsIds(courseEdition, _groupRepo).ToArray();
                        var returnableGroupsIds = new int[groupsIds.Length];

                        Array.Sort(coordinatorsIds);
                        Array.Copy(groupsIds, returnableGroupsIds, groupsIds.Length);
                        Array.Sort(groupsIds);

                        var movesIds = _scheduledMoveRepo
                                .Get(e => e.RoomId_1 == roomId && _timestamps.Contains(e.TimestampId_1)
                                    && e.CourseId == schedulePosition.CourseId)
                                .GroupBy(e => e.MoveId)
                                .Select(e => e.Key)
                                .OrderBy(e => e).ToList();

                        _scheduledMoveRepo.DeleteMany(e => movesIds.Contains(e.MoveId));
                        _schedulePositionRepo.GetAll().RemoveRange(_schedulePositions);
                        
                        var result1a = _schedulePositionRepo.SaveChanges().Result;
                        var resull1b = _scheduledMoveRepo.SaveChanges().Result;

                        var result2b = Clients.Others.RemovedSchedulePositions(
                            courseEdition.CourseId, courseEdition.CourseEditionId,
                            returnableGroupsIds, courseEdition.Groups.Count, coordinatorsIds,
                            roomId, periodIndex,
                            day, weeks,
                            movesIds.ToArray()
                        );

                        Clients.Caller.SendResponse(new MessageObject { StatusCode = 204 });
                    }
                    finally
                    {
                        RemoveSchedulePositionsLocksL2(L2schedulePositionQueues);
                        foreach (var schedulePositionQueue in L2schedulePositionQueues.Values)
                        {
                            Monitor.Enter(schedulePositionQueue);
                        }
                    }

                    MakeScheduledMoves(schedulePositionKeys, ref L1schedulePositionQueues, ref L1KeysToRemove);
                    return;
                }
                finally
                {
                    foreach (var keyToRemove in L1KeysToRemove)
                    {
                        L1schedulePositionQueues.Remove(keyToRemove);
                    }

                    RemoveSchedulePositionsLocksL1(L1schedulePositionQueues);
                    foreach (var schedulePositionQueue in L1schedulePositionQueues.Values)
                    {
                        Monitor.Exit(schedulePositionQueue);
                    }
                }
            }
            catch (Exception e)
            {
                Clients.Caller.SendResponse(new MessageObject { StatusCode = 400, Message = e.Message });
                return;
            }
        }

        
        [Authorize(Policy = "Coordinator")]
        public MessageObject AddScheduledMove(int roomId, int periodIndex, int day, int[] weeks, int destRoomId, int destPeriodIndex, int destDay, int[] destWeeks)
        {
            var schedulePositionKeys1 = new List<SchedulePositionKey>();
            var schedulePositionKeys2 = new List<SchedulePositionKey>();
            var coordinatorPositionKeys = new List<CoordinatorPositionKey>();
            var groupPositionKeys = new List<GroupPositionKey>();
            var L1schedulePositionAllQueues = new SortedList<SchedulePositionKey, ConcurrentQueue<object>>();
            var L2schedulePositionAllQueues = new SortedList<SchedulePositionKey, ConcurrentQueue<object>>();
            var coordinatorPositionQueues = new List<ConcurrentQueue<object>>();
            var groupPositionQueues = new List<ConcurrentQueue<object>>();

            weeks = weeks.Distinct().ToArray();
            Array.Sort(weeks);
            destWeeks = destWeeks.Distinct().ToArray();
            Array.Sort(destWeeks);

            if (weeks.Length != destWeeks.Length)
            {
                return new MessageObject { StatusCode = 400, Message = "Amount of weeks must be equal." };
            }

            try
            {
                var userId = int.Parse(Context.User.Claims.FirstOrDefault(x => x.Type == "user_id")?.Value!);

                var _sourceTimestamps = _timestampRepo
                        .Get(e => e.PeriodIndex == periodIndex && e.Day == day && weeks.Contains(e.Week))
                        .Select(e => e.TimestampId)
                        .OrderBy(e => e).ToList();

                if (_sourceTimestamps.Count != weeks.Length)
                {
                    return new MessageObject { StatusCode = 404, Message = "Could not find requested source time periods." };
                }

                var _destTimestamps = _timestampRepo
                            .Get(e => e.PeriodIndex == destPeriodIndex && e.Day == destDay && destWeeks.Contains(e.Week))
                            .Select(e => e.TimestampId)
                            .OrderBy(e => e).ToList();

                if (_destTimestamps.Count != destWeeks.Length)
                {
                    return new MessageObject { StatusCode = 404, Message = "Could not find requested destination time periods." };
                }

                lock (SchedulePositionLocksL1)
                {
                    foreach (var timestampId in _sourceTimestamps)
                    {
                        var key = new SchedulePositionKey { RoomId = roomId, TimestampId = timestampId };
                        schedulePositionKeys1.Add(key);
                        var queue = SchedulePositionLocksL1.GetOrAdd(key, new ConcurrentQueue<object>());
                        
                        L1schedulePositionAllQueues.Add(key, queue);
                        queue.Enqueue(new object());
                    }

                    foreach (var timestampId in _destTimestamps)
                    {
                        var key = new SchedulePositionKey { RoomId = destRoomId, TimestampId = timestampId };
                        schedulePositionKeys2.Add(key);
                        var queue = SchedulePositionLocksL1.GetOrAdd(key, new ConcurrentQueue<object>());

                        L1schedulePositionAllQueues.Add(key, queue);
                        queue.Enqueue(new object());
                    }
                }

                foreach (var schedulePositionQueue in L1schedulePositionAllQueues.Values)
                {
                    Monitor.Enter(schedulePositionQueue);
                }

                try
                {
                    lock (SchedulePositionLocksL2)
                    {
                        foreach (var key in schedulePositionKeys1)
                        {
                            var queue = SchedulePositionLocksL2.GetOrAdd(key, new ConcurrentQueue<object>());
                            queue.Enqueue(new object());

                            L2schedulePositionAllQueues.Add(key, queue);
                        }

                        foreach (var key in schedulePositionKeys2)
                        {
                            var queue = SchedulePositionLocksL2.GetOrAdd(key, new ConcurrentQueue<object>());
                            queue.Enqueue(new object());

                            L2schedulePositionAllQueues.Add(key, queue);
                        }
                    }

                    foreach (var schedulePositionQueue in L2schedulePositionAllQueues.Values)
                    {
                        Monitor.Enter(schedulePositionQueue);
                    }
                    try
                    {
                        var _sourceSchedulePositions = _schedulePositionRepo
                        .Get(e => _sourceTimestamps.Contains(e.TimestampId) && e.RoomId == roomId &&
                                  e.CourseEdition.Coordinators.Any(e => e.CoordinatorId == userId))
                        .Include(e => e.CourseEdition)
                            .ThenInclude(e => e.Coordinators);

                        if (_sourceSchedulePositions.Count() != _sourceTimestamps.Count)
                        {
                            return new MessageObject { StatusCode = 404, Message = "Could not find requested positions in schedule." };
                        }

                        if (Enumerable.Any(_sourceSchedulePositions, schedulePosition => schedulePosition.LockUserId != userId || schedulePosition.LockUserConnectionId != Context.ConnectionId))
                        {
                            return new MessageObject { StatusCode = 400, Message = "You didn't lock some positions in schedule." };
                        }

                        var courseEdition = _sourceSchedulePositions.FirstOrDefault()?.CourseEdition;

                        if (courseEdition == null)
                        {
                            return new MessageObject { StatusCode = 400, Message = "Could not find course edition for requested positions in schedule." };
                        }

                        var _courseEdition = _courseEditionRepo
                            .Get(e => e.CourseId == courseEdition.CourseId &&
                                      e.CourseEditionId == courseEdition.CourseEditionId)
                            .Include(e => e.Course)
                                .ThenInclude(e => e.Rooms)
                            .Include(e => e.Coordinators)
                            .Include(e => e.Groups)
                                .ThenInclude(e => e.Group);

                        var includableCourseEdition = _courseEdition.FirstOrDefault();
                        if (includableCourseEdition == null || !includableCourseEdition.Course.Rooms.Select(e => e.RoomId).Contains(destRoomId))
                        {
                            return new MessageObject { StatusCode = 400, Message = "Chosen room does not exist or has not been assigned to chosen course." };
                        }

                        var coordinatorsIds = includableCourseEdition.Coordinators.Select(e => e.CoordinatorId).ToArray();
                        var groupsIds = CourseEditionsController.GetNestedGroupsIds(includableCourseEdition, _groupRepo).ToArray();
                        var returnableGroupsIds = new int[groupsIds.Length];

                        Array.Sort(coordinatorsIds);
                        Array.Copy(groupsIds, returnableGroupsIds, groupsIds.Length);
                        Array.Sort(groupsIds);

                        lock (CoordinatorPositionLocks)
                        lock (GroupPositionLocks)
                        {
                            foreach (var timestampId in _destTimestamps)
                            {
                                foreach (var coordinatorId in coordinatorsIds)
                                {
                                    var key = new CoordinatorPositionKey
                                    { CoordinatorId = coordinatorId, TimestampId = timestampId };
                                    coordinatorPositionKeys.Add(key);
                                    var queue = CoordinatorPositionLocks.GetOrAdd(key, new ConcurrentQueue<object>());
                                    coordinatorPositionQueues.Add(queue);
                                    queue.Enqueue(new object());
                                }

                                foreach (var groupId in groupsIds)
                                {
                                    var key = new GroupPositionKey
                                    { GroupId = groupId, TimestampId = timestampId };
                                    groupPositionKeys.Add(key);
                                    var queue = GroupPositionLocks.GetOrAdd(key, new ConcurrentQueue<object>());
                                    groupPositionQueues.Add(queue);
                                    queue.Enqueue(new object());
                                }
                            }
                        }

                        foreach (var coordinatorPositionQueue in coordinatorPositionQueues)
                        {
                            Monitor.Enter(coordinatorPositionQueue);
                        }
                        foreach (var groupPositionQueue in groupPositionQueues)
                        {
                            Monitor.Enter(groupPositionQueue);
                        }

                        try
                        {
                            var _destSchedulePositions = _sourceTimestamps.SequenceEqual(_destTimestamps)
                                ?
                                _schedulePositionRepo
                                        .Get(e => _destTimestamps.Contains(e.TimestampId) && e.RoomId == destRoomId)
                                        .Include(e => e.CourseEdition)
                                        .ThenInclude(e => e.Coordinators)
                                        .Include(e => e.CourseEdition)
                                        .ThenInclude(e => e.Groups)
                                :
                                _schedulePositionRepo
                                .Get(e => _destTimestamps.Contains(e.TimestampId)
                                          && (e.RoomId == destRoomId || e.CourseEdition.Coordinators
                                                                     .Select(e => e.CoordinatorId)
                                                                     .Any(e => coordinatorsIds.Contains(e))
                                                                 || e.CourseEdition.Groups.Select(e => e.GroupId)
                                                                     .Any(e => groupsIds.Contains(e))))
                                .Include(e => e.CourseEdition)
                                .ThenInclude(e => e.Coordinators)
                                .Include(e => e.CourseEdition)
                                .ThenInclude(e => e.Groups);

                            if (!_destSchedulePositions.Any())
                            {
                                return new MessageObject { StatusCode = 400, Message = "This move is possible, so you should not try to schedule it." };
                            }

                            /*if (_destSchedulePositions.Any(e => e.CourseEdition.Coordinators.Select(e => e.CoordinatorId).Contains(userId)))
                            {
                                return new MessageObject { StatusCode = 400, Message = "Scheduled move cannot collide with your own courses." };
                            }*/

                            var _scheduledMovesCountsCondition = _scheduledMoveRepo
                                .Get(e => e.RoomId_1 == roomId && _sourceTimestamps.Contains(e.TimestampId_1)
                                    && e.RoomId_2 == destRoomId && _destTimestamps.Contains(e.TimestampId_2)
                                    && e.CourseId == courseEdition.CourseId)
                                .GroupBy(e => e.MoveId)
                                .Select(e => new { MoveId = e.Key, Count = e.Count() })
                                .OrderBy(e => e.MoveId).ToList();

                            var _sourceTimestampsCount = _sourceTimestamps.Count();
                            _scheduledMovesCountsCondition = _scheduledMovesCountsCondition
                                .Where(e => e.Count == _sourceTimestampsCount).ToList();

                            var _scheduledMovesCounts = _scheduledMoveRepo
                                .Get(e => _scheduledMovesCountsCondition.Select(e => e.MoveId).Contains(e.MoveId))
                                .GroupBy(e => e.MoveId)
                                .Select(e => new { MoveId = e.Key, Count = e.Count() })
                                .OrderBy(e => e.MoveId).ToList();

                            var _scheduledMovesCount = _scheduledMovesCountsCondition.Count();
                            for (var i = 0; i < _scheduledMovesCount; ++i)
                            {
                                if (_scheduledMovesCountsCondition[i].Count == _scheduledMovesCounts[i].Count)
                                {
                                    return new MessageObject { StatusCode = 400, Message = "This move is already scheduled." };
                                }
                            }

                            var _courseRoomTimestamps = _courseRoomTimestampRepo
                                .Get(e => e.RoomId == destRoomId && _destTimestamps.Contains(e.TimestampId) &&
                                          e.CourseId == courseEdition.CourseId)
                                .Select(e => e.TimestampId)
                                .OrderBy(e => e);

                            var scheduledMoveId = _scheduledMoveRepo.GetNextId();
                            var scheduleOrderDate = DateTime.Now;

                            var scheduledMove = new List<ScheduledMove>();
                            for (var i = 0; i < _destTimestamps.Count; ++i)
                            {
                                var srcTimestamp = _sourceTimestamps[i];
                                var destTimestamp = _destTimestamps[i];
                                scheduledMove.Add(new ScheduledMove
                                {
                                    MoveId = scheduledMoveId,
                                    RoomId_1 = roomId,
                                    TimestampId_1 = srcTimestamp,
                                    RoomId_2 = destRoomId,
                                    TimestampId_2 = destTimestamp,
                                    CourseId = courseEdition.CourseId,
                                    UserId = userId,
                                    IsConfirmed = true,
                                    ScheduleOrder = scheduleOrderDate,
                                    Destination = !_courseRoomTimestamps.Contains(destTimestamp) ? new CourseRoomTimestamp
                                    {
                                        RoomId = destRoomId,
                                        TimestampId = destTimestamp,
                                        CourseId = courseEdition.CourseId
                                    } : null
                                });
                            }

                            _scheduledMoveRepo.GetAll().AddRange(scheduledMove);

                            var result1 = _scheduledMoveRepo.SaveChanges().Result;
                            var result2 = Clients.All.AddedScheduledMove(
                                scheduledMoveId, true,
                                courseEdition.CourseId, courseEdition.CourseEditionId,
                                roomId, periodIndex,
                                day, weeks
                            );

                            return new MessageObject { StatusCode = 200 };
                        }
                        finally
                        {
                            RemoveGroupPositionsLocks(groupPositionQueues, groupPositionKeys);
                            RemoveCoordinatorPositionsLocks(coordinatorPositionQueues, coordinatorPositionKeys);
                            foreach (var groupPositionQueue in groupPositionQueues)
                            {
                                Monitor.Exit(groupPositionQueue);
                            }
                            foreach (var coordinatorPositionQueue in coordinatorPositionQueues)
                            {
                                Monitor.Exit(coordinatorPositionQueue);
                            }
                        }
                    }
                    finally
                    {
                        RemoveSchedulePositionsLocksL2(L2schedulePositionAllQueues);
                        foreach (var schedulePositionQueue in L2schedulePositionAllQueues.Values)
                        {
                            Monitor.Exit(schedulePositionQueue);
                        }
                    }
                }
                finally 
                {
                    RemoveSchedulePositionsLocksL1(L1schedulePositionAllQueues);
                    foreach (var schedulePositionQueue in L1schedulePositionAllQueues.Values)
                    {
                        Monitor.Exit(schedulePositionQueue);
                    }
                }
            }
            catch (Exception e)
            {
                return new MessageObject { StatusCode = 400, Message = e.Message };
            }
        }

        [Authorize(Policy = "Coordinator")]
        public MessageObject RemoveScheduledMove(int roomId, int periodIndex, int day, int[] weeks, int destRoomId, int destPeriodIndex, int destDay, int[] destWeeks)
        {
            var schedulePositionKeys1 = new List<SchedulePositionKey>();
            var schedulePositionKeys2 = new List<SchedulePositionKey>();
            var coordinatorPositionKeys = new List<CoordinatorPositionKey>();
            var groupPositionKeys = new List<GroupPositionKey>();
            var L1schedulePositionAllQueues = new SortedList<SchedulePositionKey, ConcurrentQueue<object>>();
            var L2schedulePositionAllQueues = new SortedList<SchedulePositionKey, ConcurrentQueue<object>>();
            var coordinatorPositionQueues = new List<ConcurrentQueue<object>>();
            var groupPositionQueues = new List<ConcurrentQueue<object>>();

            weeks = weeks.Distinct().ToArray();
            Array.Sort(weeks);
            destWeeks = destWeeks.Distinct().ToArray();
            Array.Sort(destWeeks);

            if (weeks.Length != destWeeks.Length)
            {
                return new MessageObject { StatusCode = 400, Message = "Amount of weeks must be equal." };
            }

            try
            {
                var userId = int.Parse(Context.User.Claims.FirstOrDefault(x => x.Type == "user_id")?.Value!);

                var _sourceTimestamps = _timestampRepo
                        .Get(e => e.PeriodIndex == periodIndex && e.Day == day && weeks.Contains(e.Week))
                        .Select(e => e.TimestampId)
                        .OrderBy(e => e).ToList();

                if (_sourceTimestamps.Count != weeks.Length)
                {
                    return new MessageObject { StatusCode = 404, Message = "Could not find requested source time periods." };
                }

                var _destTimestamps = _timestampRepo
                            .Get(e => e.PeriodIndex == destPeriodIndex && e.Day == destDay && destWeeks.Contains(e.Week))
                            .Select(e => e.TimestampId)
                            .OrderBy(e => e).ToList();

                if (_destTimestamps.Count != destWeeks.Length)
                {
                    return new MessageObject { StatusCode = 404, Message = "Could not find requested destination time periods." };
                }

                lock (SchedulePositionLocksL1)
                {
                    foreach (var timestampId in _sourceTimestamps)
                    {
                        var key = new SchedulePositionKey { RoomId = roomId, TimestampId = timestampId };
                        schedulePositionKeys1.Add(key);
                        var queue = SchedulePositionLocksL1.GetOrAdd(key, new ConcurrentQueue<object>());
                        queue.Enqueue(new object());

                        L1schedulePositionAllQueues.Add(key, queue);
                    }

                    foreach (var timestampId in _destTimestamps)
                    {
                        var key = new SchedulePositionKey { RoomId = destRoomId, TimestampId = timestampId };
                        schedulePositionKeys2.Add(key);
                        var queue = SchedulePositionLocksL1.GetOrAdd(key, new ConcurrentQueue<object>());
                        queue.Enqueue(new object());

                        L1schedulePositionAllQueues.Add(key, queue);
                    }
                }

                foreach (var schedulePositionQueue in L1schedulePositionAllQueues.Values)
                {
                    Monitor.Enter(schedulePositionQueue);
                }

                try
                {
                    lock (SchedulePositionLocksL2)
                    {
                        foreach (var key in schedulePositionKeys1)
                        {
                            var queue = SchedulePositionLocksL2.GetOrAdd(key, new ConcurrentQueue<object>());
                            queue.Enqueue(new object());

                            L2schedulePositionAllQueues.Add(key, queue);
                        }

                        foreach (var key in schedulePositionKeys2)
                        {
                            var queue = SchedulePositionLocksL2.GetOrAdd(key, new ConcurrentQueue<object>());
                            queue.Enqueue(new object());

                            L2schedulePositionAllQueues.Add(key, queue);
                        }
                    }

                    foreach (var schedulePositionQueue in L2schedulePositionAllQueues.Values)
                    {
                        Monitor.Enter(schedulePositionQueue);
                    }
                    try
                    {
                        var _sourceSchedulePositions = _schedulePositionRepo
                        .Get(e => _sourceTimestamps.Contains(e.TimestampId) && e.RoomId == roomId &&
                                  e.CourseEdition.Coordinators.Any(e => e.CoordinatorId == userId))
                        .Include(e => e.CourseEdition)
                            .ThenInclude(e => e.Coordinators);

                        if (_sourceSchedulePositions.Count() != _sourceTimestamps.Count)
                        {
                            return new MessageObject { StatusCode = 404, Message = "Could not find requested positions in schedule." };
                        }

                        if (Enumerable.Any(_sourceSchedulePositions, schedulePosition => schedulePosition.LockUserId != userId || schedulePosition.LockUserConnectionId != Context.ConnectionId))
                        {
                            return new MessageObject { StatusCode = 400, Message = "You didn't lock some positions in schedule." };
                        }

                        var courseEdition = _sourceSchedulePositions.FirstOrDefault()?.CourseEdition;

                        if (courseEdition == null)
                        {
                            return new MessageObject { StatusCode = 400, Message = "Could not find course edition for requested positions in schedule." };
                        }

                        var _courseEdition = _courseEditionRepo
                            .Get(e => e.CourseId == courseEdition.CourseId &&
                                      e.CourseEditionId == courseEdition.CourseEditionId)
                            .Include(e => e.Course)
                                .ThenInclude(e => e.Rooms)
                            .Include(e => e.Coordinators)
                            .Include(e => e.Groups)
                                .ThenInclude(e => e.Group);

                        var includableCourseEdition = _courseEdition.FirstOrDefault();
                        if (includableCourseEdition == null || !includableCourseEdition.Course.Rooms.Select(e => e.RoomId).Contains(destRoomId))
                        {
                            return new MessageObject { StatusCode = 400, Message = "Chosen room does not exist or has not been assigned to chosen course." };
                        }

                        var coordinatorsIds = includableCourseEdition.Coordinators.Select(e => e.CoordinatorId).ToArray();
                        var groupsIds = CourseEditionsController.GetNestedGroupsIds(includableCourseEdition, _groupRepo).ToArray();
                        var returnableGroupsIds = new int[groupsIds.Length];

                        Array.Sort(coordinatorsIds);
                        Array.Copy(groupsIds, returnableGroupsIds, groupsIds.Length);
                        Array.Sort(groupsIds);

                        lock (CoordinatorPositionLocks)
                        lock (GroupPositionLocks)
                        {
                            foreach (var timestampId in _destTimestamps)
                            {
                                foreach (var coordinatorId in coordinatorsIds)
                                {
                                    var key = new CoordinatorPositionKey
                                        { CoordinatorId = coordinatorId, TimestampId = timestampId };
                                    coordinatorPositionKeys.Add(key);
                                    var queue = CoordinatorPositionLocks.GetOrAdd(key, new ConcurrentQueue<object>());
                                    coordinatorPositionQueues.Add(queue);
                                    queue.Enqueue(new object());
                                }

                                foreach (var groupId in groupsIds)
                                {
                                    var key = new GroupPositionKey
                                        { GroupId = groupId, TimestampId = timestampId };
                                    groupPositionKeys.Add(key);
                                    var queue = GroupPositionLocks.GetOrAdd(key, new ConcurrentQueue<object>());
                                    groupPositionQueues.Add(queue);
                                    queue.Enqueue(new object());
                                }
                            }
                        }

                        foreach (var coordinatorPositionQueue in coordinatorPositionQueues)
                        {
                            Monitor.Enter(coordinatorPositionQueue);
                        }
                        foreach (var groupPositionQueue in groupPositionQueues)
                        {
                            Monitor.Enter(groupPositionQueue);
                        }
                        try
                        {
                            var _scheduledMovesCountsCondition = _scheduledMoveRepo
                                .Get(e => e.RoomId_1 == roomId && _sourceTimestamps.Contains(e.TimestampId_1)
                                    && e.RoomId_2 == destRoomId && _destTimestamps.Contains(e.TimestampId_2)
                                    && e.CourseId == courseEdition.CourseId)
                                .GroupBy(e => e.MoveId)
                                .Select(e => new { MoveId = e.Key, Count = e.Count() })
                                .OrderBy(e => e.MoveId).ToList();

                            var _sourceTimestampsCount = _sourceTimestamps.Count();
                            _scheduledMovesCountsCondition = _scheduledMovesCountsCondition
                                .Where(e => e.Count == _sourceTimestampsCount).ToList();

                            var _scheduledMovesCounts = _scheduledMoveRepo
                                .Get(e => _scheduledMovesCountsCondition.Select(e => e.MoveId).Contains(e.MoveId))
                                .GroupBy(e => e.MoveId)
                                .Select(e => new { MoveId = e.Key, Count = e.Count() })
                                .OrderBy(e => e.MoveId).ToList();

                            var _scheduledMovesCount = _scheduledMovesCountsCondition.Count();
                            int moveId = 0;
                            var isFound = false;
                            for (var i = 0; i < _scheduledMovesCount; ++i)
                            {
                                if (_scheduledMovesCountsCondition[i].Count == _scheduledMovesCounts[i].Count)
                                {
                                    _scheduledMoveRepo
                                        .DeleteMany(e => e.MoveId == _scheduledMovesCountsCondition[i].MoveId);

                                    moveId = _scheduledMovesCountsCondition[i].MoveId;
                                    isFound = true;
                                    break;
                                }
                            }

                            if (!isFound)
                            {
                                return new MessageObject { StatusCode = 400, Message = "Could not find scheduled move." };
                            }

                            var result1 = _scheduledMoveRepo.SaveChanges().Result;
                            var result2 = Clients.All.RemovedScheduledMove(
                                moveId,
                                courseEdition.CourseId, courseEdition.CourseEditionId,
                                roomId, periodIndex,
                                day, weeks
                            );

                            return new MessageObject { StatusCode = 200 };
                        }
                        finally
                        {
                            RemoveGroupPositionsLocks(groupPositionQueues, groupPositionKeys);
                            RemoveCoordinatorPositionsLocks(coordinatorPositionQueues, coordinatorPositionKeys);
                            foreach (var groupPositionQueue in groupPositionQueues)
                            {
                                Monitor.Exit(groupPositionQueue);
                            }
                            foreach (var coordinatorPositionQueue in coordinatorPositionQueues)
                            {
                                Monitor.Exit(coordinatorPositionQueue);
                            }
                        }
                    }
                    finally
                    {
                        RemoveSchedulePositionsLocksL2(L2schedulePositionAllQueues);
                        foreach (var schedulePositionQueue in L2schedulePositionAllQueues.Values)
                        {
                            Monitor.Exit(schedulePositionQueue);
                        }
                    }
                }
                finally
                {
                    RemoveSchedulePositionsLocksL1(L1schedulePositionAllQueues);
                    foreach (var schedulePositionQueue in L1schedulePositionAllQueues.Values)
                    {
                        Monitor.Exit(schedulePositionQueue);
                    }
                }
            }
            catch (Exception e)
            {
                return new MessageObject { StatusCode = 400, Message = e.Message };
            }
        }

        private void RemoveAllClientLocks(int userId, string connectionId)
        {
            var _courseEditions = _courseEditionRepo
                .Get(e => e.LockUserId == userId && e.LockUserConnectionId == connectionId);

            var _schedulePositions = _schedulePositionRepo
                .Get(e => e.LockUserId == userId && e.LockUserConnectionId == connectionId)
                .Include(e => e.CourseRoomTimestamp)
                    .ThenInclude(e => e.Timestamp);

            var courseEditions = _courseEditions.Any() ? _courseEditions.ToList() : new List<CourseEdition>();
            var schedulePositions = _schedulePositions.Any() ? _schedulePositions.ToList() : new List<SchedulePosition>();

            foreach (var courseEdition in courseEditions)
            {
                UnlockCourseEdition(courseEdition.CourseId, courseEdition.CourseEditionId);
            }

            foreach (var schedulePosition in schedulePositions)
            {
                var timestamp = schedulePosition.CourseRoomTimestamp.Timestamp;
                UnlockSchedulePositions(schedulePosition.RoomId, timestamp.PeriodIndex, timestamp.Day, new int[]{timestamp.Week});
            }
        }

        public override Task OnConnectedAsync()
        {
            var id = int.Parse(Context.User.Claims.FirstOrDefault(claim => claim.Type == "user_id")?.Value!);

            //Context.User.Claims.ToList().ForEach(Console.WriteLine);
            Console.WriteLine($"\tConnected {Context.ConnectionId}");
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            var id = int.Parse(Context.User.Claims.FirstOrDefault(claim => claim.Type == "user_id")?.Value!);

            Console.WriteLine($"Disconnected {Context.ConnectionId}");
            RemoveAllClientLocks(id, Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }
    }
}
