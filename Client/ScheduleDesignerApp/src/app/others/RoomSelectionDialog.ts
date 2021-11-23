import { CourseEdition } from "./CourseEdition";

export class RoomSelectionDialogData {

    constructor(
        public CourseEdition:CourseEdition,
        public SlotIndex:number[],
        public Weeks:number[],
        public DayLabels:string[],
        public TimeLabels:string[]
    ) { }
}

export const enum RoomSelectionDialogResult {
    ACCEPTED,
    SCHEDULED,
    FAILED
}