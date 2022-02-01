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
using ScheduleDesigner.Hubs.Helpers;
using ScheduleDesigner.Models;
using ScheduleDesigner.Helpers;
using LinqKit;
using System.Security.Claims;
using ScheduleDesigner.Repositories.UnitOfWork;

namespace ScheduleDesigner.Hubs
{
    [Authorize]
    public class ScheduleHub : Hub<IScheduleClient>
    {
        private readonly IUnitOfWork _unitOfWork;

        public static readonly ConcurrentDictionary<CourseEditionKey, ConcurrentQueue<object>>
            CourseEditionLocks = new ConcurrentDictionary<CourseEditionKey, ConcurrentQueue<object>>();

        public static readonly ConcurrentDictionary<SchedulePositionKey, ConcurrentQueue<object>>
            SchedulePositionLocksL1 = new ConcurrentDictionary<SchedulePositionKey, ConcurrentQueue<object>>();

        public static readonly ConcurrentDictionary<SchedulePositionKey, ConcurrentQueue<object>>
            SchedulePositionLocksL2 = new ConcurrentDictionary<SchedulePositionKey, ConcurrentQueue<object>>();

        private static readonly ConcurrentDictionary<CoordinatorPositionKey, ConcurrentQueue<object>>
            CoordinatorPositionLocks = new ConcurrentDictionary<CoordinatorPositionKey, ConcurrentQueue<object>>();

        private static readonly ConcurrentDictionary<GroupPositionKey, ConcurrentQueue<object>>
            GroupPositionLocks = new ConcurrentDictionary<GroupPositionKey, ConcurrentQueue<object>>();

