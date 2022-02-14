import { CourseEdition } from "../CourseEdition";
import { Room } from "../Room";
import { RoomType } from "../Types";

/**
 * Klasa przechowująca dane niezbędne do poprawnego wyświetlenia dialogu przypisania pokoju do przedmiotu.
 */
export class AddRoomSelectionDialogData {

    constructor(
        /** Wybrana edycja zajęć przez użytkownika. */
        public CourseEdition:CourseEdition,
        /** Kolekcja typów pokojów z ich identyfikatorami. */
        public RoomTypes:Map<number,RoomType>
    ) {}
}

/**
 * Klasa przechowująca rezultat wykonania operacji w dialogu przez użytkownika.
 */
export class AddRoomSelectionDialogResult {
    static readonly EMPTY:AddRoomSelectionDialogResult 
        = new AddRoomSelectionDialogResult();

    /** Załączona wiadomość o wykonanej operacji. */
    Message:string;

    constructor() {}
}

/**
 * Klasa reprezentująca pojedynczy pokój na liście w oknie dialogowym.
 */
export class RoomSelect {
    
    constructor(
        /** Instancja pokoju. */
        public Room:Room,
        /** Czy jest zablokowany do wyboru. */
        public IsDisabled:boolean
    ) {}
}