export class Settings {
    periods:string[];

    constructor(
        public CourseDurationMinutes:number,
        public StartTime:string,
        public EndTime:string,
        public TermDurationWeeks:number
    ) { }
}