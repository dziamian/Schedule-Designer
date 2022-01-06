import { CourseEdition } from "../CourseEdition";
import { Filter } from "../Filter";
import { Room } from "../Room";
import { RoomType } from "../Types";

export class RoomSelectionDialogData {

    constructor(
        public CourseEdition:CourseEdition,
        public SrcIndexes:number[],
        public DestIndexes:number[],
        public Weeks:number[],
        public DayLabels:string[],
        public TimeLabels:string[],
        public RoomTypes:Map<number,RoomType>,
        public IsMoveValid:boolean,
        public CanBeScheduled:boolean,
        public IsMoveAvailable:boolean,
        public IsPropositionAvailable:boolean,
        public Filter: Filter
    ) { }
}

export const enum RoomSelectionDialogStatus {
    ACCEPTED,
    SCHEDULED,
    FAILED,
    CANCELED
}

export class RoomSelectionDialogResult {
    static readonly CANCELED:RoomSelectionDialogResult 
        = new RoomSelectionDialogResult(
            RoomSelectionDialogStatus.CANCELED,
            null,
            []
        );

    Message:string;

    constructor(
        public Status:RoomSelectionDialogStatus,
        public Room:Room|null,
        public Weeks:number[]
    ) {}
}