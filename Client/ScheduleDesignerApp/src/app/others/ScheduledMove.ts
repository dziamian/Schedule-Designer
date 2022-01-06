import { Room } from "./Room";

export class ScheduledMove {

    constructor (
        public MoveId:number,
        public UserId:number,
        public IsConfirmed:boolean
    ) {}
}

export class ScheduledMoveDetails {
    IsLocked: boolean;
    IsLockedByAdmin: boolean;
    IsRemoving:boolean = false;
    IsAccepting:boolean = false;

    constructor (
        public MoveId:number,
        public IsConfirmed:boolean,
        public UserId:number,
        public SourceWeeks:number[],
        public DestRoom:Room,
        public DestPeriodIndex:number,
        public DestDay:number,
        public DestWeeks:number[]
    ) {}
}