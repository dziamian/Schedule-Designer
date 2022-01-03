import { CourseEdition } from "../CourseEdition";
import { Settings } from "../Settings";
import { RoomType } from "../Types";

export class ScheduledChangesDialogData {

    constructor(
        public CourseEdition:CourseEdition,
        public SrcIndexes:number[],
        public DayLabels:string[],
        public TimeLabels:string[],
        public RoomTypes:Map<number,RoomType>,
        public Settings:Settings,
        public IsModifying:boolean
    ) {}
}

export class ScheduledChangesDialogResult {
    static readonly EMPTY:ScheduledChangesDialogResult 
        = new ScheduledChangesDialogResult();

    Message:string;

    constructor() {}
}