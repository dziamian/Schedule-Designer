export class CourseEditionInfo {
    UnitsMinutes:number = 0;
    ScheduleAmount:number = 0;
    FullAmount:number = 0;
    IsLocked: boolean;
    IsLockedByAdmin: boolean;

    constructor(
        public CourseId:number,
        public CourseTypeId:number,
        public Name:string
    ) {}
}