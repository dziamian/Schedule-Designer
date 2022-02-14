/**
 * Klasa zawierająca informacje o użytkowniku.
 */
export class UserInfo {
    constructor(
        /** Identyfikator użytkownika. */
        public UserId: number,
        /** Imię użytkownika. */
        public FirstName: string,
        /** Nazwisko użytkownika. */
        public LastName: string,
        /** Numer przypisany studentowi lub pracownikowi. */
        public AcademicNumber: string,
        /** Tytuł naukowy wypisywany przed nazwiskiem. */
        public TitleBefore: string,
        /** Tytuł naukowy wypisywany po nazwisku. */
        public TitleAfter: string,
        /** Określa czy użytkownik posiada rolę studenta. */
        public IsStudent: boolean,
        /** Określa czy użytkownik posiada rolę pracownika. */
        public IsStaff: boolean,
        /** Określa czy użytkownik posiada rolę prowadzącego. */
        public IsCoordinator: boolean,
        /** Określa czy użytkownik posiada rolę administratora. */
        public IsAdmin: boolean,
        /** Identyfikatory grup, których użytkownik jest starostą. */
        public RepresentativeGroups: number[],
    ) {}
}

/**
 * Klasa zawierająca informacje o tytułach naukowych.
 */
export class Titles {
    constructor(
        /** Tytuł naukowy wypisywany przed nazwiskiem. */
        public TitleBefore:string,
        /** Tytuł naukowy wypisywany po nazwisku. */
        public TitleAfter:string
    ) {}
}

/**
 * Klasa reprezentująca prowadzącego - posiada informacje o użytkowniku i jego tytułach naukowych.
 */
export class Coordinator {
    constructor(
        /** Podstawowe informacje o użytkowniku. */
        public User:User,
        /** Informacje o tytułach naukowych. */
        public Titles:Titles
    ) { }
}

/**
 * Klasa reprezentująca podstawowe informacje o prowadzącym - pełną nazwę (imię, nazwisko, tytuły) oraz identyfikator użytkownika.
 */
export class CoordinatorBasic {
    constructor (
        /** Identyfikator użytkownika. */
        public UserId: number,
        /** Pełna nazwa użytkownika. */
        public FullName: string
    ) {}
}

/**
 * Klasa reprezentująca studenta - posiada informacje o użytkowniku, tytułach naukowych, numerze studenta oraz identyfikatory grup, których jest starostą.
 */
export class Student {
    constructor(
        /** Podstawowe informacje o użytkowniku. */
        public User:User,
        /** Numer studenta. */
        public StudentNumber:string,
        /** Identyfikatory grup, których student jest starostą. */
        public RepresentativeGroups:number[],
        /** Tytuły naukowe studenta. */
        public Titles:Titles
    ) { }
}

/**
 * Klasa reprezentująca podstawowe informacje o studencie - identyfikator użytkownika, pełną nazwę (imię, nazwisko, tytuły) oraz status roli starosty dla danej grupy.
 */
export class StudentBasic {
    /** Określa czy student posiada rolę starosty. */
    IsRepresentative: boolean = false;

    constructor(
        /** Identyfikator użytkownika. */
        public UserId: number,
        /** Pełna nazwa użytkownika. */
        public FullName: string
    ) { }
}

/**
 * Klasa reprezentująca pracownika - posiada informacje o użytkowniku, tytułach naukowych oraz statusie administratora w systemie.
 */
export class Staff {
    constructor(
        /** Podstawowe informacje o użytkowniku. */
        public User:User,
        /** Określa czy pracownik jest administratorem systemu. */
        public IsAdmin:boolean,
        /** Tytuły naukowe studenta. */
        public Titles:Titles
    ) { }
}

/**
 * Klasa posiadająca podstawowe informacje o użytkowniku.
 */
export class User {
    constructor(
        /** Identyfikator użytkownika. */
        public UserId:number,
        /** Imię użytkownika. */
        public FirstName:string,
        /** Nazwisko użytkownika. */
        public LastName:string
    ) { }
}

/**
 * Klasa reprezentująca najbardziej podstawowe informacje o użytkowniku - jego identyfikator oraz pełną nazwę (imię, nazwisko, tytuły).
 */
export class UserBasic {
    constructor(
        /** Identyfikator użytkownika. */
        public UserId: number,
        /** Pełna nazwa użytkownika. */
        public FullName: string
    ) { }
}

/**
 * Klasa reprezentująca wynik wyszukiwania użytkowników z zewnętrznego systemu USOS.
 */
export class SearchUser {
    constructor(
        /** Lista odnalezionych użytkowników. */
        public Users: UserInfo[],
        /** Określa czy istnieje kolejna strona odnalezionych użytkowników do pobrania. */
        public NextPage: boolean
    ) { }
}