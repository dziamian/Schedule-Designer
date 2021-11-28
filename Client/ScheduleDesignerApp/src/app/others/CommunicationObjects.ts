export class MessageObject {
    Message:string

    constructor(
        public StatusCode:number
    ) {}
}

export class SchedulePosition {

    constructor(
        public CourseId:number,
        public CourseEditionId:number,
        public RoomId:number,
        public PeriodIndex:number,
        public Day:number,
        public Weeks:number[]
    ) {}
}

export class AddedSchedulePositions {

    constructor(
        public GroupsIds:number[],
        public CoordinatorsIds:number[],
        public SchedulePosition:SchedulePosition
    ) {}
}

export class ModifiedSchedulePositions {

    constructor(
        public GroupsIds:number[],
        public CoordinatorsIds:number[],
        public SourceSchedulePosition:SchedulePosition,
        public DestinationSchedulePosition:SchedulePosition
    ) {}
}

export class RemovedSchedulePositions {

    constructor(
        public GroupsIds:number[],
        public CoordinatorsIds:number[],
        public SchedulePosition:SchedulePosition
    ) {}
}