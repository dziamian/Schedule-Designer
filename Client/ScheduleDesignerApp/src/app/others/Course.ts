import { CourseType } from "./Types";

export class Course {
    constructor(
        public CourseId: number,
        public CourseType: CourseType,
        public Name: string,
        public UnitsMinutes: number
    ) {}
}