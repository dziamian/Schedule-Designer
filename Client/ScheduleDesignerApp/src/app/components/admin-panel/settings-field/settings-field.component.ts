import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormControl, FormGroup, FormGroupDirective, NgForm, Validators } from '@angular/forms';
import { ErrorStateMatcher } from '@angular/material/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { finalize } from 'rxjs/operators';
import { Settings } from 'src/app/others/Settings';
import { validPeriods } from 'src/app/others/Validators';
import { AdministratorApiService } from 'src/app/services/AdministratorApiService/administrator-api.service';
import { ScheduleDesignerApiService } from 'src/app/services/ScheduleDesignerApiService/schedule-designer-api.service';
import { SignalrService } from 'src/app/services/SignalrService/signalr.service';

/**
 * Klasa odpowiadająca za wyświetlanie błędu o źle dobranych wartościach
 * liczby minut pojedynczej jednostki zajęciowej względem czasu rozpoczęcia i zakończenia.
 */
export class CourseDurationErrorMatcher implements ErrorStateMatcher {
  isErrorState(control: FormControl | null, form: FormGroupDirective | NgForm | null): boolean {
    const isSubmitted = form && form.submitted;
    const isCourseDurationInvalid = form && form.getError('invalidCourseDuration');
    return !!(control && (control.invalid || isCourseDurationInvalid) && (control.dirty || control.touched || isSubmitted || isCourseDurationInvalid));
  }
}

/**
 * Klasa odpowiadająca za wyświetlanie błędu o źle dobranych wartościach
 * czasu rozpoczęcia i zakończenia względem liczby minut pojedynczej jednostki zajęciowej.
 */
export class PeriodsErrorMatcher implements ErrorStateMatcher {
  isErrorState(control: FormControl | null, form: FormGroupDirective | NgForm | null): boolean {
    const isSubmitted = form && form.submitted;
    const arePeriodsInvalid = form && form.getError('invalidPeriods');
    return !!(control && (control.invalid || arePeriodsInvalid) && (control.dirty || control.touched || isSubmitted || arePeriodsInvalid));
  }
}

/**
 * Komponent zawierający widok obszaru roboczego panelu administracyjnego
 * dla sekcji modyfikowania danych na temat ustawień aplikacji.
 */
@Component({
  selector: 'app-settings-field',
  templateUrl: './settings-field.component.html',
  styleUrls: ['./settings-field.component.css']
})
export class SettingsFieldComponent implements OnInit {

  /** 
   * Dane wymagane do załadowania widoku obszaru roboczego.
   * Zawiera informacje o identyfikatorze zasobu, typie zasobu oraz rodzaju wykonywanej akcji (dodawania lub podglądu).
   */
  private _data: {id: string|undefined, type: string, actionType: string};
  
  /**
   * Metoda ustawiająca dane wymagane do załadowania widoku obszaru roboczego.
   * Po ustawieniu danych następuje załadowanie widoku.
   */
  @Input() set data(value: {id: string|undefined, type: string, actionType: string}) {
    this._data = value;
    this.loadView();
  } get data(): {id: string|undefined, type: string, actionType: string} {
    return this._data;
  }

  /** Emiter zdarzenia zapisu stanu modyfikacji zasobu. */
  @Output() onChange: EventEmitter<void> = new EventEmitter();

  /** Informacje o pobranych ustawieniach aplikacji z systemu 
   * (posiada odpowiednie informacje w przypadku trybu podglądu). 
   */
  originalSettings: Settings;

  /** Wartości początkowe formularza modyfikacji zasobu w celu 
   * możliwości późniejszego ich zresetowania. */
  originalValues: any;
  /** Określa czy włączony został tryb modyfikacji zasobu. */
  isModifying: boolean = false;

  /** Informuje czy dane zostały załadowane. */
  loading: boolean | null = null;

  constructor(
    private scheduleDesignerApiService: ScheduleDesignerApiService,
    private administratorApiService: AdministratorApiService,
    private signalrService: SignalrService,
    private snackBar: MatSnackBar
  ) { }

  /** Formularz modyfikacji oraz dodawania zasobu. */
  settingsForm: FormGroup;
  courseDurationErrorMatcher: CourseDurationErrorMatcher = new CourseDurationErrorMatcher();
  periodsErrorMatcher: PeriodsErrorMatcher = new PeriodsErrorMatcher();

  ngOnInit(): void {
  }

  /**
   * Metoda budująca formularz z danymi początkowymi podanymi w parametrze.
   * @param settings Dane początkowe zbudowanego formularza
   */
  private buildForm(settings: Settings) {
    this.settingsForm = new FormGroup({
      courseDurationHours: new FormControl(Math.floor(settings.CourseDurationMinutes / 60), [Validators.required]),
      courseDurationMinutes: new FormControl(settings.CourseDurationMinutes % 60, [Validators.required]),
      startTime: new FormControl(settings.StartTime, [Validators.required]),
      endTime: new FormControl(settings.EndTime, [Validators.required]),
      termDuration: new FormControl(settings.TermDurationWeeks, [Validators.required])
    }, [validPeriods()]);
    this.originalValues = this.settingsForm.value;
  }

