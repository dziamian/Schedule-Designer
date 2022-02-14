import { CourseEdition } from "../CourseEdition";
import { Settings } from "../Settings";
import { RoomType } from "../Types";

/**
 * Klasa przechowująca dane niezbędne do poprawnego wyświetlenia dialogu podglądu i zarządzania zaplanowanymi zmianami i propozycjami w systemie.
 */
export class ScheduledChangesDialogData {

    constructor(
        /** Wybrana edycja zajęć przez użytkownika. */
        public CourseEdition:CourseEdition,
        /** Indeksy pozycji zajęć na planie. */
        public SrcIndexes:number[],
        /** Etykiety dni tygodnia. */
        public DayLabels:string[],
        /** Etykiety okienek czasowych w ciągu dnia. */
        public TimeLabels:string[],
        /** Kolekcja typów pokojów z ich identyfikatorami. */
        public RoomTypes:Map<number,RoomType>,
        /** Instancja ustawień aplikacji. */
        public Settings:Settings,
        /** Identyfikator zalogowanego użytkownika (wykorzystywany w celu blokowania dostępu do usuwania i akceptowania ruchów nie należących do niego). */
        public PropositionUserId:number | null,
        /** Czy należy ignorować blokady nałożone przez użytkowników na powiązane z ruchami pozycje w planie. */
        public IgnoreUsersLocks:boolean,
        /** Określa czy dialog jest w trybie podglądu (fałsz) czy modyfikacji (prawda). */
        public IsModifying:boolean
    ) {}
}

/**
 * Klasa przechowująca rezultat wykonania operacji w dialogu przez użytkownika.
 */
export class ScheduledChangesDialogResult {
    static readonly EMPTY:ScheduledChangesDialogResult 
        = new ScheduledChangesDialogResult();

    /** Załączona wiadomość o wykonanej operacji. */
    Message:string;

    constructor() {}
}