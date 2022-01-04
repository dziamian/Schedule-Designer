using ScheduleDesigner.Hubs.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Hubs.Interfaces
{
    public interface IScheduleClient
    {
        Task LockCourseEdition(int courseId, int courseEditionId);
        
        Task LockSchedulePositions(
            int courseId, int courseEditionId, 
            int roomId, int periodIndex, 
            int day, int[] weeks);
        
        Task UnlockCourseEdition(int courseId, int courseEditionId);
        
        Task UnlockSchedulePositions(
            int courseId, int courseEditionId, 
            int roomId, int periodIndex, 
            int day, int[] weeks);
        
        Task AddedSchedulePositions(
            int courseId, int courseEditionId,
            int[] groupsIds, int mainGroupsAmount, int[] coordinatorsIds,
            int roomId, int periodIndex, 
            int day, int[] weeks);
        
        Task ModifiedSchedulePositions(
            int courseId, int courseEditionId,
            int[] groupsIds, int mainGroupsAmount, int[] coordinatorsIds,
            int previousRoomId, int newRoomId,
            int previousPeriodIndex, int newPeriodIndex,
            int previousDay, int newDay,
            int[] previousWeeks, int[] newWeeks,
            int[] movesIds);

        Task RemovedSchedulePositions(
            int courseId, int courseEditionId,
            int[] groupsIds, int mainGroupsAmount, int[] coordinatorsIds,
            int roomId, int periodIndex,
            int day, int[] weeks,
            int[] movesIds);

        Task AddedScheduledMove(
            int moveId, int userId, bool isConfirmed,
            int courseId, int courseEditionId,
            int roomId, int periodIndex,
            int day, int[] weeks);

        Task RemovedScheduledMove(
            int moveId,
            int courseId, int courseEditionId,
            int roomId, int periodIndex,
            int day, int[] weeks);

        Task AcceptedScheduledMove(
            int moveId,
            int courseId, int courseEditionId,
            int roomId, int periodIndex,
            int day, int[] weeks);

        Task SendResponse(MessageObject response);
    }
}
