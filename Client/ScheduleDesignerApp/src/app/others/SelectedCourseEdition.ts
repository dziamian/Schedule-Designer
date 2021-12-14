import { CourseEdition } from "./CourseEdition";

export class SelectedCourseEdition {
    CanAddRoom:boolean = true;
    CanChangeRoom:boolean = true;
    CanShowScheduledChanges:boolean = true;
    CanMakeMove:boolean = true;
    IsMoving:boolean = false;
    CanCancel:boolean = true;

    constructor(
        public CourseEdition:CourseEdition,
        public PeriodIndex:number,
        public Day:number
    ) {}
}