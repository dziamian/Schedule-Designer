/**
 * Klasa reprezentująca ustawienia aplikacji.
 */
export class Settings {
    /** Etykiety rozpoczęcia okienek czasowych w ciągu dnia (np. 08:00). */
    Periods:string[];
    /** Etykiety okienek czasowych w ciągu dnia (np. 08:00 - 10:00). */
    TimeLabels:string[];
    /** Etykiety dni tygodnia. */
    DayLabels:string[];

    constructor(
        /** Liczba minut dla pojedynczej jednostki zajęciowej. */
        public CourseDurationMinutes:number,
        /** Godzina możliwego rozpoczęcia pierwszych zajęć w planie w ciągu dnia. */
        public StartTime:string,
        /** Godzina możliwego zakończenia ostatnich zajęć w planie w ciągu dnia. */
        public EndTime:string,
        /** Liczba tygodni w semestrze. */
        public TermDurationWeeks:number
    ) {
        this.StartTime = Settings.FromPeriodTime(this.StartTime);
        this.EndTime = Settings.FromPeriodTime(this.EndTime);
    }

    /**
     * Metoda konwertująca czas wyświetlany użytkownikowi na czas do wysłania na serwer.
     * @param value Czas wyświetlany użytkownikowi
     * @returns Czas przeznaczony do wysłania na serwer
     */
    public static ToPeriodTime(value: string): string {
        const matches = value.match(/\d+/g);
        if (matches != null) {
            return `PT${matches[0] ?? '0'}H${matches[1] ?? '0'}M0S`;
        }
        return value;
    }

    /**
     * Metoda konwertująca czas pobrany z serwera na czas do wyświetlenia użytkownikowi.
     * @param value Czas pobrany z serwera
     * @returns Czas przeznaczony do wyświetlenia użytkownikowi
     */
    public static FromPeriodTime(value: string): string {
        const matches = value.match(/\d+/g);
        if (matches != null) {
            if (matches[0]?.length < 2) {
                matches[0] = '0' + matches[0];
            }
            if (matches[1]?.length < 2) {
                matches[1] = '0' + matches[1];
            }
            return `${matches[0] ?? '00'}:${matches[1] ?? '00'}`;
        }
        return value;
    }
}