import { CourseEdition } from "./CourseEdition";

/**
 * Klasa przechowująca informacje o statusie wybranych pozycji w planie przez użytkownika.
 */
export class SelectedCourseEdition {
    /** Czy opcja przypisywania pokoju przedmiotowi jest dostępna. */
    CanAddRoom:boolean = true;
    /** Czy opcja zmiany pokoju dla pozycji jest dostępna. */
    CanChangeRoom:boolean = true;
    /** Czy opcja podejrzenia zaplanowanych zmian jest dostępna. */
    CanShowScheduledChanges:boolean = true;
    /** Czy opcja wykonania ruchu bez interakcji przeciągania i upuszczania jest dostępna. */
    CanMakeMove:boolean = true;
    /** Czy aktualnie wykonywana jest operacja zmiany w planie przez użytkownika (bez wykorzystania interakcji przeciągania i upuszczania). */
    IsMoving:boolean = false;
    /** Czy opcja wycofania (wyboru lub wykonywanej zmiany) jest dostępna. */
    CanCancel:boolean = true;

    constructor(
        /** Wybrana pozycja na planie (zajęcia). */
        public CourseEdition:CourseEdition,
        /** Indeks wybranego okienka czasowego w ciągu dnia. */
        public PeriodIndex:number = -1,
        /** Indeks wybranego dnia tygodnia. */
        public Day:number = -1
    ) {}
}