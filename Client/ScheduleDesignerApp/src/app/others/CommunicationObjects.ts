export class MessageObject {
    Message:string = "";

    constructor(
        public StatusCode:number
    ) {}
}

export class SchedulePosition {
    Locked: {value: boolean, byAdmin: boolean} = {value: false, byAdmin: false};

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
        public MainGroupsAmount:number,
        public CoordinatorsIds:number[],
        public SchedulePosition:SchedulePosition
    ) {}
}

export class ModifiedSchedulePositions {

    constructor(
        public GroupsIds:number[],
        public MainGroupsAmount:number,
        public CoordinatorsIds:number[],
        public SourceSchedulePosition:SchedulePosition,
        public DestinationSchedulePosition:SchedulePosition,
        public MovesIds:number[]
    ) {}
}

export class RemovedSchedulePositions {

    constructor(
        public GroupsIds:number[],
        public MainGroupsAmount:number,
        public CoordinatorsIds:number[],
        public SchedulePosition:SchedulePosition,
        public MovesIds:number[]
    ) {}
}