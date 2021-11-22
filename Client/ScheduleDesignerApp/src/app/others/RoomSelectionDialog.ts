export class RoomSelectionDialogData {

    constructor(
        public CourseId:number,
        public CourseEditionId:number,
        public SlotIndex:number[],
        public Weeks:number[]
    ) { }
}

export const enum RoomSelectionDialogResult {
    ACCEPTED,
    SCHEDULED,
    FAILED
}