        public ScheduleHub(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public static void RemoveCourseEditionLock(
            ConcurrentQueue<object> courseEditionQueue,
            CourseEditionKey courseEditionKey)
        {
            courseEditionQueue.TryDequeue(out _);
            if (courseEditionQueue.IsEmpty)
            {
                CourseEditionLocks.TryRemove(courseEditionKey, out _);
            }
        }

        public static void RemoveCourseEditionsLocks(
            SortedList<CourseEditionKey, ConcurrentQueue<object>> courseEditions)
        {
            foreach (var courseEdition in courseEditions)
            {
                courseEdition.Value.TryDequeue(out _);
                if (courseEdition.Value.IsEmpty)
                {
                    CourseEditionLocks.TryRemove(courseEdition.Key, out _);
                }
            }
        }

        public static void RemoveSchedulePositionLockL1(
            SortedList<SchedulePositionKey, ConcurrentQueue<object>> schedulePositions,
            SchedulePositionKey key)
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

        public static void RemoveSchedulePositionsLocksL1(
            List<ConcurrentQueue<object>> schedulePositionQueues,
            List<SchedulePositionKey> schedulePositionKeys)
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

        public static void RemoveSchedulePositionsLocksL1(
            SortedList<SchedulePositionKey, ConcurrentQueue<object>> schedulePositions)
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

        public static void RemoveSchedulePositionLockL2(
            SortedList<SchedulePositionKey, ConcurrentQueue<object>> schedulePositions,
            SchedulePositionKey key)
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

        public static void RemoveSchedulePositionsLocksL2(
            List<ConcurrentQueue<object>> schedulePositionQueues,
            List<SchedulePositionKey> schedulePositionKeys)
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

        public static void RemoveSchedulePositionsLocksL2(
            SortedList<SchedulePositionKey, ConcurrentQueue<object>> schedulePositions)
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

        public static void RemoveCoordinatorPositionsLocks(
            SortedList<CoordinatorPositionKey, ConcurrentQueue<object>> coordinatorPositions)
        {
            foreach (var coordinatorPosition in coordinatorPositions)
            {
                coordinatorPosition.Value.TryDequeue(out _);
                if (coordinatorPosition.Value.IsEmpty)
                {
                    CoordinatorPositionLocks.TryRemove(coordinatorPosition.Key, out _);
                }
            }
        }

        public static void RemoveGroupPositionsLocks(
            SortedList<GroupPositionKey, ConcurrentQueue<object>> groupPositions)
        {
            foreach (var groupPosition in groupPositions)
            {
                groupPosition.Value.TryDequeue(out _);
                if (groupPosition.Value.IsEmpty)
                {
                    GroupPositionLocks.TryRemove(groupPosition.Key, out _);
                }
            }
        }

        public static void AddCourseEditionsLocks(
            List<CourseEditionKey> courseEditionKeys,
            SortedList<CourseEditionKey, ConcurrentQueue<object>> courseEditionQueues)
        {
            foreach (var key in courseEditionKeys)
            {
                var queue = CourseEditionLocks.GetOrAdd(key, new ConcurrentQueue<object>());
                courseEditionQueues.Add(key, queue);
                queue.Enqueue(new object());
            }
        }

        public static void AddSchedulePositionsLocksL1(
            List<int> _timestamps, int roomId,
            List<SchedulePositionKey> schedulePositionKeys,
            SortedList<SchedulePositionKey, ConcurrentQueue<object>> schedulePositions)
        {
            foreach (var timestampId in _timestamps)
            {
                var key = new SchedulePositionKey { RoomId = roomId, TimestampId = timestampId };
                schedulePositionKeys.Add(key);
                var queue = SchedulePositionLocksL1.GetOrAdd(key, new ConcurrentQueue<object>());
                schedulePositions.Add(key, queue);
                queue.Enqueue(new object());
            }
        }

        public static void AddSchedulePositionsLocksL1(
            List<int> _timestamps, int roomId,
            SortedList<SchedulePositionKey, ConcurrentQueue<object>> schedulePositions)
        {
            foreach (var timestampId in _timestamps)
            {
                var key = new SchedulePositionKey { RoomId = roomId, TimestampId = timestampId };
                var queue = SchedulePositionLocksL1.GetOrAdd(key, new ConcurrentQueue<object>());
                schedulePositions.Add(key, queue);
                queue.Enqueue(new object());
            }
        }

        public static void AddSchedulePositionsLocksL1(
            List<SchedulePositionKey> schedulePositionKeys,
            SortedList<SchedulePositionKey, ConcurrentQueue<object>> schedulePositions)
        {
            foreach (var key in schedulePositionKeys)
            {
                var queue = SchedulePositionLocksL1.GetOrAdd(key, new ConcurrentQueue<object>());
                schedulePositions.Add(key, queue);
                queue.Enqueue(new object());
            }
        }

        public static void AddSchedulePositionsLocksL2(
            List<SchedulePositionKey> schedulePositionKeys,
            SortedList<SchedulePositionKey, ConcurrentQueue<object>> schedulePositions)
        {
            foreach (var key in schedulePositionKeys)
            {
                var queue = SchedulePositionLocksL2.GetOrAdd(key, new ConcurrentQueue<object>());
                schedulePositions.Add(key, queue);
                queue.Enqueue(new object());
            }
        }

        private void AddCoordinatorPositionsLocks(
            int[] coordinatorsIds, int timestampId,
            SortedList<CoordinatorPositionKey, ConcurrentQueue<object>> coordinatorPositionQueues)
        {
            foreach (var coordinatorId in coordinatorsIds)
            {
                var key = new CoordinatorPositionKey { CoordinatorId = coordinatorId, TimestampId = timestampId };
                var queue = CoordinatorPositionLocks.GetOrAdd(key, new ConcurrentQueue<object>());
                coordinatorPositionQueues.Add(key, queue);
                queue.Enqueue(new object());
            }
        }

        private void AddGroupPositionsLocks(
            int[] groupsIds, int timestampId,
            SortedList<GroupPositionKey, ConcurrentQueue<object>> groupPositionQueues)
        {
            foreach (var groupId in groupsIds)
            {
                var key = new GroupPositionKey { GroupId = groupId, TimestampId = timestampId };
                var queue = GroupPositionLocks.GetOrAdd(key, new ConcurrentQueue<object>());
                groupPositionQueues.Add(key, queue);
                queue.Enqueue(new object());
            }
        }

        public static void EnterQueues(IList<ConcurrentQueue<object>> queues)
        {
            foreach (var queue in queues)
            {
                Monitor.Enter(queue);
            }
        }

        public static void ExitQueues(IList<ConcurrentQueue<object>> queues)
        {
            foreach (var queue in queues)
            {
                Monitor.Exit(queue);
            }
        }

        private int? GetPossibleMove(List<SchedulePositionKey> source, List<int> skippedMovesIds,
            out Tuple<List<SchedulePositionKey>, List<SchedulePositionKey>> possibleMove)
        {
            possibleMove = null;
            try
            {
                if (!source.Any())
                {
                    return null;
                }

                var timestampsIds = source.Select(e => e.TimestampId).ToList();

                var movesIds = _unitOfWork.ScheduledMovePositions
                    .Get(e => e.ScheduledMove.IsConfirmed && timestampsIds.Contains(e.TimestampId_2)
                        && !skippedMovesIds.Contains(e.MoveId))
                    .Include(e => e.ScheduledMove)
                    .OrderBy(e => e.ScheduledMove.ScheduleOrder)
                    .GroupBy(e => e.MoveId)
                    .Select(e => e.Key).ToList();

                if (!movesIds.Any())
                {
                    return null;
                }

                var moveId = movesIds.First();

                var _scheduledMove = _unitOfWork.ScheduledMovePositions
                    .Get(e => e.MoveId == moveId);

                var length = _scheduledMove.Count();
                var possibleMoveSource = new List<SchedulePositionKey>();
                var possibleMoveDestination = new List<SchedulePositionKey>();

                _scheduledMove.ToList().ForEach((move) =>
                {
                    possibleMoveSource.Add(new SchedulePositionKey
                    {
                        RoomId = move.RoomId_1,
                        TimestampId = move.TimestampId_1
                    });
                    possibleMoveDestination.Add(new SchedulePositionKey
                    {
                        RoomId = move.RoomId_2,
                        TimestampId = move.TimestampId_2
                    });
                });

                possibleMove = new Tuple<List<SchedulePositionKey>, List<SchedulePositionKey>>(possibleMoveSource, possibleMoveDestination);

                return moveId;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void MakeScheduledMoves(List<SchedulePositionKey> sourceSchedulePositionKeys,
            SortedList<SchedulePositionKey, ConcurrentQueue<object>> L1schedulePositionAllQueues, List<SchedulePositionKey> L1KeysToRemove)
        {
            var coordinatorPositionQueues = new SortedList<CoordinatorPositionKey, ConcurrentQueue<object>>();
            var groupPositionQueues = new SortedList<GroupPositionKey, ConcurrentQueue<object>>();

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
                        L2scheduledMovesAllQueues.Clear(); L1scheduledMovesAllQueuesNotLocked.Clear();
                        L1scheduledMovesAllQueues.Clear(); coordinatorPositionQueues.Clear();
                        groupPositionQueues.Clear();

                        var isQueued = false;
                        var candidateSourceKeys = new List<SchedulePositionKey>();
                        var localKeysToRemove = new List<SchedulePositionKey>();

                        var candidateSourceTimestamps = possibleMove.Item1.Select(e => e.TimestampId).OrderBy(e => e).ToList();
                        var candidateDestTimestamps = possibleMove.Item2.Select(e => e.TimestampId).OrderBy(e => e).ToList();
                        var candidateSourceRoomId = possibleMove.Item1.FirstOrDefault().RoomId;
                        var candidateDestRoomId = possibleMove.Item2.FirstOrDefault().RoomId;

                        var _srcTimestamps = _unitOfWork.Timestamps
                            .Get(e => candidateSourceTimestamps.Contains(e.TimestampId));

                        var _destTimestamps = _unitOfWork.Timestamps
                            .Get(e => candidateDestTimestamps.Contains(e.TimestampId));

                        lock (SchedulePositionLocksL1)
                            lock (SchedulePositionLocksL2)
                            {
                                AddSchedulePositionsLocksL1(possibleMove.Item1, L1scheduledMovesAllQueues);
                                AddSchedulePositionsLocksL2(possibleMove.Item1, L2scheduledMovesAllQueues);
                                AddSchedulePositionsLocksL2(possibleMove.Item2, L2scheduledMovesAllQueues);

                                foreach (var key in possibleMove.Item2)
                                {
                                    var queueL1 = SchedulePositionLocksL1.GetOrAdd(key, new ConcurrentQueue<object>());

                                    if (!_source.Contains(key))
                                    {
                                        queueL1.Enqueue(new object());
                                        L1scheduledMovesAllQueues.Add(key, queueL1);
                                    }
                                    else
                                    {
                                        candidateSourceKeys.Add(key);
                                    }
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
                        EnterQueues(L2scheduledMovesAllQueues.Values);
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
                            var _sourceSchedulePositions = _unitOfWork.SchedulePositions
                            .Get(e => candidateSourceTimestamps.Contains(e.TimestampId) && e.RoomId == candidateSourceRoomId);

                            if (!_sourceSchedulePositions.Any() || _sourceSchedulePositions.Count() != possibleMove.Item1.Count())
                            {
                                _skippedMovesIds.Add((int)currentMoveId);
                                continue;
                            }

                            var schedulePosition = _sourceSchedulePositions.FirstOrDefault();

                            var _courseEdition = _unitOfWork.CourseEditions
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

                            var _scheduledMove = _unitOfWork.ScheduledMoves
                                .Get(e => e.MoveId == currentMoveId);

                            if (_scheduledMove.Count() != possibleMove.Item2.Count())
                            {
                                _skippedMovesIds.Add((int)currentMoveId);
                                continue;
                            }

                            var coordinatorsIds = includableCourseEdition.Coordinators.Select(e => e.CoordinatorId).ToArray();
                            var groupsIds = Methods.GetNestedGroupsIds(includableCourseEdition, _unitOfWork.Groups).ToArray();
                            var returnableGroupsIds = new int[groupsIds.Length];

                            Array.Sort(coordinatorsIds);
                            Array.Copy(groupsIds, returnableGroupsIds, groupsIds.Length);
                            Array.Sort(groupsIds);

                            lock (CoordinatorPositionLocks)
                                lock (GroupPositionLocks)
                                {
                                    foreach (var timestampId in candidateDestTimestamps)
                                    {
                                        AddCoordinatorPositionsLocks(coordinatorsIds, timestampId, coordinatorPositionQueues);
                                        AddGroupPositionsLocks(groupsIds, timestampId, groupPositionQueues);
                                    }
                                }

                            EnterQueues(coordinatorPositionQueues.Values);
                            EnterQueues(groupPositionQueues.Values);
                            try
                            {
                                var _destSchedulePositions = candidateSourceTimestamps.SequenceEqual(candidateDestTimestamps)
                                ?
                                _unitOfWork.SchedulePositions
                                        .Get(e => candidateDestTimestamps.Contains(e.TimestampId) && e.RoomId == candidateDestRoomId)
                                        .Include(e => e.CourseEdition)
                                        .ThenInclude(e => e.Coordinators)
                                        .Include(e => e.CourseEdition)
                                        .ThenInclude(e => e.Groups)
                                        .Select(e => new { e.TimestampId, e.RoomId })
                                :
                                _unitOfWork.SchedulePositions
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

                                var destSchedulePositions = candidateDestTimestamps.Select(timestampId => new SchedulePosition
                                {
                                    RoomId = candidateDestRoomId,
                                    TimestampId = timestampId,
                                    CourseId = includableCourseEdition.CourseId,
                                    CourseEditionId = includableCourseEdition.CourseEditionId
                                }).ToList();

                                var movesIds = _unitOfWork.ScheduledMovePositions
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

                                _unitOfWork.ScheduledMovePositions.DeleteMany(e => movesIds.Contains(e.MoveId));
                                _unitOfWork.ScheduledMoves.DeleteMany(e => movesIds.Contains(e.MoveId));
                                _unitOfWork.SchedulePositions.GetAll().RemoveRange(_sourceSchedulePositions);
                                _unitOfWork.SchedulePositions.GetAll().AddRange(destSchedulePositions);

                                _unitOfWork.Complete();

                                Clients.All.ModifiedSchedulePositions(
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
                                RemoveGroupPositionsLocks(groupPositionQueues);
                                RemoveCoordinatorPositionsLocks(coordinatorPositionQueues);
                                ExitQueues(groupPositionQueues.Values);
                                ExitQueues(coordinatorPositionQueues.Values);
                            }
                        }
                        finally
                        {
                            RemoveSchedulePositionsLocksL2(L2scheduledMovesAllQueues);
                            ExitQueues(L2scheduledMovesAllQueues.Values);
                            if (!isQueued)
                            {
                                foreach (var keyToRemove in localKeysToRemove)
                                {
                                    L1scheduledMovesAllQueues.Remove(keyToRemove);
                                }

                                RemoveSchedulePositionsLocksL1(L1scheduledMovesAllQueues);
                                ExitQueues(L1scheduledMovesAllQueues.Values);
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
                                ExitQueues(L1scheduledMovesAllQueues.Values);
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

        private int GetUserId()
        {
            return int.Parse(Context.User.Claims.FirstOrDefault(x => x.Type == "user_id")?.Value!);
        }

        private bool IsAdmin()
        {
            return Context.User.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == "Administrator");
        }

        private bool IsCoordinator()
        {
            return Context.User.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == "Coordinator");
        }

        private IEnumerable<int> GetRepresentativeGroupsIds()
        {
            return Context.User.Claims.Where(x => x.Type == "representative_group_id").Select(e => int.Parse(e.Value));
        }

        [Authorize(Policy = "Designer")]
        public MessageObject LockCourseEdition(int courseId, int courseEditionId)
        {
            CourseEditionKey courseEditionKey = null;
            ConcurrentQueue<object> courseEditionQueue = null;

            var enqueued = false;

            try
            {
                var userId = GetUserId();
                var isAdmin = IsAdmin();
                var isCoordinator = IsCoordinator();

                courseEditionKey = new CourseEditionKey { CourseId = courseId, CourseEditionId = courseEditionId };
                courseEditionQueue = CourseEditionLocks.GetOrAdd(courseEditionKey, new ConcurrentQueue<object>());

                courseEditionQueue.Enqueue(new object());

                enqueued = true;

                lock (courseEditionQueue)
                {
                    var predicate = PredicateBuilder.New<CourseEdition>(isAdmin);
                    if (!isAdmin && isCoordinator)
                    {
                        predicate = predicate
                            .Or(e => e.Coordinators.Any(f => f.CoordinatorId == userId));
                    }

                    var finalPredicate = predicate.And(e => e.CourseId == courseId && e.CourseEditionId == courseEditionId);

                    var _courseEdition = _unitOfWork.CourseEditions
                        .Get(finalPredicate)
                        .Include(e => e.Coordinators)
                        .Include(e => e.LockUser).ThenInclude(e => e.Staff);

                    if (!_courseEdition.Any())
                    {
                        RemoveCourseEditionLock(courseEditionQueue, courseEditionKey);
                        return new MessageObject { StatusCode = 404, Message = "Could not find requested course or you do not have enough permissions to lock." };
                    }

                    var courseEdition = _courseEdition.FirstOrDefault();
                    var lockUserStaff = courseEdition.LockUser?.Staff;
                    if ((!isAdmin && !(courseEdition is { LockUserId: null })) || (isAdmin && lockUserStaff != null && lockUserStaff.IsAdmin))
                    {
                        RemoveCourseEditionLock(courseEditionQueue, courseEditionKey);
                        return new MessageObject { StatusCode = 400, Message = "Someone has locked this course before you." };
                    }

                    courseEdition.LockUserId = userId;
                    courseEdition.LockUserConnectionId = Context.ConnectionId;
                    _unitOfWork.CourseEditions.Update(courseEdition);

                    _unitOfWork.Complete();
                    Clients.Others.LockCourseEdition(courseEdition.CourseId, courseEdition.CourseEditionId, isAdmin);

                    RemoveCourseEditionLock(courseEditionQueue, courseEditionKey);
                    return new MessageObject { StatusCode = 200 };
                }
            }
            catch (Exception e)
            {
                if (!enqueued)
                {
                    return new MessageObject { StatusCode = 400, Message = "Unexpected error. Please try again later." };
                }

                RemoveCourseEditionLock(courseEditionQueue, courseEditionKey);
                return new MessageObject { StatusCode = 400, Message = "Unexpected error. Please try again later." };
            }
        }

        [Authorize(Policy = "AdministratorOnly")]
        public MessageObject LockAllCourseEditions()
        {
            var courseEditionQueues = new SortedList<CourseEditionKey, ConcurrentQueue<object>>();

            try
            {
                var connectionId = Context.ConnectionId;
                var userId = GetUserId();

                var courseEditionKeys = _unitOfWork.CourseEditions.GetAll().Select(e => new CourseEditionKey
                {
                    CourseId = e.CourseId,
                    CourseEditionId = e.CourseEditionId
                }).ToList();

                if (!courseEditionKeys.Any())
                {
                    return new MessageObject { StatusCode = 204, Message = "Could not find any course editions." };
                }

                lock (CourseEditionLocks)
                {
                    AddCourseEditionsLocks(courseEditionKeys, courseEditionQueues);
                }

                EnterQueues(courseEditionQueues.Values);
                try
                {
                    var courseEditions = _unitOfWork.CourseEditions.GetAll()
                        .Include(e => e.LockUser).ThenInclude(e => e.Staff)
                        .ToList();

                    if (!courseEditions.Any())
                    {
                        return new MessageObject { StatusCode = 204, Message = "Could not find any course editions." };
                    }

                    var currentCourseEditions = new SortedList<CourseEditionKey, CourseEdition>();
                    courseEditions.ForEach(e =>
                    {
                        currentCourseEditions.Add(new CourseEditionKey
                        {
                            CourseId = e.CourseId,
                            CourseEditionId = e.CourseEditionId
                        }, e);
                    });

                    if (currentCourseEditions.Count != courseEditionKeys.Count)
                    {
                        return new MessageObject { StatusCode = 400, Message = "Could not lock all course editions. Please try again later." };
                    }

                    foreach (var currentCourseEdition in currentCourseEditions)
                    {
                        var lockUserStaff = currentCourseEdition.Value.LockUser?.Staff;
                        if (!courseEditionQueues.TryGetValue(currentCourseEdition.Key, out _)
                            || (lockUserStaff != null && lockUserStaff.IsAdmin))
                        {
                            return new MessageObject {
                                StatusCode = 400,
                                Message = "Could not lock all course editions right now. Possibly someone locked one of them before you." 
                            };
                        }
                        currentCourseEdition.Value.LockUserId = userId;
                        currentCourseEdition.Value.LockUserConnectionId = connectionId;
                    }

                    _unitOfWork.CourseEditions.GetAll().UpdateRange(currentCourseEditions.Values);

                    _unitOfWork.Complete();
                    
                    foreach (var currentCourseEdition in currentCourseEditions)
                    {
                        Clients.Others.LockCourseEdition(
                            currentCourseEdition.Value.CourseId, 
                            currentCourseEdition.Value.CourseEditionId, 
                            true);
                    }

                    return new MessageObject { StatusCode = 200 };
                }
                finally
                {
                    RemoveCourseEditionsLocks(courseEditionQueues);
                    ExitQueues(courseEditionQueues.Values);
                }
            }
            catch (Exception e)
            {
                return new MessageObject { StatusCode = 400, Message = "Unexpected error. Please try again later." };
            }
        }

        [Authorize(Policy = "AdministratorOnly")]
        public MessageObject LockAllCourseEditionsForCourse(int courseId)
        {
            var courseEditionQueues = new SortedList<CourseEditionKey, ConcurrentQueue<object>>();

            try
            {
                var connectionId = Context.ConnectionId;
                var userId = GetUserId();

                var courseEditionKeys = _unitOfWork.CourseEditions.Get(e => e.CourseId == courseId).Select(e => new CourseEditionKey
                {
                    CourseId = e.CourseId,
                    CourseEditionId = e.CourseEditionId
                }).ToList();

                if (!courseEditionKeys.Any())
                {
                    return new MessageObject { StatusCode = 204, Message = "Could not find any course editions." };
                }

                lock (CourseEditionLocks)
                {
                    AddCourseEditionsLocks(courseEditionKeys, courseEditionQueues);
                }

                EnterQueues(courseEditionQueues.Values);
                try
                {
                    var courseEditions = _unitOfWork.CourseEditions.Get(e => e.CourseId == courseId)
                        .Include(e => e.LockUser).ThenInclude(e => e.Staff)
                        .ToList();

                    if (!courseEditions.Any())
                    {
                        return new MessageObject { StatusCode = 204, Message = "Could not find any course editions." };
                    }

                    var currentCourseEditions = new SortedList<CourseEditionKey, CourseEdition>();
                    courseEditions.ForEach(e =>
                    {
                        currentCourseEditions.Add(new CourseEditionKey
                        {
                            CourseId = e.CourseId,
                            CourseEditionId = e.CourseEditionId
                        }, e);
                    });

                    if (currentCourseEditions.Count != courseEditionKeys.Count)
                    {
                        return new MessageObject { StatusCode = 400, Message = "Could not lock all course editions. Please try again later." };
                    }

                    foreach (var currentCourseEdition in currentCourseEditions)
                    {
                        var lockUserStaff = currentCourseEdition.Value.LockUser?.Staff;
                        if (!courseEditionQueues.TryGetValue(currentCourseEdition.Key, out _)
                            || (lockUserStaff != null && lockUserStaff.IsAdmin))
                        {
                            return new MessageObject
                            {
                                StatusCode = 400,
                                Message = "Could not lock all course editions right now. Possibly someone locked one of them before you."
                            };
                        }
                        currentCourseEdition.Value.LockUserId = userId;
                        currentCourseEdition.Value.LockUserConnectionId = connectionId;
                    }

                    _unitOfWork.CourseEditions.GetAll().UpdateRange(currentCourseEditions.Values);

                    _unitOfWork.Complete();

                    foreach (var currentCourseEdition in currentCourseEditions)
                    {
                        Clients.Others.LockCourseEdition(
                            currentCourseEdition.Value.CourseId,
                            currentCourseEdition.Value.CourseEditionId,
                            true);
                    }

                    return new MessageObject { StatusCode = 200 };
                }
                finally
                {
                    RemoveCourseEditionsLocks(courseEditionQueues);
                    ExitQueues(courseEditionQueues.Values);
                }
            }
            catch (Exception e)
            {
                return new MessageObject { StatusCode = 400, Message = "Unexpected error. Please try again later." };
            }
        }

        [Authorize(Policy = "AdministratorOnly")]
        public MessageObject LockAllCoordinatorCourses(int coordinatorId, int courseId, int courseEditionId)
        {
            var courseEditionQueues = new SortedList<CourseEditionKey, ConcurrentQueue<object>>();
            var schedulePositionQueuesL1 = new SortedList<SchedulePositionKey, ConcurrentQueue<object>>();
            var schedulePositionQueuesL2 = new SortedList<SchedulePositionKey, ConcurrentQueue<object>>();

            try
            {
                var connectionId = Context.ConnectionId;
                var userId = GetUserId();

                var courseEditionKeys = _unitOfWork.CoordinatorCourseEditions
                    .Get(e => e.CoordinatorId == coordinatorId).Select(e => new CourseEditionKey
                    {
                        CourseId = e.CourseId,
                        CourseEditionId = e.CourseEditionId
                    }).ToList();

                courseEditionKeys.Add(new CourseEditionKey { CourseId = courseId, CourseEditionId = courseEditionId });

                lock (CourseEditionLocks)
                {
                    AddCourseEditionsLocks(courseEditionKeys, courseEditionQueues);
                }

                EnterQueues(courseEditionQueues.Values);
                try
                {
                    var currentCourseEdition = _unitOfWork.CourseEditions
                        .Get(e => e.CourseId == courseId && e.CourseEditionId == courseEditionId)
                        .Include(e => e.LockUser).ThenInclude(e => e.Staff)
                        .FirstOrDefault();

                    if (currentCourseEdition == null)
                    {
                        return new MessageObject { StatusCode = 404, Message = "Could not find current course edition." };
                    }

                    var courseEditions = _unitOfWork.CoordinatorCourseEditions
                        .Get(e => e.CoordinatorId == coordinatorId)
                        .Include(e => e.CourseEdition).ThenInclude(e => e.LockUser).ThenInclude(e => e.Staff)
                        .ToList();

                    var currentCourseEditions = new SortedList<CourseEditionKey, CourseEdition>();
                    courseEditions.ForEach(e =>
                    {
                        currentCourseEditions.Add(new CourseEditionKey
                        {
                            CourseId = e.CourseId,
                            CourseEditionId = e.CourseEditionId
                        }, e.CourseEdition);
                    });

                    currentCourseEditions.Add(
                        new CourseEditionKey { CourseId = courseId, CourseEditionId = courseEditionId },
                        currentCourseEdition);

                    if (currentCourseEditions.Count != courseEditionKeys.Count)
                    {
                        return new MessageObject { StatusCode = 400, Message = "Could not lock requested course editions. Please try again later." };
                    }

                    foreach (var courseEdition in currentCourseEditions)
                    {
                        var lockUserStaff = courseEdition.Value.LockUser?.Staff;
                        if (!courseEditionQueues.TryGetValue(courseEdition.Key, out _)
                            || (lockUserStaff != null && lockUserStaff.IsAdmin))
                        {
                            return new MessageObject
                            {
                                StatusCode = 400,
                                Message = "Could not lock requested course editions right now. Possibly someone locked one of them before you."
                            };
                        }
                        courseEdition.Value.LockUserId = userId;
                        courseEdition.Value.LockUserConnectionId = connectionId;
                    }

                    var courseEditionIds = courseEditionKeys.Select(e => e.CourseEditionId).ToList();
                    var schedulePositionKeys = _unitOfWork.SchedulePositions
                        .Get(e => courseEditionIds.Contains(e.CourseEditionId)).Select(e => new SchedulePositionKey
                        {
                            TimestampId = e.TimestampId,
                            RoomId = e.RoomId
                        }).ToList();

                    lock (SchedulePositionLocksL1)
                    lock (SchedulePositionLocksL2)
                    {
                        AddSchedulePositionsLocksL1(schedulePositionKeys, schedulePositionQueuesL1);
                        AddSchedulePositionsLocksL2(schedulePositionKeys, schedulePositionQueuesL2);
                    }

                    EnterQueues(schedulePositionQueuesL1.Values);
                    EnterQueues(schedulePositionQueuesL2.Values);
                    try
                    {
                        var schedulePositions = _unitOfWork.SchedulePositions
                            .Get(e => courseEditionIds.Contains(e.CourseEditionId))
                            .Include(e => e.LockUser).ThenInclude(e => e.Staff)
                            .Include(e => e.Timestamp)
                            .ToList();

                        var currentSchedulePositions = new SortedList<SchedulePositionKey, SchedulePosition>();
                        schedulePositions.ForEach(e =>
                        {
                            currentSchedulePositions.Add(new SchedulePositionKey
                            {
                                TimestampId = e.TimestampId,
                                RoomId = e.RoomId
                            }, e);
                        });

                        if (currentSchedulePositions.Count != schedulePositionKeys.Count)
                        {
                            return new MessageObject { StatusCode = 400, Message = "Could not lock coordinator's schedule positions. Please try again later." };
                        }

                        foreach (var currentSchedulePosition in currentSchedulePositions)
                        {
                            var lockUserStaff = currentSchedulePosition.Value.LockUser?.Staff;
                            if (!schedulePositionQueuesL1.TryGetValue(currentSchedulePosition.Key, out _)
                                || (lockUserStaff != null && lockUserStaff.IsAdmin))
                            {
                                return new MessageObject
                                {
                                    StatusCode = 400,
                                    Message = "Could not lock coordinator's schedule positions right now. Possibly someone locked one of them before you."
                                };
                            }
                            currentSchedulePosition.Value.LockUserId = userId;
                            currentSchedulePosition.Value.LockUserConnectionId = connectionId;
                        }

                        _unitOfWork.CourseEditions.GetAll().UpdateRange(currentCourseEditions.Values);
                        _unitOfWork.SchedulePositions.GetAll().UpdateRange(currentSchedulePositions.Values);

                        _unitOfWork.Complete();

                        var positionsWeeks = schedulePositions.GroupBy(e => new
                        {
                            e.CourseId,
                            e.CourseEditionId,
                            e.RoomId,
                            e.Timestamp.PeriodIndex,
                            e.Timestamp.Day
                        })
                            .Select(e => new { e.Key, Weeks = e.Select(f => f.Timestamp.Week).ToList() })
                            .ToList();

                        foreach (var courseEdition in currentCourseEditions)
                        {
                            Clients.Others.LockCourseEdition(
                                courseEdition.Value.CourseId,
                                courseEdition.Value.CourseEditionId, 
                                true);
                        }
                        
                        foreach (var position in positionsWeeks)
                        {
                            Clients.Others.LockSchedulePositions(
                                position.Key.CourseId, position.Key.CourseEditionId,
                                position.Key.RoomId, position.Key.PeriodIndex,
                                position.Key.Day, position.Weeks.ToArray(),
                                true);
                        }

                        return new MessageObject { StatusCode = 200 };
                    }
                    finally
                    {
                        RemoveSchedulePositionsLocksL2(schedulePositionQueuesL2);
                        RemoveSchedulePositionsLocksL1(schedulePositionQueuesL1);
                        ExitQueues(schedulePositionQueuesL2.Values);
                        ExitQueues(schedulePositionQueuesL1.Values);
                    }
                }
                finally
                {
                    RemoveCourseEditionsLocks(courseEditionQueues);
                    ExitQueues(courseEditionQueues.Values);
                }
            }
            catch (Exception e)
            {
                return new MessageObject { StatusCode = 400, Message = "Unexpected error. Please try again later." };
            }
        }

        [Authorize(Policy = "AdministratorOnly")]
        public MessageObject LockAllGroupCourses(int groupId, int courseId, int courseEditionId)
        {
            var courseEditionQueues = new SortedList<CourseEditionKey, ConcurrentQueue<object>>();
            var schedulePositionQueuesL1 = new SortedList<SchedulePositionKey, ConcurrentQueue<object>>();
            var schedulePositionQueuesL2 = new SortedList<SchedulePositionKey, ConcurrentQueue<object>>();

            try
            {
                var connectionId = Context.ConnectionId;
                var userId = GetUserId();

                var group = _unitOfWork.Groups.Get(e => e.GroupId == groupId).FirstOrDefault();

                if (group == null)
                {
                    return new MessageObject { StatusCode = 400, Message = "Could not find group." };
                }

                var groupsIds = Methods.GetNestedGroupsIds(new List<Group>() { group }, _unitOfWork.Groups);
                var courseEditionKeys = _unitOfWork.GroupCourseEditions
                    .Get(e => groupsIds.Contains(e.GroupId)).Select(e => new CourseEditionKey
                    {
                        CourseId = e.CourseId,
                        CourseEditionId = e.CourseEditionId
                    }).ToList();

                var key = new CourseEditionKey { CourseId = courseId, CourseEditionId = courseEditionId };
                if (!courseEditionKeys.Contains(key))
                {
                    courseEditionKeys.Add(key);
                }

                lock (CourseEditionLocks)
                {
                    AddCourseEditionsLocks(courseEditionKeys, courseEditionQueues);
                }

                EnterQueues(courseEditionQueues.Values);
                try
                {
                    var currentCourseEdition = _unitOfWork.CourseEditions
                        .Get(e => e.CourseId == courseId && e.CourseEditionId == courseEditionId)
                        .Include(e => e.LockUser).ThenInclude(e => e.Staff)
                        .FirstOrDefault();

                    if (currentCourseEdition == null)
                    {
                        return new MessageObject { StatusCode = 404, Message = "Could not find current course edition." };
                    }

                    var currentGroupsIds = Methods.GetNestedGroupsIds(new List<Group>() { group }, _unitOfWork.Groups);
                    if (currentGroupsIds.Except(groupsIds).Count() > 0 || currentGroupsIds.Count != groupsIds.Count)
                    {
                        return new MessageObject { StatusCode = 404, Message = "Could not find group. Please try again later." };
                    }

                    var courseEditions = _unitOfWork.GroupCourseEditions
                        .Get(e => currentGroupsIds.Contains(e.GroupId))
                        .Include(e => e.CourseEdition).ThenInclude(e => e.LockUser).ThenInclude(e => e.Staff)
                        .ToList();

                    var currentCourseEditions = new SortedList<CourseEditionKey, CourseEdition>();
                    courseEditions.ForEach(e =>
                    {
                        currentCourseEditions.Add(new CourseEditionKey
                        {
                            CourseId = e.CourseId,
                            CourseEditionId = e.CourseEditionId
                        }, e.CourseEdition);
                    });

                    currentCourseEditions.Add(
                        new CourseEditionKey { CourseId = courseId, CourseEditionId = courseEditionId },
                        currentCourseEdition);

                    if (currentCourseEditions.Count != courseEditionKeys.Count)
                    {
                        return new MessageObject { StatusCode = 404, Message = "Could not lock requested course editions. Please try again later." };
                    }

                    foreach (var courseEdition in currentCourseEditions)
                    {
                        var lockUserStaff = courseEdition.Value.LockUser?.Staff;
                        if (!courseEditionQueues.TryGetValue(courseEdition.Key, out _)
                            || (lockUserStaff != null && lockUserStaff.IsAdmin))
                        {
                            return new MessageObject
                            {
                                StatusCode = 404,
                                Message = "Could not lock requested course editions right now. Possibly someone locked one of them before you."
                            };
                        }
                        courseEdition.Value.LockUserId = userId;
                        courseEdition.Value.LockUserConnectionId = connectionId;
                    }

                    var courseEditionIds = courseEditionKeys.Select(e => e.CourseEditionId).ToList();
                    var schedulePositionKeys = _unitOfWork.SchedulePositions
                        .Get(e => courseEditionIds.Contains(e.CourseEditionId)).Select(e => new SchedulePositionKey
                        {
                            TimestampId = e.TimestampId,
                            RoomId = e.RoomId
                        }).ToList();

                    lock (SchedulePositionLocksL1)
                    lock (SchedulePositionLocksL2)
                    {
                        AddSchedulePositionsLocksL1(schedulePositionKeys, schedulePositionQueuesL1);
                        AddSchedulePositionsLocksL2(schedulePositionKeys, schedulePositionQueuesL2);
                    }

                    EnterQueues(schedulePositionQueuesL1.Values);
                    EnterQueues(schedulePositionQueuesL2.Values);
                    try
                    {
                        var schedulePositions = _unitOfWork.SchedulePositions
                            .Get(e => courseEditionIds.Contains(e.CourseEditionId))
                            .Include(e => e.LockUser).ThenInclude(e => e.Staff)
                            .Include(e => e.Timestamp)
                            .ToList();

                        var currentSchedulePositions = new SortedList<SchedulePositionKey, SchedulePosition>();
                        schedulePositions.ForEach(e =>
                        {
                            currentSchedulePositions.Add(new SchedulePositionKey
                            {
                                TimestampId = e.TimestampId,
                                RoomId = e.RoomId
                            }, e);
                        });

                        if (currentSchedulePositions.Count != schedulePositionKeys.Count)
                        {
                            return new MessageObject { StatusCode = 404, Message = "Could not lock groups' schedule positions. Please try again later." };
                        }

                        foreach (var currentSchedulePosition in currentSchedulePositions)
                        {
                            var lockUserStaff = currentSchedulePosition.Value.LockUser?.Staff;
                            if (!schedulePositionQueuesL1.TryGetValue(currentSchedulePosition.Key, out _)
                                || (lockUserStaff != null && lockUserStaff.IsAdmin))
                            {
                                return new MessageObject
                                {
                                    StatusCode = 404,
                                    Message = "Could not lock groups' schedule positions right now. Possibly someone locked one of them before you."
                                };
                            }
                            currentSchedulePosition.Value.LockUserId = userId;
                            currentSchedulePosition.Value.LockUserConnectionId = connectionId;
                        }

                        _unitOfWork.CourseEditions.GetAll().UpdateRange(currentCourseEditions.Values);
                        _unitOfWork.SchedulePositions.GetAll().UpdateRange(currentSchedulePositions.Values);

                        _unitOfWork.Complete();

                        var positionsWeeks = schedulePositions.GroupBy(e => new
                        {
                            e.CourseId,
                            e.CourseEditionId,
                            e.RoomId,
                            e.Timestamp.PeriodIndex,
                            e.Timestamp.Day
                        })
                            .Select(e => new { e.Key, Weeks = e.Select(f => f.Timestamp.Week).ToList() })
                            .ToList();

                        foreach (var courseEdition in currentCourseEditions)
                        {
                            Clients.Others.LockCourseEdition(
                                courseEdition.Value.CourseId,
                                courseEdition.Value.CourseEditionId,
                                true);
                        }

                        foreach (var position in positionsWeeks)
                        {
                            Clients.Others.LockSchedulePositions(
                                position.Key.CourseId, position.Key.CourseEditionId,
                                position.Key.RoomId, position.Key.PeriodIndex,
                                position.Key.Day, position.Weeks.ToArray(),
                                true);
                        }

                        return new MessageObject { StatusCode = 200 };
                    }
                    finally
                    {
                        RemoveSchedulePositionsLocksL2(schedulePositionQueuesL2);
                        RemoveSchedulePositionsLocksL1(schedulePositionQueuesL1);
                        ExitQueues(schedulePositionQueuesL2.Values);
                        ExitQueues(schedulePositionQueuesL1.Values);
                    }
                }
                finally
                {
                    RemoveCourseEditionsLocks(courseEditionQueues);
                    ExitQueues(courseEditionQueues.Values);
                }
            }
            catch (Exception e)
            {
                return new MessageObject { StatusCode = 400, Message = "Unexpected error. Please try again later." };
            }
        }

        [Authorize(Policy = "AdministratorOnly")]
        public MessageObject LockAllCoursesForGroupChange(int originGroupId, int? destinationGroupId)
        {
            var courseEditionQueues = new SortedList<CourseEditionKey, ConcurrentQueue<object>>();
            var schedulePositionQueuesL1 = new SortedList<SchedulePositionKey, ConcurrentQueue<object>>();
            var schedulePositionQueuesL2 = new SortedList<SchedulePositionKey, ConcurrentQueue<object>>();

            try
            {
                var connectionId = Context.ConnectionId;
                var userId = GetUserId();

                var originGroup = _unitOfWork.Groups.Get(e => e.GroupId == originGroupId).FirstOrDefault();
                if (originGroup == null)
                {
                    return new MessageObject { StatusCode = 400, Message = "Could not find origin group." };
                }

                if (originGroup.ParentGroupId == destinationGroupId)
                {
                    return new MessageObject { StatusCode = 400, Message = "Destination group cannot be the direct parent of origin group." };
                }

                Group destinationGroup = null;
                if (destinationGroupId != null)
                {
                    destinationGroup = _unitOfWork.Groups.Get(e => e.GroupId == destinationGroupId).FirstOrDefault();
                    if (destinationGroup == null)
                    {
                        return new MessageObject { StatusCode = 400, Message = "Could not find destination group." };
                    }
                }

                var childGroupsIds = Methods.GetChildGroups(new List<Group>() { originGroup }, _unitOfWork.Groups);
                var parentGroupsIds = destinationGroup != null ? Methods.GetParentGroups(new List<Group>() { destinationGroup }, _unitOfWork.Groups) : new List<int>();

                if (destinationGroupId != null)
                {
                    if (childGroupsIds.Contains((int)destinationGroupId))
                    {
                        return new MessageObject { StatusCode = 400, Message = "Destination group cannot be the child of origin group." };
                    }
                }

                var groupsIds = childGroupsIds.Union(parentGroupsIds).ToList();
                var courseEditionKeys = _unitOfWork.GroupCourseEditions
                    .Get(e => groupsIds.Contains(e.GroupId))
                    .GroupBy(e => new {e.CourseId, e.CourseEditionId})
                    .Select(e => new CourseEditionKey
                    {
                        CourseId = e.Key.CourseId,
                        CourseEditionId = e.Key.CourseEditionId
                    }).ToList();

                lock (CourseEditionLocks)
                {
                    AddCourseEditionsLocks(courseEditionKeys, courseEditionQueues);
                }

                EnterQueues(courseEditionQueues.Values);
                try
                {
                    var currentGroupsIds = Methods.GetChildGroups(new List<Group>() { originGroup }, _unitOfWork.Groups)
                        .Union(destinationGroup != null ? Methods.GetParentGroups(new List<Group>() { destinationGroup }, _unitOfWork.Groups) : new List<int>())
                        .ToList();
                    
                    if (currentGroupsIds.Except(groupsIds).Count() > 0 || currentGroupsIds.Count != groupsIds.Count)
                    {
                        return new MessageObject { StatusCode = 404, Message = "Could not find some of the groups. Please try again later." };
                    }

                    var courseEditions = _unitOfWork.GroupCourseEditions
                        .Get(e => currentGroupsIds.Contains(e.GroupId))
                        .Include(e => e.CourseEdition).ThenInclude(e => e.LockUser).ThenInclude(e => e.Staff)
                        .ToList();

                    var currentCourseEditions = new SortedList<CourseEditionKey, CourseEdition>();
                    courseEditions.ForEach(e =>
                    {
                        currentCourseEditions.TryAdd(new CourseEditionKey
                        {
                            CourseId = e.CourseId,
                            CourseEditionId = e.CourseEditionId
                        }, e.CourseEdition);
                    });

                    if (currentCourseEditions.Count != courseEditionKeys.Count)
                    {
                        return new MessageObject { StatusCode = 404, Message = "Could not lock requested course editions. Please try again later." };
                    }

                    foreach (var courseEdition in currentCourseEditions)
                    {
                        var lockUserStaff = courseEdition.Value.LockUser?.Staff;
                        if (!courseEditionQueues.TryGetValue(courseEdition.Key, out _)
                            || (lockUserStaff != null && lockUserStaff.IsAdmin))
                        {
                            return new MessageObject
                            {
                                StatusCode = 404,
                                Message = "Could not lock requested course editions right now. Possibly someone locked one of them before you."
                            };
                        }
                        courseEdition.Value.LockUserId = userId;
                        courseEdition.Value.LockUserConnectionId = connectionId;
                    }

                    var courseEditionIds = courseEditionKeys.Select(e => e.CourseEditionId).ToList();
                    var schedulePositionKeys = _unitOfWork.SchedulePositions
                        .Get(e => courseEditionIds.Contains(e.CourseEditionId)).Select(e => new SchedulePositionKey
                        {
                            TimestampId = e.TimestampId,
                            RoomId = e.RoomId
                        }).ToList();

                    lock (SchedulePositionLocksL1)
                    lock (SchedulePositionLocksL2)
                    {
                        AddSchedulePositionsLocksL1(schedulePositionKeys, schedulePositionQueuesL1);
                        AddSchedulePositionsLocksL2(schedulePositionKeys, schedulePositionQueuesL2);
                    }

                    EnterQueues(schedulePositionQueuesL1.Values);
                    EnterQueues(schedulePositionQueuesL2.Values);
                    try
                    {
                        var schedulePositions = _unitOfWork.SchedulePositions
                            .Get(e => courseEditionIds.Contains(e.CourseEditionId))
                            .Include(e => e.LockUser).ThenInclude(e => e.Staff)
                            .Include(e => e.Timestamp)
                            .ToList();

                        var currentSchedulePositions = new SortedList<SchedulePositionKey, SchedulePosition>();
                        schedulePositions.ForEach(e =>
                        {
                            currentSchedulePositions.Add(new SchedulePositionKey
                            {
                                TimestampId = e.TimestampId,
                                RoomId = e.RoomId
                            }, e);
                        });

                        if (currentSchedulePositions.Count != schedulePositionKeys.Count)
                        {
                            return new MessageObject { StatusCode = 404, Message = "Could not lock groups' schedule positions. Please try again later." };
                        }

                        foreach (var currentSchedulePosition in currentSchedulePositions)
                        {
                            var lockUserStaff = currentSchedulePosition.Value.LockUser?.Staff;
                            if (!schedulePositionQueuesL1.TryGetValue(currentSchedulePosition.Key, out _)
                                || (lockUserStaff != null && lockUserStaff.IsAdmin))
                            {
                                return new MessageObject
                                {
                                    StatusCode = 404,
                                    Message = "Could not lock groups' schedule positions right now. Possibly someone locked one of them before you."
                                };
                            }
                            currentSchedulePosition.Value.LockUserId = userId;
                            currentSchedulePosition.Value.LockUserConnectionId = connectionId;
                        }

                        _unitOfWork.CourseEditions.GetAll().UpdateRange(currentCourseEditions.Values);
                        _unitOfWork.SchedulePositions.GetAll().UpdateRange(currentSchedulePositions.Values);

                        _unitOfWork.Complete();

                        var positionsWeeks = schedulePositions.GroupBy(e => new
                        {
                            e.CourseId,
                            e.CourseEditionId,
                            e.RoomId,
                            e.Timestamp.PeriodIndex,
                            e.Timestamp.Day
                        })
                            .Select(e => new { e.Key, Weeks = e.Select(f => f.Timestamp.Week).ToList() })
                            .ToList();

                        foreach (var courseEdition in currentCourseEditions)
                        {
                            Clients.Others.LockCourseEdition(
                                courseEdition.Value.CourseId,
                                courseEdition.Value.CourseEditionId,
                                true);
                        }

                        foreach (var position in positionsWeeks)
                        {
                            Clients.Others.LockSchedulePositions(
                                position.Key.CourseId, position.Key.CourseEditionId,
                                position.Key.RoomId, position.Key.PeriodIndex,
                                position.Key.Day, position.Weeks.ToArray(),
                                true);
                        }

                        return new MessageObject { StatusCode = 200 };
                    }
                    finally
                    {
                        RemoveSchedulePositionsLocksL2(schedulePositionQueuesL2);
                        RemoveSchedulePositionsLocksL1(schedulePositionQueuesL1);
                        ExitQueues(schedulePositionQueuesL2.Values);
                        ExitQueues(schedulePositionQueuesL1.Values);
                    }
                }
                finally
                {
                    RemoveCourseEditionsLocks(courseEditionQueues);
                    ExitQueues(courseEditionQueues.Values);
                }
            }
            catch (Exception e)
            {
                return new MessageObject { StatusCode = 400, Message = "Unexpected error. Please try again later." };
            }
        }

        [Authorize(Policy = "Assistant")]
        public MessageObject LockSchedulePositions(int roomId, int periodIndex, int day, int[] weeks)
        {
            var schedulePositionQueuesL1 = new SortedList<SchedulePositionKey, ConcurrentQueue<object>>();
            var schedulePositionQueuesL2 = new SortedList<SchedulePositionKey, ConcurrentQueue<object>>();

            try
            {
                var userId = GetUserId();
                var isAdmin = IsAdmin();
                var isCoordinator = IsCoordinator();
                var representativeGroupsIds = GetRepresentativeGroupsIds();

                var _timestamps = _unitOfWork.Timestamps
                        .Get(e => e.PeriodIndex == periodIndex && e.Day == day && weeks.Contains(e.Week))
                        .Select(e => e.TimestampId)
                        .OrderBy(e => e).ToList();

                if (_timestamps.Count != weeks.Length)
                {
                    return new MessageObject { StatusCode = 404, Message = "Could not find requested time periods." };
                }

                lock (SchedulePositionLocksL1)
                {
                    AddSchedulePositionsLocksL1(_timestamps, roomId, schedulePositionQueuesL1);
                }

                EnterQueues(schedulePositionQueuesL1.Values);
                try
                {
                    lock (SchedulePositionLocksL2)
                    {
                        AddSchedulePositionsLocksL2(schedulePositionQueuesL1.Keys.ToList(), schedulePositionQueuesL2);
                    }

                    EnterQueues(schedulePositionQueuesL2.Values);
                    try
                    {
                        var predicate = PredicateBuilder.New<SchedulePosition>(true);
                        if (!isAdmin && !isCoordinator && representativeGroupsIds.Count() > 0)
                        {
                            predicate = predicate
                                .Or(e => e.CourseEdition.Groups.Any(f => representativeGroupsIds.Contains(f.GroupId)));
                        }

                        var finalPredicate = PredicateBuilder.New<SchedulePosition>()
                            .And(e => _timestamps.Contains(e.TimestampId) && e.RoomId == roomId)
                            .And(predicate);

                        var _schedulePositions = _unitOfWork.SchedulePositions
                            .Get(finalPredicate)
                            .Include(e => e.CourseEdition)
                                .ThenInclude(e => e.Groups)
                            .Include(e => e.LockUser)
                                .ThenInclude(e => e.Staff);

                        if (_schedulePositions.Count() != weeks.Length)
                        {
                            return new MessageObject { StatusCode = 404, Message = "Could not find requested positions in schedule or you do not have enough permissions to lock." };
                        }

                        if ((!isAdmin && Enumerable.Any(_schedulePositions, schedulePosition => schedulePosition.LockUserId != null))
                            || (isAdmin && Enumerable.Any(_schedulePositions, schedulePosition =>
                            {
                                var lockUserStaff = schedulePosition.LockUser?.Staff;
                                return lockUserStaff != null && lockUserStaff.IsAdmin;
                            })))
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

                        _unitOfWork.SchedulePositions.GetAll().UpdateRange(_schedulePositions);

                        _unitOfWork.Complete();
                        Clients.Others.LockSchedulePositions(
                            courseEdition.CourseId, courseEdition.CourseEditionId,
                            roomId, periodIndex,
                            day, weeks, isAdmin
                        );

                        return new MessageObject { StatusCode = 200 };
                    }
                    finally
                    {
                        RemoveSchedulePositionsLocksL2(schedulePositionQueuesL2);
                        ExitQueues(schedulePositionQueuesL2.Values);
                    }
                }
                finally
                {
                    RemoveSchedulePositionsLocksL1(schedulePositionQueuesL1);
                    ExitQueues(schedulePositionQueuesL1.Values);
                }
            }
            catch (Exception e)
            {
                return new MessageObject { StatusCode = 400, Message = "Unexpected error. Please try again later." };
            }
        }

        [Authorize(Policy = "Designer")]
        public MessageObject UnlockCourseEdition(int courseId, int courseEditionId)
        {
            CourseEditionKey courseEditionKey = null;
            ConcurrentQueue<object> courseEditionQueue = null;
            
            var enqueued = false;
            
            try
            {
                var userId = GetUserId();

                courseEditionKey = new CourseEditionKey { CourseId = courseId, CourseEditionId = courseEditionId };
                courseEditionQueue = CourseEditionLocks.GetOrAdd(courseEditionKey, new ConcurrentQueue<object>());

                courseEditionQueue.Enqueue(new object());

                enqueued = true;

                lock (courseEditionQueue)
                {
                    var _courseEdition = _unitOfWork.CourseEditions
                        .Get(e => e.CourseId == courseId && e.CourseEditionId == courseEditionId);

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
                    _unitOfWork.CourseEditions.Update(courseEdition);

                    _unitOfWork.Complete();
                    Clients.Others.UnlockCourseEdition(courseEdition.CourseId, courseEdition.CourseEditionId);

                    RemoveCourseEditionLock(courseEditionQueue, courseEditionKey);
                    return new MessageObject { StatusCode = 200 };
                }
            }
            catch (Exception e)
            {
                if (!enqueued)
                {
                    return new MessageObject { StatusCode = 400, Message = "Unexpected error. Please try again later." };
                }
                
                RemoveCourseEditionLock(courseEditionQueue, courseEditionKey);
                return new MessageObject { StatusCode = 400, Message = "Unexpected error. Please try again later." };
            }
        }

        [Authorize(Policy = "Assistant")]
        public MessageObject UnlockSchedulePositions(int roomId, int periodIndex, int day, int[] weeks)
        {
            var schedulePositionQueuesL1 = new SortedList<SchedulePositionKey, ConcurrentQueue<object>>();
            var schedulePositionQueuesL2 = new SortedList<SchedulePositionKey, ConcurrentQueue<object>>();

            try
            {
                var userId = GetUserId();

                var _timestamps = _unitOfWork.Timestamps
                        .Get(e => e.PeriodIndex == periodIndex && e.Day == day && weeks.Contains(e.Week))
                        .Select(e => e.TimestampId)
                        .OrderBy(e => e).ToList();

                if (_timestamps.Count != weeks.Length)
                {
                    return new MessageObject { StatusCode = 404, Message = "Could not find requested time periods." };
                }

                lock (SchedulePositionLocksL1)
                {
                    AddSchedulePositionsLocksL1(_timestamps, roomId, schedulePositionQueuesL1);
                }

                EnterQueues(schedulePositionQueuesL1.Values);
                try
                {
                    lock (SchedulePositionLocksL2)
                    {
                        AddSchedulePositionsLocksL2(schedulePositionQueuesL1.Keys.ToList(), schedulePositionQueuesL2);
                    }

                    EnterQueues(schedulePositionQueuesL2.Values);
                    try
                    {
                        var _schedulePositions = _unitOfWork.SchedulePositions
                            .Get(e => _timestamps.Contains(e.TimestampId) && e.RoomId == roomId)
                            .Include(e => e.CourseEdition)
                            .Include(e => e.Timestamp);

                        if (_schedulePositions.Count() != weeks.Length)
                        {
                            return new MessageObject { StatusCode = 404, Message = "Could not find requested positions in schedule." };
                        }

                        var unlockedWeeks = new List<int>();
                        foreach (var schedulePosition in _schedulePositions)
                        {
                            if (schedulePosition.LockUserId == null || schedulePosition.LockUserId != userId 
                                || schedulePosition.LockUserConnectionId != Context.ConnectionId)
                            {
                                continue;
                            }
                            
                            schedulePosition.LockUserId = null;
                            schedulePosition.LockUserConnectionId = null;
                            unlockedWeeks.Add(schedulePosition.Timestamp.Week);
                        }

                        var courseEdition = _schedulePositions.FirstOrDefault()?.CourseEdition;
                        if (courseEdition == null)
                        {
                            return new MessageObject { StatusCode = 400, Message = "There was an unexpected error." };
                        }

                        _unitOfWork.SchedulePositions.GetAll().UpdateRange(_schedulePositions);

                        _unitOfWork.Complete();
                        if (unlockedWeeks.Count != weeks.Length)
                        {
                            Clients.All.UnlockSchedulePositions(
                                courseEdition.CourseId, courseEdition.CourseEditionId,
                                roomId, periodIndex,
                                day, unlockedWeeks.ToArray());
                            
                            return new MessageObject { StatusCode = 404, Message = "Not all positions have been unlocked." };
                        } else
                        {
                            Clients.Others.UnlockSchedulePositions(
                                courseEdition.CourseId, courseEdition.CourseEditionId,
                                roomId, periodIndex,
                                day, weeks);

                            return new MessageObject { StatusCode = 200 };
                        }
                    }
                    finally
                    {
                        RemoveSchedulePositionsLocksL2(schedulePositionQueuesL2);
                        ExitQueues(schedulePositionQueuesL2.Values);
                    }
                }
                finally
                {
                    RemoveSchedulePositionsLocksL1(schedulePositionQueuesL1);
                    ExitQueues(schedulePositionQueuesL1.Values);
                }
            }
            catch (Exception e)
            {
                return new MessageObject { StatusCode = 400, Message = "Unexpected error. Please try again later." };
            }
        }

        [Authorize(Policy = "AdministratorOnly")]
        public MessageObject UnlockAllCourseEditions()
        {
            var courseEditionQueues = new SortedList<CourseEditionKey, ConcurrentQueue<object>>();

            try
            {
                var connectionId = Context.ConnectionId;
                var userId = GetUserId();

                var courseEditionKeys = _unitOfWork.CourseEditions.GetAll().Select(e => new CourseEditionKey
                {
                    CourseId = e.CourseId,
                    CourseEditionId = e.CourseEditionId
                }).ToList();

                if (!courseEditionKeys.Any())
                {
                    return new MessageObject { StatusCode = 204, Message = "Could not find any course editions." };
                }

                lock (CourseEditionLocks)
                {
                    AddCourseEditionsLocks(courseEditionKeys, courseEditionQueues);
                }

                EnterQueues(courseEditionQueues.Values);
                try
                {
                    var courseEditions = _unitOfWork.CourseEditions.GetAll()
                        .ToList();

                    if (!courseEditions.Any())
                    {
                        return new MessageObject { StatusCode = 204, Message = "Could not find any course editions." };
                    }

                    var unlockedCourseEditions = new List<CourseEdition>();
                    foreach (var courseEdition in courseEditions)
                    {
                        if (!courseEditionQueues.TryGetValue(new CourseEditionKey 
                        { 
                            CourseId = courseEdition.CourseId,
                            CourseEditionId = courseEdition.CourseEditionId
                        }, out _))
                        {
                            continue;
                        }

                        if (courseEdition.LockUserId == null 
                            || courseEdition.LockUserId != userId
                            || courseEdition.LockUserConnectionId != connectionId)
                        {
                            continue;
                        }

                        courseEdition.LockUserId = null;
                        courseEdition.LockUserConnectionId = null;
                        unlockedCourseEditions.Add(courseEdition);
                    }

                    _unitOfWork.CourseEditions.GetAll().UpdateRange(unlockedCourseEditions);

                    _unitOfWork.Complete();

                    foreach (var unlockedCourseEdition in unlockedCourseEditions)
                    {
                        Clients.Others.UnlockCourseEdition(
                            unlockedCourseEdition.CourseId,
                            unlockedCourseEdition.CourseEditionId);
                    }

                    return new MessageObject { StatusCode = 200 };
                }
                finally
                {
                    RemoveCourseEditionsLocks(courseEditionQueues);
                    ExitQueues(courseEditionQueues.Values);
                }
            }
            catch (Exception e)
            {
                return new MessageObject { StatusCode = 400, Message = "Unexpected error. Please try again later." };
            }
        }

        [Authorize(Policy = "AdministratorOnly")]
        public MessageObject UnlockAllCourseEditionsForCourse(int courseId)
        {
            var courseEditionQueues = new SortedList<CourseEditionKey, ConcurrentQueue<object>>();

            try
            {
                var connectionId = Context.ConnectionId;
                var userId = GetUserId();

                var courseEditionKeys = _unitOfWork.CourseEditions.Get(e => e.CourseId == courseId).Select(e => new CourseEditionKey
                {
                    CourseId = e.CourseId,
                    CourseEditionId = e.CourseEditionId
                }).ToList();

                if (!courseEditionKeys.Any())
                {
                    return new MessageObject { StatusCode = 204, Message = "Could not find any course editions." };
                }

                lock (CourseEditionLocks)
                {
                    AddCourseEditionsLocks(courseEditionKeys, courseEditionQueues);
                }

                EnterQueues(courseEditionQueues.Values);
                try
                {
                    var courseEditions = _unitOfWork.CourseEditions.Get(e => e.CourseId == courseId)
                        .ToList();

                    if (!courseEditions.Any())
                    {
                        return new MessageObject { StatusCode = 204, Message = "Could not find any course editions." };
                    }

                    var unlockedCourseEditions = new List<CourseEdition>();
                    foreach (var courseEdition in courseEditions)
                    {
                        if (!courseEditionQueues.TryGetValue(new CourseEditionKey 
                        { 
                            CourseId = courseEdition.CourseId, 
                            CourseEditionId = courseEdition.CourseEditionId
                        }, out _))
                        {
                            continue;
                        }

                        if (courseEdition.LockUserId == null
                            || courseEdition.LockUserId != userId
                            || courseEdition.LockUserConnectionId != connectionId)
                        {
                            continue;
                        }

                        courseEdition.LockUserId = null;
                        courseEdition.LockUserConnectionId = null;
                        unlockedCourseEditions.Add(courseEdition);
                    }

                    _unitOfWork.CourseEditions.GetAll().UpdateRange(unlockedCourseEditions);

                    _unitOfWork.Complete();

                    foreach (var unlockedCourseEdition in unlockedCourseEditions)
                    {
                        Clients.Others.UnlockCourseEdition(
                            unlockedCourseEdition.CourseId,
                            unlockedCourseEdition.CourseEditionId);
                    }

                    return new MessageObject { StatusCode = 200 };
                }
                finally
                {
                    RemoveCourseEditionsLocks(courseEditionQueues);
                    ExitQueues(courseEditionQueues.Values);
                }
            }
            catch (Exception e)
            {
                return new MessageObject { StatusCode = 400, Message = "Unexpected error. Please try again later." };
            }
        }

        [Authorize(Policy = "AdministratorOnly")]
        public MessageObject UnlockAllCoordinatorCourses(int coordinatorId, int courseId, int courseEditionId)
        {
            var courseEditionQueues = new SortedList<CourseEditionKey, ConcurrentQueue<object>>();
            var schedulePositionQueuesL1 = new SortedList<SchedulePositionKey, ConcurrentQueue<object>>();
            var schedulePositionQueuesL2 = new SortedList<SchedulePositionKey, ConcurrentQueue<object>>();

            try
            {
                var connectionId = Context.ConnectionId;
                var userId = GetUserId();

                var courseEditionKeys = _unitOfWork.CoordinatorCourseEditions
                    .Get(e => e.CoordinatorId == coordinatorId).Select(e => new CourseEditionKey
                    {
                        CourseId = e.CourseId,
                        CourseEditionId = e.CourseEditionId
                    }).ToList();

                var key = new CourseEditionKey { CourseId = courseId, CourseEditionId = courseEditionId };
                if (!courseEditionKeys.Contains(key))
                {
                    courseEditionKeys.Add(key);
                }

                if (!courseEditionKeys.Any())
                {
                    return new MessageObject { StatusCode = 204, Message = "Could not find any course editions." };
                }

                lock (CourseEditionLocks)
                {
                    AddCourseEditionsLocks(courseEditionKeys, courseEditionQueues);
                }

                EnterQueues(courseEditionQueues.Values);
                try
                {
                    var currentCourseEdition = _unitOfWork.CourseEditions
                        .Get(e => e.CourseId == courseId && e.CourseEditionId == courseEditionId)
                        .FirstOrDefault();

                    var courseEditions = _unitOfWork.CoordinatorCourseEditions
                        .Get(e => e.CoordinatorId == coordinatorId)
                        .Include(e => e.CourseEdition)
                        .Select(e => e.CourseEdition)
                        .ToList();

                    if (currentCourseEdition != null)
                    {
                        courseEditions.Add(currentCourseEdition);
                    }

                    if (!courseEditions.Any())
                    {
                        return new MessageObject { StatusCode = 400, Message = "Could not find any course editions." };
                    }

                    var unlockedCourseEditions = new List<CourseEdition>();
                    foreach (var courseEdition in courseEditions)
                    {
                        if (!courseEditionQueues.TryGetValue(new CourseEditionKey 
                        { 
                            CourseId = courseEdition.CourseId, 
                            CourseEditionId = courseEdition.CourseEditionId
                        }, out _))
                        {
                            continue;
                        }

                        if (courseEdition.LockUserId == null
                            || courseEdition.LockUserId != userId
                            || courseEdition.LockUserConnectionId != connectionId)
                        {
                            continue;
                        }

                        courseEdition.LockUserId = null;
                        courseEdition.LockUserConnectionId = null;
                        unlockedCourseEditions.Add(courseEdition);
                    }

                    var courseEditionIds = courseEditionKeys.Select(e => e.CourseEditionId).ToList();
                    var schedulePositionKeys = _unitOfWork.SchedulePositions
                        .Get(e => courseEditionIds.Contains(e.CourseEditionId)).Select(e => new SchedulePositionKey
                        {
                            TimestampId = e.TimestampId,
                            RoomId = e.RoomId
                        }).ToList();

                    lock (SchedulePositionLocksL1)
                    lock (SchedulePositionLocksL2)
                    {
                        AddSchedulePositionsLocksL1(schedulePositionKeys, schedulePositionQueuesL1);
                        AddSchedulePositionsLocksL2(schedulePositionKeys, schedulePositionQueuesL2);
                    }

                    EnterQueues(schedulePositionQueuesL1.Values);
                    EnterQueues(schedulePositionQueuesL2.Values);
                    try
                    {
                        var schedulePositions = _unitOfWork.SchedulePositions
                            .Get(e => courseEditionIds.Contains(e.CourseEditionId))
                            .Include(e => e.Timestamp)
                            .ToList();

                        var unlockedSchedulePositions = new List<SchedulePosition>();
                        foreach (var schedulePosition in schedulePositions)
                        {
                            if (!schedulePositionQueuesL1.TryGetValue(new SchedulePositionKey 
                            { 
                                RoomId = schedulePosition.RoomId, 
                                TimestampId = schedulePosition.TimestampId
                            }, out _))
                            {
                                continue;
                            }

                            if (schedulePosition.LockUserId == null
                                || schedulePosition.LockUserId != userId
                                || schedulePosition.LockUserConnectionId != connectionId)
                            {
                                continue;
                            }

                            schedulePosition.LockUserId = null;
                            schedulePosition.LockUserConnectionId = null;
                            unlockedSchedulePositions.Add(schedulePosition);
                        }

                        _unitOfWork.CourseEditions.GetAll().UpdateRange(unlockedCourseEditions);
                        _unitOfWork.SchedulePositions.GetAll().UpdateRange(unlockedSchedulePositions);

                        _unitOfWork.Complete();

                        var positionsWeeks = unlockedSchedulePositions.GroupBy(e => new
                        {
                            e.CourseId,
                            e.CourseEditionId,
                            e.RoomId,
                            e.Timestamp.PeriodIndex,
                            e.Timestamp.Day
                        })
                            .Select(e => new { e.Key, Weeks = e.Select(f => f.Timestamp.Week).ToList() })
                            .ToList();

                        foreach (var courseEdition in unlockedCourseEditions)
                        {
                            Clients.Others.UnlockCourseEdition(
                                courseEdition.CourseId,
                                courseEdition.CourseEditionId);
                        }
                        
                        foreach (var position in positionsWeeks)
                        {
                            Clients.Others.UnlockSchedulePositions(
                                position.Key.CourseId, position.Key.CourseEditionId,
                                position.Key.RoomId, position.Key.PeriodIndex,
                                position.Key.Day, position.Weeks.ToArray());
                        }

                        return new MessageObject { StatusCode = 200 };
                    }
                    finally
                    {
                        RemoveSchedulePositionsLocksL2(schedulePositionQueuesL2);
                        RemoveSchedulePositionsLocksL1(schedulePositionQueuesL1);
                        ExitQueues(schedulePositionQueuesL2.Values);
                        ExitQueues(schedulePositionQueuesL1.Values);
                    }
                }
                finally
                {
                    RemoveCourseEditionsLocks(courseEditionQueues);
                    ExitQueues(courseEditionQueues.Values);
                }
            }
            catch (Exception e)
            {
                return new MessageObject { StatusCode = 400, Message = "Unexpected error. Please try again later." };
            }
        }

        [Authorize(Policy = "AdministratorOnly")]
        public MessageObject UnlockAllGroupCourses(int groupId, int courseId, int courseEditionId)
        {
            var courseEditionQueues = new SortedList<CourseEditionKey, ConcurrentQueue<object>>();
            var schedulePositionQueuesL1 = new SortedList<SchedulePositionKey, ConcurrentQueue<object>>();
            var schedulePositionQueuesL2 = new SortedList<SchedulePositionKey, ConcurrentQueue<object>>();

            try
            {
                var connectionId = Context.ConnectionId;
                var userId = GetUserId();

                var group = _unitOfWork.Groups.Get(e => e.GroupId == groupId).FirstOrDefault();

                if (group == null)
                {
                    return new MessageObject { StatusCode = 400, Message = "Could not find group." };
                }

                var groupsIds = Methods.GetNestedGroupsIds(new List<Group>() { group }, _unitOfWork.Groups);
                var courseEditionKeys = _unitOfWork.GroupCourseEditions
                    .Get(e => groupsIds.Contains(e.GroupId)).Select(e => new CourseEditionKey
                    {
                        CourseId = e.CourseId,
                        CourseEditionId = e.CourseEditionId
                    }).ToList();

                var key = new CourseEditionKey { CourseId = courseId, CourseEditionId = courseEditionId };
                if (!courseEditionKeys.Contains(key))
                {
                    courseEditionKeys.Add(key);
                }

                lock (CourseEditionLocks)
                {
                    AddCourseEditionsLocks(courseEditionKeys, courseEditionQueues);
                }

                EnterQueues(courseEditionQueues.Values);
                try
                {
                    var currentCourseEdition = _unitOfWork.CourseEditions
                        .Get(e => e.CourseId == courseId && e.CourseEditionId == courseEditionId)
                        .FirstOrDefault();

                    var currentGroupsIds = Methods.GetNestedGroupsIds(new List<Group>() { group }, _unitOfWork.Groups)
                        .Intersect(groupsIds);

                    if (!currentGroupsIds.Any())
                    {
                        return new MessageObject { StatusCode = 404, Message = "Could not find group. Please try again later." };
                    }

                    var courseEditions = _unitOfWork.GroupCourseEditions
                        .Get(e => currentGroupsIds.Contains(e.GroupId))
                        .Include(e => e.CourseEdition)
                        .Select(e => e.CourseEdition)
                        .ToList();

                    if (currentCourseEdition != null)
                    {
                        courseEditions.Add(currentCourseEdition);
                    }

                    if (!courseEditions.Any())
                    {
                        return new MessageObject { StatusCode = 400, Message = "Could not find any course editions." };
                    }

                    var unlockedCourseEditions = new List<CourseEdition>();
                    foreach (var courseEdition in courseEditions)
                    {
                        if (!courseEditionQueues.TryGetValue(new CourseEditionKey
                        {
                            CourseId = courseEdition.CourseId,
                            CourseEditionId = courseEdition.CourseEditionId
                        }, out _))
                        {
                            continue;
                        }

                        if (courseEdition.LockUserId == null
                            || courseEdition.LockUserId != userId
                            || courseEdition.LockUserConnectionId != connectionId)
                        {
                            continue;
                        }

                        courseEdition.LockUserId = null;
                        courseEdition.LockUserConnectionId = null;
                        unlockedCourseEditions.Add(courseEdition);
                    }

                    var courseEditionIds = courseEditionKeys.Select(e => e.CourseEditionId).ToList();
                    var schedulePositionKeys = _unitOfWork.SchedulePositions
                        .Get(e => courseEditionIds.Contains(e.CourseEditionId)).Select(e => new SchedulePositionKey
                        {
                            TimestampId = e.TimestampId,
                            RoomId = e.RoomId
                        }).ToList();

                    lock (SchedulePositionLocksL1)
                    lock (SchedulePositionLocksL2)
                    {
                        AddSchedulePositionsLocksL1(schedulePositionKeys, schedulePositionQueuesL1);
                        AddSchedulePositionsLocksL2(schedulePositionKeys, schedulePositionQueuesL2);
                    }

                    EnterQueues(schedulePositionQueuesL1.Values);
                    EnterQueues(schedulePositionQueuesL2.Values);
                    try
                    {
                        var schedulePositions = _unitOfWork.SchedulePositions
                            .Get(e => courseEditionIds.Contains(e.CourseEditionId))
                            .Include(e => e.Timestamp)
                            .ToList();

                        var unlockedSchedulePositions = new List<SchedulePosition>();
                        foreach (var schedulePosition in schedulePositions)
                        {
                            if (!schedulePositionQueuesL1.TryGetValue(new SchedulePositionKey
                            {
                                RoomId = schedulePosition.RoomId,
                                TimestampId = schedulePosition.TimestampId
                            }, out _))
                            {
                                continue;
                            }

                            if (schedulePosition.LockUserId == null
                                || schedulePosition.LockUserId != userId
                                || schedulePosition.LockUserConnectionId != connectionId)
                            {
                                continue;
                            }

                            schedulePosition.LockUserId = null;
                            schedulePosition.LockUserConnectionId = null;
                            unlockedSchedulePositions.Add(schedulePosition);
                        }

                        _unitOfWork.CourseEditions.GetAll().UpdateRange(unlockedCourseEditions);
                        _unitOfWork.SchedulePositions.GetAll().UpdateRange(unlockedSchedulePositions);

                        _unitOfWork.Complete();

                        var positionsWeeks = unlockedSchedulePositions.GroupBy(e => new
                        {
                            e.CourseId,
                            e.CourseEditionId,
                            e.RoomId,
                            e.Timestamp.PeriodIndex,
                            e.Timestamp.Day
                        })
                            .Select(e => new { e.Key, Weeks = e.Select(f => f.Timestamp.Week).ToList() })
                            .ToList();

                        foreach (var courseEdition in unlockedCourseEditions)
                        {
                            Clients.Others.UnlockCourseEdition(
                                courseEdition.CourseId,
                                courseEdition.CourseEditionId);
                        }

                        foreach (var position in positionsWeeks)
                        {
                            Clients.Others.UnlockSchedulePositions(
                                position.Key.CourseId, position.Key.CourseEditionId,
                                position.Key.RoomId, position.Key.PeriodIndex,
                                position.Key.Day, position.Weeks.ToArray());
                        }

                        return new MessageObject { StatusCode = 200 };
                    }
                    finally
                    {
                        RemoveSchedulePositionsLocksL2(schedulePositionQueuesL2);
                        RemoveSchedulePositionsLocksL1(schedulePositionQueuesL1);
                        ExitQueues(schedulePositionQueuesL2.Values);
                        ExitQueues(schedulePositionQueuesL1.Values);
                    }
                }
                finally
                {
                    RemoveCourseEditionsLocks(courseEditionQueues);
                    ExitQueues(courseEditionQueues.Values);
                }
            }
            catch (Exception e)
            {
                return new MessageObject { StatusCode = 400, Message = "Unexpected error. Please try again later." };
            }
        }

        [Authorize(Policy = "AdministratorOnly")]
        public MessageObject UnlockAllCoursesForGroupChange(int originGroupId, int? destinationGroupId)
        {
            var courseEditionQueues = new SortedList<CourseEditionKey, ConcurrentQueue<object>>();
            var schedulePositionQueuesL1 = new SortedList<SchedulePositionKey, ConcurrentQueue<object>>();
            var schedulePositionQueuesL2 = new SortedList<SchedulePositionKey, ConcurrentQueue<object>>();

            try
            {
                var connectionId = Context.ConnectionId;
                var userId = GetUserId();

                var originGroup = _unitOfWork.Groups.Get(e => e.GroupId == originGroupId).FirstOrDefault();
                if (originGroup == null)
                {
                    return new MessageObject { StatusCode = 400, Message = "Could not find origin group." };
                }
                
                Group destinationGroup = null;
                if (destinationGroupId != null)
                {
                    destinationGroup = _unitOfWork.Groups.Get(e => e.GroupId == destinationGroupId).FirstOrDefault();
                    if (destinationGroup == null)
                    {
                        return new MessageObject { StatusCode = 400, Message = "Could not find destination group." };
                    }
                }

                var childGroupsIds = Methods.GetChildGroups(new List<Group>() { originGroup }, _unitOfWork.Groups);
                var parentGroupsIds = destinationGroupId != null ? Methods.GetParentGroups(new List<Group>() { destinationGroup }, _unitOfWork.Groups) : new List<int>();

                if (destinationGroupId != null)
                {
                    if (childGroupsIds.Contains((int)destinationGroupId))
                    {
                        return new MessageObject { StatusCode = 400, Message = "Destination group cannot be the child of origin group." };
                    }
                }

                var groupsIds = childGroupsIds.Union(parentGroupsIds).ToList();
                var courseEditionKeys = _unitOfWork.GroupCourseEditions
                    .Get(e => groupsIds.Contains(e.GroupId))
                    .GroupBy(e => new { e.CourseId, e.CourseEditionId })
                    .Select(e => new CourseEditionKey
                    {
                        CourseId = e.Key.CourseId,
                        CourseEditionId = e.Key.CourseEditionId
                    }).ToList();

                lock (CourseEditionLocks)
                {
                    AddCourseEditionsLocks(courseEditionKeys, courseEditionQueues);
                }

                EnterQueues(courseEditionQueues.Values);
                try
                {
                    var currentGroupsIds = Methods.GetChildGroups(new List<Group>() { originGroup }, _unitOfWork.Groups)
                        .Union(destinationGroupId != null ? Methods.GetParentGroups(new List<Group>() { destinationGroup }, _unitOfWork.Groups) : new List<int>())
                        .Intersect(groupsIds);

                    if (!currentGroupsIds.Any())
                    {
                        return new MessageObject { StatusCode = 404, Message = "Could not find some of the groups. Please try again later." };
                    }

                    var courseEditions = _unitOfWork.GroupCourseEditions
                        .Get(e => currentGroupsIds.Contains(e.GroupId))
                        .Include(e => e.CourseEdition)
                        .Select(e => e.CourseEdition)
                        .ToList();

                    if (!courseEditions.Any())
                    {
                        return new MessageObject { StatusCode = 400, Message = "Could not find any course editions." };
                    }

                    var unlockedCourseEditions = new List<CourseEdition>();
                    foreach (var courseEdition in courseEditions)
                    {
                        if (!courseEditionQueues.TryGetValue(new CourseEditionKey
                        {
                            CourseId = courseEdition.CourseId,
                            CourseEditionId = courseEdition.CourseEditionId
                        }, out _))
                        {
                            continue;
                        }

                        if (courseEdition.LockUserId == null
                            || courseEdition.LockUserId != userId
                            || courseEdition.LockUserConnectionId != connectionId)
                        {
                            continue;
                        }

                        courseEdition.LockUserId = null;
                        courseEdition.LockUserConnectionId = null;
                        unlockedCourseEditions.Add(courseEdition);
                    }

                    var courseEditionIds = courseEditionKeys.Select(e => e.CourseEditionId).ToList();
                    var schedulePositionKeys = _unitOfWork.SchedulePositions
                        .Get(e => courseEditionIds.Contains(e.CourseEditionId)).Select(e => new SchedulePositionKey
                        {
                            TimestampId = e.TimestampId,
                            RoomId = e.RoomId
                        }).ToList();

                    lock (SchedulePositionLocksL1)
                    lock (SchedulePositionLocksL2)
                    {
                        AddSchedulePositionsLocksL1(schedulePositionKeys, schedulePositionQueuesL1);
                        AddSchedulePositionsLocksL2(schedulePositionKeys, schedulePositionQueuesL2);
                    }

                    EnterQueues(schedulePositionQueuesL1.Values);
                    EnterQueues(schedulePositionQueuesL2.Values);
                    try
                    {
                        var schedulePositions = _unitOfWork.SchedulePositions
                            .Get(e => courseEditionIds.Contains(e.CourseEditionId))
                            .Include(e => e.Timestamp)
                            .ToList();

                        var unlockedSchedulePositions = new List<SchedulePosition>();
                        foreach (var schedulePosition in schedulePositions)
                        {
                            if (!schedulePositionQueuesL1.TryGetValue(new SchedulePositionKey
                            {
                                RoomId = schedulePosition.RoomId,
                                TimestampId = schedulePosition.TimestampId
                            }, out _))
                            {
                                continue;
                            }

                            if (schedulePosition.LockUserId == null
                                || schedulePosition.LockUserId != userId
                                || schedulePosition.LockUserConnectionId != connectionId)
                            {
                                continue;
                            }

                            schedulePosition.LockUserId = null;
                            schedulePosition.LockUserConnectionId = null;
                            unlockedSchedulePositions.Add(schedulePosition);
                        }

                        _unitOfWork.CourseEditions.GetAll().UpdateRange(unlockedCourseEditions);
                        _unitOfWork.SchedulePositions.GetAll().UpdateRange(unlockedSchedulePositions);

                        _unitOfWork.Complete();

                        var positionsWeeks = unlockedSchedulePositions.GroupBy(e => new
                        {
                            e.CourseId,
                            e.CourseEditionId,
                            e.RoomId,
                            e.Timestamp.PeriodIndex,
                            e.Timestamp.Day
                        })
                            .Select(e => new { e.Key, Weeks = e.Select(f => f.Timestamp.Week).ToList() })
                            .ToList();

                        foreach (var courseEdition in unlockedCourseEditions)
                        {
                            Clients.Others.UnlockCourseEdition(
                                courseEdition.CourseId,
                                courseEdition.CourseEditionId);
                        }

                        foreach (var position in positionsWeeks)
                        {
                            Clients.Others.UnlockSchedulePositions(
                                position.Key.CourseId, position.Key.CourseEditionId,
                                position.Key.RoomId, position.Key.PeriodIndex,
                                position.Key.Day, position.Weeks.ToArray());
                        }

                        return new MessageObject { StatusCode = 200 };
                    }
                    finally
                    {
                        RemoveSchedulePositionsLocksL2(schedulePositionQueuesL2);
                        RemoveSchedulePositionsLocksL1(schedulePositionQueuesL1);
                        ExitQueues(schedulePositionQueuesL2.Values);
                        ExitQueues(schedulePositionQueuesL1.Values);
                    }
                }
                finally
                {
                    RemoveCourseEditionsLocks(courseEditionQueues);
                    ExitQueues(courseEditionQueues.Values);
                }
            }
            catch (Exception e)
            {
                return new MessageObject { StatusCode = 400, Message = "Unexpected error. Please try again later." };
            }
        }

        [Authorize(Policy = "Designer")]
        public void AddSchedulePositions(int courseId, int courseEditionId, int roomId, int periodIndex, int day, int[] weeks)
        {
            CourseEditionKey courseEditionKey = null;
            ConcurrentQueue<object> courseEditionQueue = null;
            var schedulePositionQueuesL1 = new SortedList<SchedulePositionKey, ConcurrentQueue<object>>();
            var schedulePositionQueuesL2 = new SortedList<SchedulePositionKey, ConcurrentQueue<object>>();
            var coordinatorPositionQueues = new SortedList<CoordinatorPositionKey, ConcurrentQueue<object>>();
            var groupPositionQueues = new SortedList<GroupPositionKey, ConcurrentQueue<object>>();

            weeks = weeks.Distinct().ToArray();
            Array.Sort(weeks);

            try
            {
                var userId = GetUserId();

                var _timestamps = _unitOfWork.Timestamps
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
                    AddSchedulePositionsLocksL1(_timestamps, roomId, schedulePositionQueuesL1);
                }

                Monitor.Enter(courseEditionQueue);
                EnterQueues(schedulePositionQueuesL1.Values);
                try
                {
                    var _courseEdition = _unitOfWork.CourseEditions
                        .Get(e => e.CourseId == courseId && e.CourseEditionId == courseEditionId)
                        .Include(e => e.Coordinators)
                        .Include(e => e.Groups)
                        .ThenInclude(e => e.Group)
                        .Include(e => e.SchedulePositions)
                        .Include(e => e.Course)
                        .ThenInclude(e => e.Rooms);

                    if (!_courseEdition.Any())
                    {
                        Clients.Caller.SendResponse(new MessageObject { StatusCode = 404, Message = "Could not find requested course edition." });
                        return;
                    }

                    var courseEdition = _courseEdition.FirstOrDefault();
                    if (courseEdition.LockUserId != userId || courseEdition.LockUserConnectionId != Context.ConnectionId)
                    {
                        Clients.Caller.SendResponse(new MessageObject { StatusCode = 400, Message = "You didn't lock this course edition." });
                        return;
                    }

                    var _settings = _unitOfWork.Settings.GetFirst(e => true).Result;
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
                    var groupsIds = Methods.GetNestedGroupsIds(courseEdition, _unitOfWork.Groups).ToArray();
                    var returnableGroupsIds = new int[groupsIds.Length];

                    Array.Sort(coordinatorsIds);
                    Array.Copy(groupsIds, returnableGroupsIds, groupsIds.Length);
                    Array.Sort(groupsIds);

                    lock (SchedulePositionLocksL2)
                    lock (CoordinatorPositionLocks)
                    lock (GroupPositionLocks)
                    {
                        AddSchedulePositionsLocksL2(schedulePositionQueuesL1.Keys.ToList(), schedulePositionQueuesL2);
                        foreach (var timestampId in _timestamps)
                        {
                            AddCoordinatorPositionsLocks(coordinatorsIds, timestampId, coordinatorPositionQueues);
                            AddGroupPositionsLocks(groupsIds, timestampId, groupPositionQueues);
                        }
                    }

                    EnterQueues(schedulePositionQueuesL2.Values);
                    EnterQueues(coordinatorPositionQueues.Values);
                    EnterQueues(groupPositionQueues.Values);
                    try
                    {
                        var _schedulePositions = _unitOfWork.SchedulePositions
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

                        var schedulePositions = _timestamps.Select(timestampId => new SchedulePosition
                        {
                            RoomId = roomId,
                            TimestampId = timestampId,
                            CourseId = courseEdition.CourseId,
                            CourseEditionId = courseEdition.CourseEditionId
                        }).ToList();

                        _unitOfWork.SchedulePositions.GetAll().AddRange(schedulePositions);

                        _unitOfWork.Complete();
                        Clients.Others.AddedSchedulePositions(
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
                        RemoveGroupPositionsLocks(groupPositionQueues);
                        RemoveCoordinatorPositionsLocks(coordinatorPositionQueues);
                        RemoveSchedulePositionsLocksL2(schedulePositionQueuesL2);
                        
                        ExitQueues(groupPositionQueues.Values);
                        ExitQueues(coordinatorPositionQueues.Values);
                        ExitQueues(schedulePositionQueuesL2.Values);
                    }
                }
                finally
                {
                    RemoveSchedulePositionsLocksL1(schedulePositionQueuesL1);
                    RemoveCourseEditionLock(courseEditionQueue, courseEditionKey);

                    ExitQueues(schedulePositionQueuesL1.Values);
                    Monitor.Exit(courseEditionQueue);
                }
            }
            catch (Exception e)
            {
                Clients.Caller.SendResponse(new MessageObject { StatusCode = 400, Message = "Unexpected error. Please try again later." });
                return;
            }
        }

        [Authorize(Policy = "Designer")]
        public void ModifySchedulePositions(
            int roomId, int periodIndex, int day, int[] weeks, 
            int destRoomId, int destPeriodIndex, int destDay, int[] destWeeks
        )
        {
            var schedulePositionKeys1 = new List<SchedulePositionKey>();
            var schedulePositionKeys2 = new List<SchedulePositionKey>();
            var L1schedulePositionAllQueues = new SortedList<SchedulePositionKey, ConcurrentQueue<object>>();
            var L2schedulePositionAllQueues = new SortedList<SchedulePositionKey, ConcurrentQueue<object>>();
            var coordinatorPositionQueues = new SortedList<CoordinatorPositionKey, ConcurrentQueue<object>>();
            var groupPositionQueues = new SortedList<GroupPositionKey, ConcurrentQueue<object>>();

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
                var userId = GetUserId();
                var isAdmin = IsAdmin();

                var _sourceTimestamps = _unitOfWork.Timestamps
                        .Get(e => e.PeriodIndex == periodIndex && e.Day == day && weeks.Contains(e.Week))
                        .Select(e => e.TimestampId)
                        .OrderBy(e => e).ToList();

                if (_sourceTimestamps.Count != weeks.Length)
                {
                    Clients.Caller.SendResponse(new MessageObject { StatusCode = 404, Message = "Could not find requested source time periods." });
                    return;
                }

                var _destTimestamps = _unitOfWork.Timestamps
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
                    AddSchedulePositionsLocksL1(_sourceTimestamps, roomId, schedulePositionKeys1, L1schedulePositionAllQueues);
                    AddSchedulePositionsLocksL1(_destTimestamps, destRoomId, schedulePositionKeys2, L1schedulePositionAllQueues);
                }
                var L1KeysToRemove = new List<SchedulePositionKey>();

                EnterQueues(L1schedulePositionAllQueues.Values);
                try
                {
                    lock (SchedulePositionLocksL2)
                    {
                        AddSchedulePositionsLocksL2(schedulePositionKeys1, L2schedulePositionAllQueues);
                        AddSchedulePositionsLocksL2(schedulePositionKeys2, L2schedulePositionAllQueues);
                    }

                    EnterQueues(L2schedulePositionAllQueues.Values);
                    try
                    {
                        var _sourceSchedulePositions = _unitOfWork.SchedulePositions
                            .Get(e => _sourceTimestamps.Contains(e.TimestampId) && e.RoomId == roomId);

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

                        var _courseEdition = _unitOfWork.CourseEditions
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

                        if (!isAdmin && !includableCourseEdition.Coordinators.Any(e => e.CoordinatorId == userId))
                        {
                            Clients.Caller.SendResponse(new MessageObject { StatusCode = 400, Message = "You do not have enough permissions to modify these positions." });
                            return;
                        }

                        if (!includableCourseEdition.Course.Rooms.Select(e => e.RoomId).Contains(destRoomId))
                        {
                            Clients.Caller.SendResponse(new MessageObject { StatusCode = 400, Message = "Chosen room does not exist or has not been assigned to chosen course." });
                            return;
                        }

                        var coordinatorsIds = includableCourseEdition.Coordinators.Select(e => e.CoordinatorId).ToArray();
                        var groupsIds = Methods.GetNestedGroupsIds(includableCourseEdition, _unitOfWork.Groups).ToArray();
                        var returnableGroupsIds = new int[groupsIds.Length];

                        Array.Sort(coordinatorsIds);
                        Array.Copy(groupsIds, returnableGroupsIds, groupsIds.Length);
                        Array.Sort(groupsIds);

                        lock (CoordinatorPositionLocks)
                        lock (GroupPositionLocks)
                        {
                            foreach (var timestampId in _destTimestamps)
                            {
                                AddCoordinatorPositionsLocks(coordinatorsIds, timestampId, coordinatorPositionQueues);
                                AddGroupPositionsLocks(groupsIds, timestampId, groupPositionQueues);
                            }
                        }

                        EnterQueues(coordinatorPositionQueues.Values);
                        EnterQueues(groupPositionQueues.Values);
                        try
                        {
                            var _destSchedulePositions = _sourceTimestamps.SequenceEqual(_destTimestamps)
                                ?
                                _unitOfWork.SchedulePositions
                                        .Get(e => _destTimestamps.Contains(e.TimestampId) && e.RoomId == destRoomId)
                                        .Include(e => e.CourseEdition)
                                        .ThenInclude(e => e.Coordinators)
                                        .Include(e => e.CourseEdition)
                                        .ThenInclude(e => e.Groups)
                                        .Select(e => new { e.TimestampId, e.RoomId })
                                :
                                _unitOfWork.SchedulePositions
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

                            var destSchedulePositions = _destTimestamps.Select(timestampId => new SchedulePosition
                            {
                                RoomId = destRoomId,
                                TimestampId = timestampId,
                                CourseId = includableCourseEdition.CourseId,
                                CourseEditionId = includableCourseEdition.CourseEditionId
                            }).ToList();

                            var movesIds = _unitOfWork.ScheduledMovePositions
                                .Get(e => e.RoomId_1 == roomId && _sourceTimestamps.Contains(e.TimestampId_1)
                                    && e.CourseId == includableCourseEdition.CourseId)
                                .GroupBy(e => e.MoveId)
                                .Select(e => e.Key)
                                .OrderBy(e => e).ToList();

                            _unitOfWork.ScheduledMovePositions.DeleteMany(e => movesIds.Contains(e.MoveId));
                            _unitOfWork.ScheduledMoves.DeleteMany(e => movesIds.Contains(e.MoveId));
                            _unitOfWork.SchedulePositions.GetAll().RemoveRange(_sourceSchedulePositions);
                            _unitOfWork.SchedulePositions.GetAll().AddRange(destSchedulePositions);

                            _unitOfWork.Complete();
                            
                            Clients.Others.ModifiedSchedulePositions(
                                includableCourseEdition.CourseId, includableCourseEdition.CourseEditionId,
                                returnableGroupsIds, includableCourseEdition.Groups.Count, coordinatorsIds,
                                roomId, destRoomId,
                                periodIndex, destPeriodIndex,
                                day, destDay,
                                weeks, destWeeks,
                                movesIds.ToArray()
                            );

                            Clients.Caller.SendResponse(new MessageObject { StatusCode = 200 });
                        }
                        finally
                        {
                            RemoveCoordinatorPositionsLocks(coordinatorPositionQueues);
                            RemoveGroupPositionsLocks(groupPositionQueues);
                            ExitQueues(groupPositionQueues.Values);
                            ExitQueues(coordinatorPositionQueues.Values);
                        }
                    }
                    finally
                    {
                        RemoveSchedulePositionsLocksL2(L2schedulePositionAllQueues);
                        ExitQueues(L2schedulePositionAllQueues.Values);
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

                    MakeScheduledMoves(schedulePositionKeys1, L1schedulePositionAllQueues, L1KeysToRemove);
                    return;
                }
                finally
                {
                    foreach (var keyToRemove in L1KeysToRemove)
                    {
                        L1schedulePositionAllQueues.Remove(keyToRemove);
                    }

                    RemoveSchedulePositionsLocksL1(L1schedulePositionAllQueues);
                    ExitQueues(L1schedulePositionAllQueues.Values);
                }
            }
            catch (Exception e)
            {
                Clients.Caller.SendResponse(new MessageObject { StatusCode = 400, Message = "Unexpected error. Please try again later." });
                return;
            }
        }

        [Authorize(Policy = "Designer")]
        public void RemoveSchedulePositions(int roomId, int periodIndex, int day, int[] weeks)
        {
            var schedulePositionKeys = new List<SchedulePositionKey>();
            var L1schedulePositionQueues = new SortedList<SchedulePositionKey, ConcurrentQueue<object>>();
            var L2schedulePositionQueues = new SortedList<SchedulePositionKey, ConcurrentQueue<object>>();

            weeks = weeks.Distinct().ToArray();
            Array.Sort(weeks);

            try
            {
                var userId = GetUserId();
                var isAdmin = IsAdmin();

                var _timestamps = _unitOfWork.Timestamps
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
                    AddSchedulePositionsLocksL1(_timestamps, roomId, schedulePositionKeys, L1schedulePositionQueues);
                }
                var L1KeysToRemove = new List<SchedulePositionKey>();

                EnterQueues(L1schedulePositionQueues.Values);
                try
                {
                    lock (SchedulePositionLocksL2)
                    {
                        AddSchedulePositionsLocksL2(schedulePositionKeys, L2schedulePositionQueues);
                    }

                    EnterQueues(L2schedulePositionQueues.Values);
                    try
                    {
                        var _schedulePositions = _unitOfWork.SchedulePositions
                            .Get(e => _timestamps.Contains(e.TimestampId) && e.RoomId == roomId);

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

                        var _courseEdition = _unitOfWork.CourseEditions
                            .Get(e => e.CourseId == schedulePosition.CourseId &&
                                      e.CourseEditionId == schedulePosition.CourseEditionId)
                            .Include(e => e.Groups)
                                .ThenInclude(e => e.Group)
                            .Include(e => e.Coordinators);

                        var courseEdition = _courseEdition.FirstOrDefault();
                        if (courseEdition == null)
                        {
                            Clients.Caller.SendResponse(new MessageObject { StatusCode = 400, Message = "Could not find course edition for requested positions in schedule." });
                            return;
                        }

                        if (!isAdmin && !courseEdition.Coordinators.Any(e => e.CoordinatorId == userId))
                        {
                            Clients.Caller.SendResponse(new MessageObject { StatusCode = 400, Message = "You do not have enough permissions to remove these positions." });
                            return;
                        }

                        var coordinatorsIds = courseEdition.Coordinators.Select(e => e.CoordinatorId).ToArray();
                        var groupsIds = Methods.GetNestedGroupsIds(courseEdition, _unitOfWork.Groups).ToArray();
                        var returnableGroupsIds = new int[groupsIds.Length];

                        Array.Sort(coordinatorsIds);
                        Array.Copy(groupsIds, returnableGroupsIds, groupsIds.Length);
                        Array.Sort(groupsIds);

                        var movesIds = _unitOfWork.ScheduledMovePositions
                                .Get(e => e.RoomId_1 == roomId && _timestamps.Contains(e.TimestampId_1)
                                    && e.CourseId == schedulePosition.CourseId)
                                .GroupBy(e => e.MoveId)
                                .Select(e => e.Key)
                                .OrderBy(e => e).ToList();

                        _unitOfWork.ScheduledMovePositions.DeleteMany(e => movesIds.Contains(e.MoveId));
                        _unitOfWork.ScheduledMoves.DeleteMany(e => movesIds.Contains(e.MoveId));
                        _unitOfWork.SchedulePositions.GetAll().RemoveRange(_schedulePositions);

                        _unitOfWork.Complete();

                        Clients.Others.RemovedSchedulePositions(
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
                        ExitQueues(L2schedulePositionQueues.Values);
                    }

                    MakeScheduledMoves(schedulePositionKeys, L1schedulePositionQueues, L1KeysToRemove);
                    return;
                }
                finally
                {
                    foreach (var keyToRemove in L1KeysToRemove)
                    {
                        L1schedulePositionQueues.Remove(keyToRemove);
                    }

                    RemoveSchedulePositionsLocksL1(L1schedulePositionQueues);
                    ExitQueues(L1schedulePositionQueues.Values);
                }
            }
            catch (Exception e)
            {
                Clients.Caller.SendResponse(new MessageObject { StatusCode = 400, Message = "Unexpected error. Please try again later." });
                return;
            }
        }


        [Authorize(Policy = "Assistant")]
        public MessageObject AddScheduledMove(
            int roomId, int periodIndex, int day, int[] weeks, 
            int destRoomId, int destPeriodIndex, int destDay, int[] destWeeks, bool isProposition, string message)
        {
            var L1schedulePositionAllQueues = new SortedList<SchedulePositionKey, ConcurrentQueue<object>>();
            var L2schedulePositionAllQueues = new SortedList<SchedulePositionKey, ConcurrentQueue<object>>();
            var coordinatorPositionQueues = new SortedList<CoordinatorPositionKey, ConcurrentQueue<object>>();
            var groupPositionQueues = new SortedList<GroupPositionKey, ConcurrentQueue<object>>();

            weeks = weeks.Distinct().ToArray();
            Array.Sort(weeks);
            destWeeks = destWeeks.Distinct().ToArray();
            Array.Sort(destWeeks);

            if (weeks.Length != destWeeks.Length)
            {
                return new MessageObject { StatusCode = 400, Message = "Amount of weeks must be equal." };
            }

            if (isProposition && message?.Length > 300)
            {
                return new MessageObject { StatusCode = 400, Message = "Message has too many characters." };
            }

            try
            {
                var userId = GetUserId();
                var isAdmin = IsAdmin();
                var isCoordinator = IsCoordinator();
                var representativeGroupsIds = GetRepresentativeGroupsIds();

                if (!isAdmin && !isCoordinator && !isProposition)
                {
                    return new MessageObject { StatusCode = 400, Message = "You do not have enough permissions to create scheduled moves." };
                }


                var _sourceTimestamps = _unitOfWork.Timestamps
                        .Get(e => e.PeriodIndex == periodIndex && e.Day == day && weeks.Contains(e.Week))
                        .Select(e => e.TimestampId)
                        .OrderBy(e => e).ToList();

                if (_sourceTimestamps.Count != weeks.Length)
                {
                    return new MessageObject { StatusCode = 404, Message = "Could not find requested source time periods." };
                }

                var _destTimestamps = _unitOfWork.Timestamps
                            .Get(e => e.PeriodIndex == destPeriodIndex && e.Day == destDay && destWeeks.Contains(e.Week))
                            .Select(e => e.TimestampId)
                            .OrderBy(e => e).ToList();

                if (_destTimestamps.Count != destWeeks.Length)
                {
                    return new MessageObject { StatusCode = 404, Message = "Could not find requested destination time periods." };
                }

                lock (SchedulePositionLocksL1)
                {
                    AddSchedulePositionsLocksL1(_sourceTimestamps, roomId, L1schedulePositionAllQueues);
                    AddSchedulePositionsLocksL1(_destTimestamps, destRoomId, L1schedulePositionAllQueues);
                }

                EnterQueues(L1schedulePositionAllQueues.Values);
                try
                {
                    lock (SchedulePositionLocksL2)
                    {
                        AddSchedulePositionsLocksL2(L1schedulePositionAllQueues.Keys.ToList(), L2schedulePositionAllQueues);
                    }

                    EnterQueues(L2schedulePositionAllQueues.Values);
                    try
                    {
                        var _sourceSchedulePositions = _unitOfWork.SchedulePositions
                            .Get(e => _sourceTimestamps.Contains(e.TimestampId) && e.RoomId == roomId)
                            .Include(e => e.CourseEdition);

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

                        var _courseEdition = _unitOfWork.CourseEditions
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
                        var groupsIds = Methods.GetNestedGroupsIds(includableCourseEdition, _unitOfWork.Groups).ToArray();
                        var returnableGroupsIds = new int[groupsIds.Length];

                        Array.Sort(coordinatorsIds);
                        Array.Copy(groupsIds, returnableGroupsIds, groupsIds.Length);
                        Array.Sort(groupsIds);

                        lock (CoordinatorPositionLocks)
                        lock (GroupPositionLocks)
                        {
                            foreach (var timestampId in _destTimestamps)
                            {
                                AddCoordinatorPositionsLocks(coordinatorsIds, timestampId, coordinatorPositionQueues);
                                AddGroupPositionsLocks(groupsIds, timestampId, groupPositionQueues);
                            }
                        }

                        EnterQueues(coordinatorPositionQueues.Values);
                        EnterQueues(groupPositionQueues.Values);
                        try
                        {
                            var _destSchedulePositions = _sourceTimestamps.SequenceEqual(_destTimestamps)
                                ?
                                _unitOfWork.SchedulePositions
                                        .Get(e => _destTimestamps.Contains(e.TimestampId) && e.RoomId == destRoomId)
                                        .Include(e => e.CourseEdition)
                                        .ThenInclude(e => e.Coordinators)
                                        .Include(e => e.CourseEdition)
                                        .ThenInclude(e => e.Groups)
                                :
                                _unitOfWork.SchedulePositions
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

                            if (!isProposition && !_destSchedulePositions.Any())
                            {
                                return new MessageObject { StatusCode = 400, Message = "This move is possible, so you should not try to schedule it." };
                            }

                            var _scheduledMovesCountsCondition = _unitOfWork.ScheduledMovePositions
                                .Get(e => e.RoomId_1 == roomId && _sourceTimestamps.Contains(e.TimestampId_1)
                                    && e.RoomId_2 == destRoomId && _destTimestamps.Contains(e.TimestampId_2)
                                    && e.CourseId == courseEdition.CourseId)
                                .GroupBy(e => e.MoveId)
                                .Select(e => new { MoveId = e.Key, Count = e.Count() })
                                .OrderBy(e => e.MoveId).ToList();

                            var _sourceTimestampsCount = _sourceTimestamps.Count();
                            _scheduledMovesCountsCondition = _scheduledMovesCountsCondition
                                .Where(e => e.Count == _sourceTimestampsCount).ToList();

                            var _scheduledMovesCounts = _unitOfWork.ScheduledMovePositions
                                .Get(e => _scheduledMovesCountsCondition.Select(e => e.MoveId).Contains(e.MoveId))
                                .GroupBy(e => e.MoveId)
                                .Select(e => new { MoveId = e.Key, Count = e.Count() })
                                .OrderBy(e => e.MoveId).ToList();

                            var _scheduledMovesCount = _scheduledMovesCountsCondition.Count();
                            for (var i = 0; i < _scheduledMovesCount; ++i)
                            {
                                if (_scheduledMovesCountsCondition[i].Count == _scheduledMovesCounts[i].Count)
                                {
                                    return new MessageObject { StatusCode = 400, Message = "This move already exists." };
                                }
                            }

                            var destTimestampsCount = _destTimestamps.Count;
                            var scheduledMove = new ScheduledMove
                            {
                                UserId = userId,
                                IsConfirmed = !isProposition,
                                ScheduleOrder = DateTime.Now,
                                Message = message != null ? new Message { Content = message} : null,
                                ScheduledPositions = new List<ScheduledMovePosition>(destTimestampsCount)
                            };
                            for (var i = 0; i < destTimestampsCount; ++i)
                            {
                                var srcTimestamp = _sourceTimestamps[i];
                                var destTimestamp = _destTimestamps[i];
                                scheduledMove.ScheduledPositions.Add(new ScheduledMovePosition
                                {
                                    RoomId_1 = roomId,
                                    TimestampId_1 = srcTimestamp,
                                    RoomId_2 = destRoomId,
                                    TimestampId_2 = destTimestamp,
                                    CourseId = courseEdition.CourseId
                                });
                            }

                            _unitOfWork.ScheduledMoves.GetAll().AddRange(scheduledMove);

                            _unitOfWork.Complete();
                            Clients.All.AddedScheduledMove(
                                scheduledMove.MoveId, userId, !isProposition,
                                courseEdition.CourseId, courseEdition.CourseEditionId,
                                roomId, periodIndex,
                                day, weeks
                            );

                            return new MessageObject { StatusCode = 200 };
                        }
                        finally
                        {
                            RemoveGroupPositionsLocks(groupPositionQueues);
                            RemoveCoordinatorPositionsLocks(coordinatorPositionQueues);
                            ExitQueues(groupPositionQueues.Values);
                            ExitQueues(coordinatorPositionQueues.Values);
                        }
                    }
                    finally
                    {
                        RemoveSchedulePositionsLocksL2(L2schedulePositionAllQueues);
                        ExitQueues(L2schedulePositionAllQueues.Values);
                    }
                }
                finally 
                {
                    RemoveSchedulePositionsLocksL1(L1schedulePositionAllQueues);
                    ExitQueues(L1schedulePositionAllQueues.Values);
                }
            }
            catch (Exception e)
            {
                return new MessageObject { StatusCode = 400, Message = "Unexpected error. Please try again later." };
            }
        }

        [Authorize(Policy = "Assistant")]
        public MessageObject RemoveScheduledMove(
            int roomId, int periodIndex, int day, int[] weeks, 
            int destRoomId, int destPeriodIndex, int destDay, int[] destWeeks)
        {
            var L1schedulePositionAllQueues = new SortedList<SchedulePositionKey, ConcurrentQueue<object>>();
            var L2schedulePositionAllQueues = new SortedList<SchedulePositionKey, ConcurrentQueue<object>>();
            var coordinatorPositionQueues = new SortedList<CoordinatorPositionKey, ConcurrentQueue<object>>();
            var groupPositionQueues = new SortedList<GroupPositionKey, ConcurrentQueue<object>>();

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
                var userId = GetUserId();
                var isAdmin = IsAdmin();
                var isCoordinator = IsCoordinator();
                var representativeGroupsIds = GetRepresentativeGroupsIds();

                var _sourceTimestamps = _unitOfWork.Timestamps
                        .Get(e => e.PeriodIndex == periodIndex && e.Day == day && weeks.Contains(e.Week))
                        .Select(e => e.TimestampId)
                        .OrderBy(e => e).ToList();

                if (_sourceTimestamps.Count != weeks.Length)
                {
                    return new MessageObject { StatusCode = 404, Message = "Could not find requested source time periods." };
                }

                var _destTimestamps = _unitOfWork.Timestamps
                            .Get(e => e.PeriodIndex == destPeriodIndex && e.Day == destDay && destWeeks.Contains(e.Week))
                            .Select(e => e.TimestampId)
                            .OrderBy(e => e).ToList();

                if (_destTimestamps.Count != destWeeks.Length)
                {
                    return new MessageObject { StatusCode = 404, Message = "Could not find requested destination time periods." };
                }

                lock (SchedulePositionLocksL1)
                {
                    AddSchedulePositionsLocksL1(_sourceTimestamps, roomId, L1schedulePositionAllQueues);
                    AddSchedulePositionsLocksL1(_destTimestamps, destRoomId, L1schedulePositionAllQueues);
                }

                EnterQueues(L1schedulePositionAllQueues.Values);
                try
                {
                    lock (SchedulePositionLocksL2)
                    {
                        AddSchedulePositionsLocksL2(L1schedulePositionAllQueues.Keys.ToList(), L2schedulePositionAllQueues);
                    }

                    EnterQueues(L2schedulePositionAllQueues.Values);
                    try
                    {
                        var predicate = PredicateBuilder.New<SchedulePosition>(true);
                        if (!isAdmin && !isCoordinator)
                        {
                            predicate = predicate
                                .And(e => e.CourseEdition.Groups.Any(f => representativeGroupsIds.Contains(f.GroupId)));
                        }

                        var finalPredicate = PredicateBuilder.New<SchedulePosition>(true)
                            .And(e => _sourceTimestamps.Contains(e.TimestampId) && e.RoomId == roomId)
                            .And(predicate);

                        var _sourceSchedulePositions = _unitOfWork.SchedulePositions
                            .Get(finalPredicate)
                            .Include(e => e.CourseEdition)
                                .ThenInclude(e => e.Coordinators)
                            .Include(e => e.CourseEdition)
                                .ThenInclude(e => e.Groups);

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

                        var _courseEdition = _unitOfWork.CourseEditions
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
                        var groupsIds = Methods.GetNestedGroupsIds(includableCourseEdition, _unitOfWork.Groups).ToArray();
                        var returnableGroupsIds = new int[groupsIds.Length];

                        Array.Sort(coordinatorsIds);
                        Array.Copy(groupsIds, returnableGroupsIds, groupsIds.Length);
                        Array.Sort(groupsIds);

                        lock (CoordinatorPositionLocks)
                        lock (GroupPositionLocks)
                        {
                            foreach (var timestampId in _destTimestamps)
                            {
                                AddCoordinatorPositionsLocks(coordinatorsIds, timestampId, coordinatorPositionQueues);
                                AddGroupPositionsLocks(groupsIds, timestampId, groupPositionQueues);
                            }
                        }

                        EnterQueues(coordinatorPositionQueues.Values);
                        EnterQueues(groupPositionQueues.Values);
                        try
                        {
                            var _scheduledMovesCountsCondition = _unitOfWork.ScheduledMovePositions
                                .Get(e => e.RoomId_1 == roomId && _sourceTimestamps.Contains(e.TimestampId_1)
                                    && e.RoomId_2 == destRoomId && _destTimestamps.Contains(e.TimestampId_2)
                                    && e.CourseId == courseEdition.CourseId)
                                .Include(e => e.ScheduledMove)
                                .GroupBy(e => new { e.MoveId, e.ScheduledMove.UserId, e.ScheduledMove.IsConfirmed })
                                .Select(e => new { e.Key.MoveId, e.Key.UserId, e.Key.IsConfirmed, Count = e.Count() })
                                .OrderBy(e => e.MoveId).ToList();

                            var _sourceTimestampsCount = _sourceTimestamps.Count();
                            _scheduledMovesCountsCondition = _scheduledMovesCountsCondition
                                .Where(e => e.Count == _sourceTimestampsCount).ToList();

                            var _scheduledMovesCounts = _unitOfWork.ScheduledMovePositions
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
                                    if (isAdmin || (isCoordinator && includableCourseEdition.Coordinators.Any(e => e.CoordinatorId == userId)) 
                                        || (_scheduledMovesCountsCondition[i].UserId == userId && !_scheduledMovesCountsCondition[i].IsConfirmed))
                                    {
                                        _unitOfWork.ScheduledMovePositions
                                            .DeleteMany(e => e.MoveId == _scheduledMovesCountsCondition[i].MoveId);
                                        _unitOfWork.ScheduledMoves
                                            .DeleteMany(e => e.MoveId == _scheduledMovesCountsCondition[i].MoveId);

                                        moveId = _scheduledMovesCountsCondition[i].MoveId;
                                        isFound = true;
                                        break;
                                    }
                                }
                            }

                            if (!isFound)
                            {
                                return new MessageObject { StatusCode = 400, Message = "Could not find scheduled move or you cannot remove it." };
                            }

                            _unitOfWork.Complete();
                            Clients.All.RemovedScheduledMove(
                                moveId,
                                courseEdition.CourseId, courseEdition.CourseEditionId,
                                roomId, periodIndex,
                                day, weeks
                            );

                            return new MessageObject { StatusCode = 200 };
                        }
                        finally
                        {
                            RemoveGroupPositionsLocks(groupPositionQueues);
                            RemoveCoordinatorPositionsLocks(coordinatorPositionQueues);
                            ExitQueues(groupPositionQueues.Values);
                            ExitQueues(coordinatorPositionQueues.Values);
                        }
                    }
                    finally
                    {
                        RemoveSchedulePositionsLocksL2(L2schedulePositionAllQueues);
                        ExitQueues(L2schedulePositionAllQueues.Values);
                    }
                }
                finally
                {
                    RemoveSchedulePositionsLocksL1(L1schedulePositionAllQueues);
                    ExitQueues(L1schedulePositionAllQueues.Values);
                }
            }
            catch (Exception e)
            {
                return new MessageObject { StatusCode = 400, Message = "Unexpected error. Please try again later." };
            }
        }

        [Authorize(Policy = "Designer")]
        public MessageObject AcceptProposition(
            int roomId, int periodIndex, int day, int[] weeks,
            int destRoomId, int destPeriodIndex, int destDay, int[] destWeeks)
        {
            var L1schedulePositionAllQueues = new SortedList<SchedulePositionKey, ConcurrentQueue<object>>();
            var L2schedulePositionAllQueues = new SortedList<SchedulePositionKey, ConcurrentQueue<object>>();
            var coordinatorPositionQueues = new SortedList<CoordinatorPositionKey, ConcurrentQueue<object>>();
            var groupPositionQueues = new SortedList<GroupPositionKey, ConcurrentQueue<object>>();

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
                var userId = GetUserId();
                var isAdmin = IsAdmin();

                var _sourceTimestamps = _unitOfWork.Timestamps
                        .Get(e => e.PeriodIndex == periodIndex && e.Day == day && weeks.Contains(e.Week))
                        .Select(e => e.TimestampId)
                        .OrderBy(e => e).ToList();

                if (_sourceTimestamps.Count != weeks.Length)
                {
                    return new MessageObject { StatusCode = 404, Message = "Could not find requested source time periods." };
                }

                var _destTimestamps = _unitOfWork.Timestamps
                            .Get(e => e.PeriodIndex == destPeriodIndex && e.Day == destDay && destWeeks.Contains(e.Week))
                            .Select(e => e.TimestampId)
                            .OrderBy(e => e).ToList();

                if (_destTimestamps.Count != destWeeks.Length)
                {
                    return new MessageObject { StatusCode = 404, Message = "Could not find requested destination time periods." };
                }

                lock (SchedulePositionLocksL1)
                {
                    AddSchedulePositionsLocksL1(_sourceTimestamps, roomId, L1schedulePositionAllQueues);
                    AddSchedulePositionsLocksL1(_destTimestamps, destRoomId, L1schedulePositionAllQueues);
                }

                EnterQueues(L1schedulePositionAllQueues.Values);
                try
                {
                    lock (SchedulePositionLocksL2)
                    {
                        AddSchedulePositionsLocksL2(L1schedulePositionAllQueues.Keys.ToList(), L2schedulePositionAllQueues);
                    }

                    EnterQueues(L2schedulePositionAllQueues.Values);
                    try
                    {
                        var _sourceSchedulePositions = _unitOfWork.SchedulePositions
                        .Get(e => _sourceTimestamps.Contains(e.TimestampId) && e.RoomId == roomId)
                            .Include(e => e.CourseEdition);

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

                        var _courseEdition = _unitOfWork.CourseEditions
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

                        if (!isAdmin && !courseEdition.Coordinators.Any(e => e.CoordinatorId == userId))
                        {
                            return new MessageObject { StatusCode = 400, Message = "You do not have enough permissions to accept this proposition." };
                        }

                        var coordinatorsIds = includableCourseEdition.Coordinators.Select(e => e.CoordinatorId).ToArray();
                        var groupsIds = Methods.GetNestedGroupsIds(includableCourseEdition, _unitOfWork.Groups).ToArray();
                        var returnableGroupsIds = new int[groupsIds.Length];

                        Array.Sort(coordinatorsIds);
                        Array.Copy(groupsIds, returnableGroupsIds, groupsIds.Length);
                        Array.Sort(groupsIds);

                        lock (CoordinatorPositionLocks)
                        lock (GroupPositionLocks)
                        {
                            foreach (var timestampId in _destTimestamps)
                            {
                                AddCoordinatorPositionsLocks(coordinatorsIds, timestampId, coordinatorPositionQueues);
                                AddGroupPositionsLocks(groupsIds, timestampId, groupPositionQueues);
                            }
                        }

                        EnterQueues(coordinatorPositionQueues.Values);
                        EnterQueues(groupPositionQueues.Values);
                        try
                        {
                            var _scheduledMovesCountsCondition = _unitOfWork.ScheduledMovePositions
                                .Get(e => e.RoomId_1 == roomId && _sourceTimestamps.Contains(e.TimestampId_1)
                                    && e.RoomId_2 == destRoomId && _destTimestamps.Contains(e.TimestampId_2)
                                    && e.CourseId == courseEdition.CourseId)
                                .GroupBy(e => e.MoveId)
                                .Select(e => new { MoveId = e.Key, Count = e.Count() })
                                .OrderBy(e => e.MoveId).ToList();

                            var _sourceTimestampsCount = _sourceTimestamps.Count();
                            _scheduledMovesCountsCondition = _scheduledMovesCountsCondition
                                .Where(e => e.Count == _sourceTimestampsCount).ToList();

                            var _scheduledMovesCounts = _unitOfWork.ScheduledMovePositions
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
                                    moveId = _scheduledMovesCountsCondition[i].MoveId;
                                    isFound = true;
                                    break;
                                }
                            }

                            if (!isFound)
                            {
                                return new MessageObject { StatusCode = 400, Message = "Could not find scheduled move." };
                            }

                            var _destSchedulePositions = _sourceTimestamps.SequenceEqual(_destTimestamps)
                                ?
                                _unitOfWork.SchedulePositions
                                        .Get(e => _destTimestamps.Contains(e.TimestampId) && e.RoomId == destRoomId)
                                        .Include(e => e.CourseEdition)
                                        .ThenInclude(e => e.Coordinators)
                                        .Include(e => e.CourseEdition)
                                        .ThenInclude(e => e.Groups)
                                        .Select(e => new { e.TimestampId, e.RoomId })
                                :
                                _unitOfWork.SchedulePositions
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
                                var scheduledMove = _unitOfWork.ScheduledMoves
                                    .Get(e => e.MoveId == moveId)
                                    .Include(e => e.Message).FirstOrDefault();

                                if (scheduledMove == null)
                                {
                                    return new MessageObject { StatusCode = 400, Message = "Could not find scheduled move." };
                                }

                                scheduledMove.IsConfirmed = true;
                                _unitOfWork.ScheduledMoves.Update(scheduledMove);
                                _unitOfWork.Messages.Delete(e => e.MoveId == moveId);

                                _unitOfWork.Complete();
                                Clients.All.AcceptedScheduledMove(
                                    moveId,
                                    courseEdition.CourseId, courseEdition.CourseEditionId,
                                    roomId, periodIndex,
                                    day, weeks
                                );
                            } 
                            else
                            {
                                var destSchedulePositions = _destTimestamps.Select(timestampId => new SchedulePosition
                                {
                                    RoomId = destRoomId,
                                    TimestampId = timestampId,
                                    CourseId = includableCourseEdition.CourseId,
                                    CourseEditionId = includableCourseEdition.CourseEditionId
                                }).ToList();

                                var movesIds = _unitOfWork.ScheduledMovePositions
                                    .Get(e => e.RoomId_1 == roomId && _sourceTimestamps.Contains(e.TimestampId_1)
                                        && e.CourseId == includableCourseEdition.CourseId)
                                    .GroupBy(e => e.MoveId)
                                    .Select(e => e.Key)
                                    .OrderBy(e => e).ToList();

                                _unitOfWork.ScheduledMovePositions.DeleteMany(e => movesIds.Contains(e.MoveId));
                                _unitOfWork.ScheduledMoves.DeleteMany(e => movesIds.Contains(e.MoveId));
                                _unitOfWork.SchedulePositions.GetAll().RemoveRange(_sourceSchedulePositions);
                                _unitOfWork.SchedulePositions.GetAll().AddRange(destSchedulePositions);

                                _unitOfWork.Complete();

                                Clients.All.ModifiedSchedulePositions(
                                    includableCourseEdition.CourseId, includableCourseEdition.CourseEditionId,
                                    returnableGroupsIds, includableCourseEdition.Groups.Count, coordinatorsIds,
                                    roomId, destRoomId,
                                    periodIndex, destPeriodIndex,
                                    day, destDay,
                                    weeks, destWeeks,
                                    movesIds.ToArray()
                                );
                            }

                            return new MessageObject { StatusCode = 200 };
                        }
                        finally
                        {
                            RemoveGroupPositionsLocks(groupPositionQueues);
                            RemoveCoordinatorPositionsLocks(coordinatorPositionQueues);
                            ExitQueues(groupPositionQueues.Values);
                            ExitQueues(coordinatorPositionQueues.Values);
                        }
                    }
                    finally
                    {
                        RemoveSchedulePositionsLocksL2(L2schedulePositionAllQueues);
                        ExitQueues(L2schedulePositionAllQueues.Values);
                    }
                }
                finally
                {
                    RemoveSchedulePositionsLocksL1(L1schedulePositionAllQueues);
                    ExitQueues(L1schedulePositionAllQueues.Values);
                }
            }
            catch (Exception e)
            {
                return new MessageObject { StatusCode = 400, Message = "Unexpected error. Please try again later." };
            }
        }

        private void RemoveAllClientLocks(int userId, string connectionId)
        {
            var _courseEditions = _unitOfWork.CourseEditions
                .Get(e => e.LockUserId == userId && e.LockUserConnectionId == connectionId);

            var _schedulePositions = _unitOfWork.SchedulePositions
                .Get(e => e.LockUserId == userId && e.LockUserConnectionId == connectionId)
                .Include(e => e.Timestamp);

            var courseEditions = _courseEditions.Any() ? _courseEditions.ToList() : new List<CourseEdition>();
            var schedulePositions = _schedulePositions.Any() ? _schedulePositions.ToList() : new List<SchedulePosition>();

            foreach (var courseEdition in courseEditions)
            {
                UnlockCourseEdition(courseEdition.CourseId, courseEdition.CourseEditionId);
            }

            foreach (var schedulePosition in schedulePositions)
            {
                var timestamp = schedulePosition.Timestamp;
                UnlockSchedulePositions(schedulePosition.RoomId, timestamp.PeriodIndex, timestamp.Day, new int[]{timestamp.Week});
            }
        }

        public override Task OnConnectedAsync()
        {
            var id = int.Parse(Context.User.Claims.FirstOrDefault(claim => claim.Type == "user_id")?.Value!);

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
