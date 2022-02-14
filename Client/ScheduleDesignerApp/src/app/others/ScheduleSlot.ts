/**
 * Klasa reprezentująca pojedyncze miejsce na wyświetlanym planie.
 * Używana głównie do odczytu miejsc, które mogą spowodować konflikty w planie.
 */
export class ScheduleSlot {

    constructor(
        public PeriodIndex:number,
        public Day:number
    ) {}
}