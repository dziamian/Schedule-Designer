import { CourseEdition } from "./CourseEdition";

export class RoomSelectionDialogData {

    constructor(
        public CourseEdition:CourseEdition,
        public SlotIndex:number[],
        public Weeks:number[]
    ) { }
}

export const enum RoomSelectionDialogResult {
    ACCEPTED,
    SCHEDULED,
    FAILED
}