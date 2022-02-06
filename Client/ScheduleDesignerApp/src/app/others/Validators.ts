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

export function validPeriods(): ValidatorFn {
    return (formGroup: AbstractControl): ValidationErrors | null => {
        const hoursControl = formGroup.get('courseDurationHours');
        const minutesControl = formGroup.get('courseDurationMinutes');
        const startTimeControl = formGroup.get('startTime');
        const endTimeControl = formGroup.get('endTime');

        if (!hoursControl || !minutesControl || !startTimeControl || !endTimeControl) {
            return null;
        }
        const hours = hoursControl.value;
        const minutes = minutesControl.value;

        const courseDurationMinutes = hours * 60 + minutes;

        if (courseDurationMinutes == 0) {
            return { invalidCourseDuration: true };
        }

        const startHM = startTimeControl.value.split(':');
        const endHM = endTimeControl.value.split(':');

        const startMinutes = Number.parseInt(startHM[0]) * 60 + Number.parseInt(startHM[1]);
        const endMinutes = Number.parseInt(endHM[0]) * 60 + Number.parseInt(endHM[1]);

        if (endMinutes - startMinutes <= 0) {
            return { invalidPeriods: true };
        }

        if ((endMinutes - startMinutes) % courseDurationMinutes !== 0) {
            return { invalidPeriods: true };
        }

        return null;
    };
}