  /** Metoda wyłączająca możliwość modyfikacji formularza. */
  private disableForm() {
    for (var controlName in this.settingsForm.controls) {
      this.settingsForm.controls[controlName].disable();
    }
    this.isModifying = false;
  }

  /** Metoda włączająca możliwość modyfikacji formularza. */
  private enableForm() {
    for (var controlName in this.settingsForm.controls) {
      this.settingsForm.controls[controlName].enable();
    }
    this.isModifying = true;
  }

  /**
   * Metoda ładująca dane wymagane do wyświetlenia obszaru roboczego.
   * Różnią się one w zależności od trybu widoku - dodawania lub podglądu.
   */
  private loadView() {
    this.loading = true;

    if (this._data.actionType === 'view') {
      this.scheduleDesignerApiService.GetSettings().subscribe(settings => {
        this.originalSettings = settings;
        this.buildForm(this.originalSettings);
        
        this.disableForm();

        this.loading = false;
      }, () => {
        this.snackBar.open("Could not find settings.", "OK");
      });
    } else {
      this.loading = false;
    }
  }

  /**
   * Metoda porównująca aktualny stan pól formularzy z oryginalnymi 
   * wartościami zasobu pobranymi z serwera.
   * @returns Prawdę jeśli dane w formularzu są identyczne z oryginalnymi wartościami zasobu
   */
  IsSameAsOriginal(): boolean {
    return this.originalSettings.CourseDurationMinutes === this.settingsForm.controls['courseDurationHours'].value * 60 
        + this.settingsForm.controls['courseDurationMinutes'].value
      && this.originalSettings.StartTime === this.settingsForm.controls['startTime'].value
      && this.originalSettings.EndTime === this.settingsForm.controls['endTime'].value
      && this.originalSettings.TermDurationWeeks === this.settingsForm.controls['termDuration'].value;
  }

  /**
   * Metoda uruchamiająca tryb modyfikacji zasobu.
   */
  Modify() {
    this.Reset();
    this.enableForm();
  }

  Reset() {
    this.settingsForm.reset(this.originalValues);
  }

  Cancel() {
    this.Reset();
    this.disableForm();
  }

  /**
   * Metoda wysyłająca żądanie modyfikacji zasobu na serwer (zgodnie z danymi podanymi w formularzu).
   */
  async Save() {
    if (!this.settingsForm.valid) {
      return;
    }

    const courseDurationMinutes = this.settingsForm.controls['courseDurationHours'].value * 60 
      + this.settingsForm.controls['courseDurationMinutes'].value;
    const startTime = this.settingsForm.controls['startTime'].value;
    const endTime = this.settingsForm.controls['endTime'].value;
    const termDurationWeeks = this.settingsForm.controls['termDuration'].value;

    const settings = {
      CourseDurationMinutes: this.originalSettings.CourseDurationMinutes === courseDurationMinutes ? undefined : courseDurationMinutes,
      StartTime: this.originalSettings.StartTime === startTime ? undefined : Settings.ToPeriodTime(startTime),
      EndTime: this.originalSettings.EndTime === endTime ? undefined : Settings.ToPeriodTime(endTime),
      TermDurationWeeks: this.originalSettings.TermDurationWeeks === termDurationWeeks ? undefined : termDurationWeeks
    };

    const connectionId = this.signalrService.connection.connectionId;
    var isLocked = false;
    if (settings.StartTime || settings.EndTime || settings.TermDurationWeeks) {
      if (!connectionId) {
        return;
      }

      try {
        const lockingResult = await this.signalrService.LockAllCourseEditions().toPromise();
        
        if (lockingResult.StatusCode >= 400) {
          throw lockingResult;
        }
        isLocked = true;
      } catch (error: any) {
        if (error.Message != undefined) {
          this.snackBar.open(error.Message, "OK");
        } else if (error.error != undefined) {
          this.snackBar.open(error.error, "OK");
        } else {
          this.snackBar.open("You are not authorized to do this.", "OK");
        }
        return;
      }
    }
    this.disableForm();
    this.administratorApiService.UpdateSettings(settings, connectionId ?? '').pipe(finalize(async () => {
      if (isLocked) {
        try {
          const unlockingResult = await this.signalrService.UnlockAllCourseEditions().toPromise();
    
          if (unlockingResult.StatusCode >= 400) {
            throw unlockingResult;
          }
  
        } catch (error:any) {
  
        }
      }
    })).subscribe(() => {
      this.originalSettings = new Settings(
        courseDurationMinutes, startTime, endTime, termDurationWeeks
      );
      this.buildForm(this.originalSettings);
      this.disableForm();
      
      this.onChange.emit();
      
      this.snackBar.open("Successfully updated settings.", "OK");
    }, response => {
      this.enableForm();
      
      if (response.error?.error?.message != undefined) {
        this.snackBar.open(response.error.error.message, "OK");
      } else if (typeof response.error !== 'object') {
        this.snackBar.open(response.error, "OK");
      }
    });
  }

}
