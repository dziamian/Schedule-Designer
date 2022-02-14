import { Settings } from "../Settings";

/**
 * Klasa przechowująca dane niezbędne do poprawnego wyświetlenia dialogu utworzenia niestandardowego widoku planu.
 */
export class SelectViewDialogData {
    constructor(
        /** Instancja ustawień aplikacji. */
        public Settings: Settings
    ) {}
}

/**
 * Klasa przechowująca rezultat wykonania operacji w dialogu przez użytkownika.
 */
export class SelectViewDialogResult {
    static readonly EMPTY:SelectViewDialogResult 
        = new SelectViewDialogResult(
            []
        );

    constructor(
        /** Tygodnie wybrane przez użytkownika. */
        public SelectedWeeks: number[]
    ) {}
}