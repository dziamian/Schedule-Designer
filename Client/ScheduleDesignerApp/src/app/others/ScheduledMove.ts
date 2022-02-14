import { Titles } from "./Accounts";
import { Room } from "./Room";

/**
 * Podstawowe informacje o zaplanowanym ruchu (zmianie lub propozycji zmiany).
 */
export class ScheduledMove {

    constructor (
        /** Identyfikator ruchu. */
        public MoveId:number,
        /** Identyfikator użytkownika, który utworzył ruch w systemie. */
        public UserId:number,
        /** Czy ruch jest potwierdzony do wykonania. */
        public IsConfirmed:boolean
    ) {}
}

/**
 * Klasa przechowująca większą ilość informacji na temat zaplanowanego ruchu.
 */
export class ScheduledMoveDetails {
    /** Określa czy któraś z pozycji w planie mająca powiązanie z ruchem jest zablokowana. */
    IsLocked: boolean;
    /** Określa czy któraś z pozycji w planie mająca powiązanie z ruchem jest zablokowana przez administratora. */
    IsLockedByAdmin: boolean;
    /** Czy aktualnie jest wykonywana operacja usuwania ruchu z systemu. */
    IsRemoving:boolean = false;
    /** Czy aktualnie jest wykonywana operacja akceptowania propozycji ruchu. */
    IsAccepting:boolean = false;

    constructor (
        /** Identyfikator ruchu. */
        public MoveId:number,
        /** Czy ruch jest potwierdzony do wykonania. */
        public IsConfirmed:boolean,
        /** Identyfikator użytkownika, który utworzył ruch w systemie. */
        public UserId:number,
        /** Tygodnie źródłowe, których dotyczy ruch. */
        public SourceWeeks:number[],
        /** Pokój docelowy ruchu. */
        public DestRoom:Room,
        /** Indeks docelowego okienka czasowego w ciągu dnia. */
        public DestPeriodIndex:number,
        /** Indeks docelowego dnia tygodnia. */
        public DestDay:number,
        /** Tygodnie docelowe, których dotyczy ruch. */
        public DestWeeks:number[]
    ) {}
}

/**
 * Dodatkowe informacje o zaplanowanym ruchu lub propozycji.
 */
export class ScheduledMoveInfo {
    
    constructor(
        /** Imię użytkownika, który utworzył ruch w systemie. */
        public FirstName:string,
        /** Nazwisko użytkownika, który utworzył ruch w systemie. */
        public LastName:string,
        /** Tytuły użytkownika, który utworzył ruch w systemie */
        public Titles:Titles|null,
        /** Wiadomość załączona do propozycji. */
        public Message:string|null
    ) {}
}