import { RoomType } from "./Types";

export class Room {
    Name:string;
    RoomType:RoomType;
    Capacity:number;
    IsBusy:boolean = false;

    constructor(
        public RoomId:number
    ) { }
}