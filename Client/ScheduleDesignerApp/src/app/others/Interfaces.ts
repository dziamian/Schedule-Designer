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

export interface IRoomType {
    RoomTypeId?: number,
    Name?: string
}

export interface IRoom {
    RoomId?: number,
    RoomTypeId?: number,
    Name?: string,
    Capacity?: number
}

export interface IUserInfo {
    UserId: number,
    FirstName: string,
    LastName: string,
    AcademicNumber: string,
    TitleBefore: string,
    TitleAfter: string,
    IsStudent: boolean,
    IsStaff: boolean,
    IsCoordinator: boolean,
    IsAdmin: boolean
}

export interface ISettings {
    CourseDurationMinutes?: number,
    StartTime?: string,
    EndTime?: string,
    TermDurationWeeks?: number
}