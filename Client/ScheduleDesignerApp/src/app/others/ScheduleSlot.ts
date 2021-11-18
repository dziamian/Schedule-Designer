import { Room } from "./Room";

export class ScheduleSlot {
    Room:Room|null = null;

    constructor(
        public PeriodIndex:number,
        public Day:number
    ) {}
}