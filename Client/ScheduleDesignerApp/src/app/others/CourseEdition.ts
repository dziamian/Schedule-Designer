import { Coordinator } from "./Accounts";
import { CourseType } from "./CourseType";
import { Group } from "./Group";
import { Room } from "./Room";

export class CourseEdition {
    Weeks:number[]|null = null;
    Room:Room|null = null;
    Optional:boolean = false;
    Locked:boolean = false;

    constructor(
        public CourseId:number,
        public CourseEditionId:number,
        public Name:string,
        public Type:CourseType,
        public Amount:number,
        public Groups:Group[],
        public Coordinators:Coordinator[]
    ) {}
}