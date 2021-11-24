import { RoomType } from "./Types";

export class Room {
    Name:string;
    RoomType:RoomType;
    IsBusy:boolean = false;

    constructor(
        public RoomId:number
    ) { }
}