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

export interface ICourseEdition {
    CourseId?: number;
    CourseEditionId?: number;
    Name?: string;
}

export interface IGroup {
    GroupId?: number;
    Name?: string;
    ParentGroupId?: number | null;
}