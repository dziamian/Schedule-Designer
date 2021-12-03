import { Coordinator } from "./Accounts";
import { CourseType } from "./Types";
import { Group } from "./Group";
import { Room } from "./Room";

export class CourseEdition {
    FullAmount:number = 0;
    ScheduleAmount:number = 0;
    Weeks:number[]|null = null;
    Room:Room|null = null;
    Locked:boolean = false;

    constructor(
        public CourseId:number,
        public CourseEditionId:number,
        public Name:string,
        public Type:CourseType,
        public CurrentAmount:number,
        public Groups:Group[],
        public Coordinators:Coordinator[]
    ) {}
}