import { Coordinator } from "./Accounts";
import { CourseType } from "./Types";
import { Group } from "./Group";
import { Room } from "./Room";
import { Settings } from "./Settings";
import { ScheduledMove } from "./ScheduledMove";

/**
 * Klasa reprezentująca informacje o odbywanych zajęciach.
 */
export class CourseEdition {
    /** Informuje czy użytkownik aktualnie wykonuje operację na zajęciach. */
    IsCurrentlyActive:boolean = false;
    /** Pełna liczba jednostek zajęciowych koniecznych do zrealizowania w ciągu semestru. */
    FullAmount:number = 0;
    /** Określa ile jednostek zajęciowych jest już ułożonych w planie. */
    ScheduleAmount:number = 0;
    /** Tablica zaplanowanych ruchów dla danej pozycji na planie.*/
    ScheduledMoves:ScheduledMove[] = [];
    /** Tygodnie, które są brane pod uwagę dla danej pozycji na planie. */
    Weeks:number[]|null = null;
    /** Pokój, w którym odbywane są zajęcia. */
    Room:Room|null = null;
    /** Określa czy zajęcia są zablokowane (edycja lub pozycje). */
    IsLocked: boolean;
    /** Określa czy zajęcia są zablokowane przez administratora (edycja lub pozycje). */
    IsLockedByAdmin: boolean;

    constructor(
        /** ID przedmiotu. */
        public CourseId:number,
        /** ID edycji zajęć. */
        public CourseEditionId:number,
        /** Nazwa przedmiotu. */
        public Name:string,
        /** Typ przedmiotu. */
        public Type:CourseType,
        /** Obecnie przechowywana liczba jednostek zajęciowych (zależna od widoku planu). */
        public CurrentAmount:number,
        /** Grupy, które odbywają zajęcia w planie. */
        public Groups:Group[],
        /** Prowadzący zajęć. */
        public Coordinators:Coordinator[]
    ) {}

    private static checkFrequency(weeks:number[],termDurationWeeks:number,even:boolean):boolean {
        const length = weeks.length;
        const halfTermDurationWeeks = termDurationWeeks / 2;
        const requiredLength = (even) ? Math.floor(halfTermDurationWeeks) : Math.ceil(halfTermDurationWeeks);
    
        if (length != requiredLength) {
            return false;
        }
    
        for (let i = 0; i < length; ++i) {
            if (even && weeks[i] % 2 != 0) {
                return false;
            }
            if (!even && weeks[i] % 2 == 0) {
                return false;
            }
        }
    
        return true;
    }

    /**
     * Metoda zamieniająca częstotliwość zajęć na ciąg znaków czytelny dla użytkownika.
     * @param weeks Tygodnie, które mają być wzięte pod uwagę
     * @returns Ciąg znaków określający częstotliwość zajęć czytelny dla użytkownika.
     */
    private static frequencyToString(weeks:number[]|null):string {
        if (weeks == null || weeks.length == 0) {
            return '';
        }
        const weeksLength = weeks.length;
        let result = (weeksLength > 1) ? 'Weeks ' : 'Week ';  
        weeks.sort((a, b) => a - b);
        let leftValue = -1;
        let move = 1;
        for (let i = 0; i < weeksLength; ++i) {
            if (leftValue != weeks[i] - move) {
                leftValue = weeks[i];
                move = 0;
            }
            if (weeks[i + 1] == undefined) {
                if (move == 0) {
                    result += `${weeks[i]}`;
                } else {
                    result += `${leftValue}-${weeks[i]}`
                }
            } else if (leftValue == weeks[i + 1] - move - 1) {
                ++move;
            } else {
                if (move == 0) {
                    result += `${weeks[i]}, `;
                } else {
                    result += `${leftValue}-${weeks[i]}, `
                }
            }
        }
        return result;
    }

    ShowFrequency(settings:Settings):string {
        return CourseEdition.ShowWeeksDescription(settings, this.Weeks, true);
    }

    static ShowWeeks(settings: Settings, weeks: number[]): string {
        return this.ShowWeeksDescription(settings, weeks, false);
    }

    static ShowFrequency(settings:Settings, weeks:number[]|null): string {
        return this.ShowWeeksDescription(settings, weeks, true);
    }

    /**
     * Metoda zwracająca ciąg znaków będących etykietą częstotliwości lub tygodni na podstawie parametrów.
     * @param settings Ustawienia aplikacji
     * @param weeks Tygodnie brane pod uwagę
     * @param isFrequencyDescription Czy tekst ma być etykietą danych tygodni czy częstotliwości (np. Semester -- Weekly).
     * @returns Etykietę tygodni lub częstotliwości zajęć
     */
    private static ShowWeeksDescription(settings:Settings, weeks:number[]|null, isFrequencyDescription: boolean):string {
        if (weeks == null) {
            return '';
        }
    
        const weeksLength = weeks.length;
        if (weeksLength == 0) {
            return '';
        }
    
        const termDurationWeeks = settings.TermDurationWeeks;
    
        if (weeksLength == termDurationWeeks) {
            return isFrequencyDescription ? 'Weekly' : 'Semester';
        }
    
        if (CourseEdition.checkFrequency(weeks, termDurationWeeks, true)) {
            return 'Even weeks';
        }
    
        if (CourseEdition.checkFrequency(weeks, termDurationWeeks, false)) {
            return 'Odd weeks';
        }
    
        return CourseEdition.frequencyToString(weeks);
    }

    /**
     * Zwraca liczbę zaplanowanych ruchów oraz propozycji w zależności od trybu podglądu planu.
     * @param isModifying Czy zwrócić liczbę dla trybu modyfikacji planu
     * @returns Liczbę zaplanowanych ruchów (i propozycji w zależności od trybu)
     */
    public getScheduledMovesBadge(isModifying: boolean): number  {
        if (isModifying) {
            return this.ScheduledMoves.length;
        }
        return this.ScheduledMoves.filter(move => move.IsConfirmed).length;
    }
}