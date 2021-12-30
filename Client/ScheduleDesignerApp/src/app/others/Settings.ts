export class Settings {
    Periods:string[];
    TimeLabels:string[];
    DayLabels:string[];

    constructor(
        public CourseDurationMinutes:number,
        public StartTime:string,
        public EndTime:string,
        public TermDurationWeeks:number
    ) { }
}