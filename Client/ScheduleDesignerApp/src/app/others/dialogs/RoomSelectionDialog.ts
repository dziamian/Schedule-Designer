import { CourseEdition } from "../CourseEdition";
import { Filter } from "../Filter";
import { Room } from "../Room";
import { RoomType } from "../Types";

/**
 * Klasa przechowująca dane niezbędne do poprawnego wyświetlenia dialogu wybrania pokoju i wykonania operacji na planie.
 */
export class RoomSelectionDialogData {

    constructor(
        /** Wybrana edycja zajęć przez użytkownika. */
        public CourseEdition:CourseEdition,
        /** Indeksy początkowej pozycji zajęć na planie (-1,-1 jeśli nieułożone). */
        public SrcIndexes:number[],
        /** Indeksy końcowej pozycji zajęć na planie. */
        public DestIndexes:number[],
        /** Tygodnie, które są brane pod uwagę podczas wykonywanej operacji. */
        public Weeks:number[],
        /** Etykiety dni tygodnia. */
        public DayLabels:string[],
        /** Etykiety okienek czasowych w ciągu dnia. */
        public TimeLabels:string[],
        /** Kolekcja typów pokojów z ich identyfikatorami. */
        public RoomTypes:Map<number,RoomType>,
        /** Czy ruch jest możliwy do wykonania, czy tylko do zaplanowania. */
        public IsMoveValid:boolean,
        /** Czy operacja pozwala na tworzenie zaplanowanych zmian. */
        public CanBeScheduled:boolean,
        /** Czy możliwe jest wykonywanie bieżących ruchów na planie. */
        public IsMoveAvailable:boolean,
        /** Czy możliwe jest wykonywanie propozycji. */
        public IsPropositionAvailable:boolean,
        /** Filtr określający jakie dane należy brać pod uwagę podczas odbierania ich z serwera (np. w celu wycofania operacji na planie). */
        public Filter: Filter
    ) { }
}

/**
 * Typ wyliczeniowy określający status wykonanej operacji w oknie dialogowym przez użytkownika.
 */
export const enum RoomSelectionDialogStatus {
    /** Ruch został wykonany. */
    ACCEPTED,
    /** Ruch został zaplanowany. */
    SCHEDULED,
    /** Ruch się nie powiódł. */
    FAILED,
    /** Ruch został anulowany. */
    CANCELED
}

/**
 * Klasa przechowująca rezultat wykonania operacji w dialogu przez użytkownika.
 */
export class RoomSelectionDialogResult {
    static readonly CANCELED:RoomSelectionDialogResult 
        = new RoomSelectionDialogResult(
            RoomSelectionDialogStatus.CANCELED,
            null,
            []
        );

    /** Załączona wiadomość o wykonanej operacji. */
    Message:string;

    constructor(
        /** Status operacji. */
        public Status:RoomSelectionDialogStatus,
        /** Pokój wybrany przez użytkownika. */
        public Room:Room|null,
        /** Tygodnie wzięte pod uwagę podczas operacji na planie. */
        public Weeks:number[]
    ) {}
}