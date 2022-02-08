export class UserInfo {
    constructor(
        public UserId: number,
        public FirstName: string,
        public LastName: string,
        public AcademicNumber: string,
        public TitleBefore: string,
        public TitleAfter: string,
        public IsStudent: boolean,
        public IsStaff: boolean,
        public IsCoordinator: boolean,
        public IsAdmin: boolean,
        public RepresentativeGroups: number[],
    ) {}
}

export class Titles {
    constructor(
        public TitleBefore:string,
        public TitleAfter:string
    ) {}
}

export class Coordinator {
    constructor(
        public User:User,
        public Titles:Titles
    ) { }
}

export class CoordinatorBasic {
    constructor (
        public UserId: number,
        public FullName: string
    ) {}
}

export class Student {
    constructor(
        public User:User,
        public StudentNumber:string,
        public RepresentativeGroups:number[],
        public Titles:Titles
    ) { }
}

export class StudentBasic {
    IsRepresentative: boolean = false;

    constructor(
        public UserId: number,
        public FullName: string
    ) { }
}

export class Staff {
    constructor(
        public User:User,
        public IsAdmin:boolean,
        public Titles:Titles
    ) { }
}

export class User {
    constructor(
        public UserId:number,
        public FirstName:string,
        public LastName:string
    ) { }
}

export class UserBasic {
    constructor(
        public UserId: number,
        public FullName: string
    ) { }
}

export class SearchUser {
    constructor(
        public Users: UserInfo[],
        public NextPage: boolean
    ) { }
}