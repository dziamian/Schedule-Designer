export class Account {
    constructor(
        public User:User,
        public Student:Student|null,
        public Coordinator:Coordinator|null,
        public Staff:Staff|null
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
        public IsAdmin:boolean
    ) { }
}

export class User {
    constructor(
        public UserId:number,
        public FirstName:string,
        public LastName:string
    ) { }
}

export class SearchUser {
    constructor(
        public Users: Account[],
        public NextPage: boolean
    ) { }
}