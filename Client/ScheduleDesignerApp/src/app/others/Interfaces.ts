export interface ICourseType {
    CourseTypeId?: number;
    Name?: string;
    Color?: string;
}

export interface ICourse {
    CourseId?: number;
    CourseTypeId?: number;
    Name?: string;
    UnitsMinutes?: number;
}