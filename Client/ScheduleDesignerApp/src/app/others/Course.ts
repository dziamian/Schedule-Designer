import { Coordinator } from "./Coordinator";
import { CourseType } from "./CourseType";
import { Group } from "./Group";
import { Room } from "./Room";

export class Course {
    readonly name:string;
    readonly type:CourseType;
    readonly amount:number;
    readonly group:Group;
    readonly coordinators:Coordinator[];
    room:Room|null = new Room("4.01D");

    constructor(name:string, type:CourseType, amount:number, group:Group, coordinators:Coordinator[]) {
        this.name = name;
        this.type = type;
        this.amount = amount;
        this.group = group;
        this.coordinators = coordinators;
    }
}