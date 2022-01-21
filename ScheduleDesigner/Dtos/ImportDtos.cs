namespace ScheduleDesigner.Dtos
{
    public static class ImportDtos
    {
        public class SchedulePositionDto
        {
            public int RoomId { get; set; }
            public int TimestampId { get; set; }
            public int CourseId { get; set; }
            public int CourseEditionId { get; set; }
        }
    }
}
