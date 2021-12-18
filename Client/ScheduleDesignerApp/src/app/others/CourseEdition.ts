import { Coordinator } from "./Accounts";
import { CourseType } from "./Types";
import { Group } from "./Group";
import { Room } from "./Room";
import { Settings } from "./Settings";
import { ScheduledMove } from "./ScheduledMove";

export class CourseEdition {
    IsCurrentlyActive:boolean = false;
    FullAmount:number = 0;
    ScheduleAmount:number = 0;
    ScheduledMoves:ScheduledMove[] = [];
    Weeks:number[]|null = null;
    Room:Room|null = null;
    Locked:boolean = false;

    constructor(
        public CourseId:number,
        public CourseEditionId:number,
        public Name:string,
        public Type:CourseType,
        public CurrentAmount:number,
        public Groups:Group[],
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

    private static frequencyToString(weeks:number[]|null):string {
        if (weeks == null) {
            return '';
        }
        let result = (weeks.length > 1) ? 'Weeks ' : 'Week ';  
        weeks.sort((a, b) => a - b).forEach((week) => {
            result += week + ', ';
        });
        result = result.substring(0, result.length - 2);
        return result;
    }

    ShowFrequency(settings:Settings):string {
        if (this.Weeks == null) {
            return '';
        }
    
        const courseWeeksLength = this.Weeks.length;
        if (courseWeeksLength == 0) {
            return '';
        }
    
        const termDurationWeeks = settings.TermDurationWeeks;
    
        if (courseWeeksLength == termDurationWeeks) {
            return 'Weekly';
        }
    
        if (CourseEdition.checkFrequency(this.Weeks, termDurationWeeks, true)) {
            return 'Even weeks';
        }
    
        if (CourseEdition.checkFrequency(this.Weeks, termDurationWeeks, false)) {
            return 'Odd weeks';
        }
    
        return CourseEdition.frequencyToString(this.Weeks);
    }

    static ShowFrequency(settings:Settings, weeks:number[]):string {
        if (weeks == null) {
            return '';
        }
    
        const weeksLength = weeks.length;
        if (weeksLength == 0) {
            return '';
        }
    
        const termDurationWeeks = settings.TermDurationWeeks;
    
        if (weeksLength == termDurationWeeks) {
            return 'Weekly';
        }
    
        if (CourseEdition.checkFrequency(weeks, termDurationWeeks, true)) {
            return 'Even weeks';
        }
    
        if (CourseEdition.checkFrequency(weeks, termDurationWeeks, false)) {
            return 'Odd weeks';
        }
    
        return CourseEdition.frequencyToString(weeks);
    }
}