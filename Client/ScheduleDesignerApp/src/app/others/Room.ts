import { User } from "./Accounts";
import { RoomType } from "./Types";

/**
 * Klasa reprezentująca pokój.
 */
export class Room {
    /** Nazwa pokoju. */
    Name:string;
    /** Typ pokoju. */
    RoomType:RoomType;
    /** Pojemność pokoju. */
    Capacity:number;
    /** Czy jest aktualnie dostępny. */
    IsBusy:boolean = false;
    /** Użytkownik, który przypisał pokój do danego przedmiotu. */
    User:User|null;

    constructor(
        /** Identyfikator pokoju. */
        public RoomId:number
    ) { }
}