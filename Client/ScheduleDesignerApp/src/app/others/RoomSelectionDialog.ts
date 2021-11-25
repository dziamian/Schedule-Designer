import { CourseEdition } from "./CourseEdition";
import { Room } from "./Room";
import { RoomType } from "./Types";

export class RoomSelectionDialogData {

    constructor(
        public CourseEdition:CourseEdition,
        public SlotIndex:number[],
        public Weeks:number[],
        public DayLabels:string[],
        public TimeLabels:string[],
        public RoomTypes:Map<number,RoomType>,
        public IsMoveValid:boolean,
        public CanBeScheduled:boolean
    ) { }
}

export class RoomSelectionDialogResult {
    
    constructor(
        public Status:RoomSelectionDialogStatus,
        public Room:Room|null
    ) {}
}

export const enum RoomSelectionDialogStatus {
    ACCEPTED,
    SCHEDULED,
    FAILED,
    CANCELED
}