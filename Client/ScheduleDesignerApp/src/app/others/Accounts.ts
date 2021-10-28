export class Account {
    constructor(
        public UserId:number,
        public FirstName:string,
        public LastName:string,
        public Student:boolean,
        public Coordinator:boolean,
        public Titles:Titles|null,
        public Staff:boolean,
        public Admin:boolean
    ) {}
}

export class Titles {
    constructor(
        public TitleBefore:string,
        public TitleAfter:string
    ) {}
}

export class Student {
    
}

export class Coordinator {

}

export class Staff {

}