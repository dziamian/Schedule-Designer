import { User } from "./Accounts";
import { RoomType } from "./Types";

export class Room {
    Name:string;
    RoomType:RoomType;
    Capacity:number;
    IsBusy:boolean = false;
    User:User|null;

    constructor(
        public RoomId:number
    ) { }
}