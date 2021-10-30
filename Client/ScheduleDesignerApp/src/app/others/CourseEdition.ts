import { Coordinator } from "./Coordinator";
import { CourseType } from "./CourseType";
import { Group } from "./Group";
import { Room } from "./Room";

export class CourseEdition {
    readonly name:string;
    readonly type:CourseType;
    readonly amount:number;
    readonly groups:Group[];
    readonly coordinators:Coordinator[];
    room:Room|null = null;
    locked:boolean = false;

    constructor(name:string, type:CourseType, amount:number, groups:Group[], coordinators:Coordinator[]) {
        this.name = name;
        this.type = type;
        this.amount = amount;
        this.groups = groups;
        this.coordinators = coordinators;
    }
}