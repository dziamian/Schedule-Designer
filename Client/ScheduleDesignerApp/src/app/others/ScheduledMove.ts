import { Room } from "./Room";

export class ScheduledMove {

    constructor (
        public MoveId:number,
        public IsConfirmed:boolean
    ) {}
}

export class ScheduledMoveDetails {
    Locked:boolean = false;
    IsRemoving:boolean = false;

    constructor (
        public MoveId:number,
        public IsConfirmed:boolean,
        public SourceWeeks:number[],
        public DestRoom:Room,
        public DestPeriodIndex:number,
        public DestDay:number,
        public DestWeeks:number[]
    ) {}
}