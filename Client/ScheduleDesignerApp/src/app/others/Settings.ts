export class Settings {
    Periods:string[];
    TimeLabels:string[];
    DayLabels:string[];

    constructor(
        public CourseDurationMinutes:number,
        public StartTime:string,
        public EndTime:string,
        public TermDurationWeeks:number
    ) {
        this.StartTime = Settings.FromPeriodTime(this.StartTime);
        this.EndTime = Settings.FromPeriodTime(this.EndTime);
    }

    public static ToPeriodTime(value: string): string {
        const matches = value.match(/\d+/g);
        if (matches != null) {
            return `PT${matches[0] ?? '0'}H${matches[1] ?? '0'}M0S`;
        }
        return value;
    }

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