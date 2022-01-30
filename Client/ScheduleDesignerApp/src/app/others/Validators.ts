import { AbstractControl, ValidationErrors, ValidatorFn } from "@angular/forms";
import { Settings } from "./Settings";

export function validUnitsMinutes(settings: Settings): ValidatorFn {
    return (formGroup: AbstractControl): ValidationErrors | null => {
        const hoursControl = formGroup.get('hours');
        const minutesControl = formGroup.get('minutes');

        if (!hoursControl || !minutesControl) {
            return null;
        }
        const hours = hoursControl.value;
        const minutes = minutesControl.value;

        const unitsMinutes = hours * 60 + minutes;

        if (unitsMinutes == 0) {
            return { invalidUnitsMinutes: true };
        }

        if (!((unitsMinutes % settings.CourseDurationMinutes == 0) 
            || (unitsMinutes * 2 / settings.CourseDurationMinutes % settings.TermDurationWeeks == 0))) {
                return { invalidUnitsMinutes: true };
        }

        return null;
    };
}