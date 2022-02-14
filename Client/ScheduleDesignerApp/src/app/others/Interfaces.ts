/**
 * Interfejs używany do wykonywania podstawowych operacji CRUD na typie przedmiotu.
 */
export interface ICourseType {
    CourseTypeId?: number;
    Name?: string;
    Color?: string;
}

/**
 * Interfejs używany do wykonywania podstawowych operacji CRUD na przedmiocie.
 */
export interface ICourse {
    CourseId?: number;
    CourseTypeId?: number;
    Name?: string;
    UnitsMinutes?: number;
}

/**
 * Interfejs używany do wykonywania podstawowych operacji CRUD na edycji zajęć.
 */
export interface ICourseEdition {
    CourseId?: number;
    CourseEditionId?: number;
    Name?: string;
}

/**
 * Interfejs używany do wykonywania podstawowych operacji CRUD na grupie.
 */
export interface IGroup {
    GroupId?: number;
    Name?: string;
    ParentGroupId?: number | null;
}

/**
 * Interfejs używany do wykonywania podstawowych operacji CRUD typie pokoju.
 */
export interface IRoomType {
    RoomTypeId?: number,
    Name?: string
}

/**
 * Interfejs używany do wykonywania podstawowych operacji CRUD na pokoju.
 */
export interface IRoom {
    RoomId?: number,
    RoomTypeId?: number,
    Name?: string,
    Capacity?: number
}

/**
 * Interfejs używany do wykonywania podstawowych operacji na użytkowniku.
 */
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

/**
 * Interfejs używany do wykonywania podstawowych operacji na ustawieniach aplikacji.
 */
export interface ISettings {
    CourseDurationMinutes?: number,
    StartTime?: string,
    EndTime?: string,
    TermDurationWeeks?: number
}