import { CourseEdition } from "../CourseEdition";
import { Room } from "../Room";
import { RoomType } from "../Types";

export class AddRoomSelectionDialogData {

    constructor(
        public CourseEdition:CourseEdition,
        public RoomTypes:Map<number,RoomType>
    ) {}
}

export class AddRoomSelectionDialogResult {
    static readonly EMPTY:AddRoomSelectionDialogResult 
        = new AddRoomSelectionDialogResult();

    Message:string;

    constructor() {}
}

export class RoomSelect {
    
    constructor(
        public Room:Room,
        public IsDisabled:boolean
    ) {}
}