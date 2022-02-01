export class Group {
    FullName:string;
    ParentGroupId:number|null;

    constructor(
        public GroupId:number,
    ) { }
}

export class GroupInfo {

    constructor(
        public GroupId:number,
        public BasicName:string,
        public FullName:string,
        public ParentIds:number[],
        public ChildIds:number[]
    ) { }
}