/**
 * Skrócona informacja o edycji zajęć lub pozycji w planie.
 */
export class CourseEditionInfo {
    /** Liczba minut zajęć do odbycia w ciągu semestru. */
    UnitsMinutes:number = 0;
    /** Określa ile jednostek zajęciowych jest już ułożonych w planie. */
    ScheduleAmount:number = 0;
    /** Pełna liczba jednostek zajęciowych koniecznych do zrealizowania w ciągu semestru. */
    FullAmount:number = 0;
    /** Określa czy zajęcia są zablokowane (edycja lub pozycje). */
    IsLocked: boolean;
    /** Określa czy zajęcia są zablokowane przez administratora (edycja lub pozycje). */
    IsLockedByAdmin: boolean;

    constructor(
        /** Identyfikator przedmiotu. */
        public CourseId:number,
        /** Identyfikator edycji zajęć. */
        public CourseEditionId:number,
        /** Identyfikator typu przedmiotu. */
        public CourseTypeId:number,
        /** Nazwa przedmiotu. */
        public Name:string,
        /** Nazwa edycji zajęć. */
        public CourseEditionName:string
    ) {}
}

/**
 * Skrócona informacja o przedmiocie.
 */
export class CourseInfo {
    /** Liczba minut zajęć do odbycia w ciągu semestru. */
    UnitsMinutes:number = 0;

    constructor(
        /** Identyfikator przedmiotu. */
        public CourseId:number,
        /** Identyfikator typu przedmiotu. */
        public CourseTypeId: number,
        /** Nazwa przedmiotu. */
        public Name:string
    ) {}
}