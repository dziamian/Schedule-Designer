import { CourseType } from "./Types";

/**
 * Podstawowe informacje o przedmiocie.
 */
export class Course {
    constructor(
        /** Identyfikator przedmiotu. */
        public CourseId: number,
        /** Typ przedmiotu. */
        public CourseType: CourseType,
        /** Nazwa przedmiotu. */
        public Name: string,
        /** Liczba minut zajęć do odbycia w ciągu semestru. */
        public UnitsMinutes: number
    ) {}
}