import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { CourseType } from 'src/app/others/Types';
import { AdministratorApiService } from 'src/app/services/AdministratorApiService/administrator-api.service';
import { ScheduleDesignerApiService } from 'src/app/services/ScheduleDesignerApiService/schedule-designer-api.service';

/**
 * Komponent zawierający widok obszaru roboczego panelu administracyjnego
 * dla sekcji wprowadzania i modyfikowania danych na temat typu przedmiotu.
 */
@Component({
  selector: 'app-course-type-field',
  templateUrl: './course-type-field.component.html',
  styleUrls: ['./course-type-field.component.css']
})
export class CourseTypeFieldComponent implements OnInit {

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
  /** 
   * Emiter zdarzenia dodania nowego zasobu do systemu.
   * Zdarzenie przechowuje informacje o identyfikatorach powstałego zasobu.
  */
  @Output() onCreate: EventEmitter<string> = new EventEmitter();
  /** Emiter zdarzenia usunięcia zasobu z systemu. */
  @Output() onRemove: EventEmitter<void> = new EventEmitter();

  /** Informacje o pobranym zasobie typie przedmiotu z systemu 
   * (posiada odpowiednie informacje w przypadku trybu podglądu). 
   */
  originalCourseType: CourseType;

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
    private snackBar: MatSnackBar
  ) { }

  /** Formularz modyfikacji oraz dodawania zasobu. */
  courseTypeForm: FormGroup;

  ngOnInit(): void {

  }

  /**
   * Metoda budująca formularz z danymi początkowymi podanymi w parametrze.
   * @param courseType Dane początkowe zbudowanego formularza
   */
  private buildForm(courseType: CourseType) {
    this.courseTypeForm = new FormGroup({
      name: new FormControl(courseType.Name, [Validators.required]),
      color: new FormControl(courseType.Color, [Validators.required])
    });
    this.originalValues = this.courseTypeForm.value;
  }

  /** Metoda wyłączająca możliwość modyfikacji formularza. */
  private disableForm() {
    for (var controlName in this.courseTypeForm.controls) {
      this.courseTypeForm.controls[controlName].disable();
    }
    this.isModifying = false;
  }

  /** Metoda włączająca możliwość modyfikacji formularza. */
  private enableForm() {
    for (var controlName in this.courseTypeForm.controls) {
      this.courseTypeForm.controls[controlName].enable();
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
      this.scheduleDesignerApiService.GetCourseType(Number.parseInt(this._data.id!)).subscribe((courseType) => {
        this.originalCourseType = courseType;
        this.buildForm(this.originalCourseType);
        
        this.disableForm();

        this.loading = false;
      }, () => {
        this.snackBar.open("Could not find course type.", "OK");
      });
    } else if (this._data.actionType === 'add') {
      this.originalCourseType = new CourseType(
        0, '', '#ffffff'
      );
      this.buildForm(this.originalCourseType);

      this.loading = false;
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
    return this.originalCourseType.Name === this.courseTypeForm.controls['name'].value 
      && this.originalCourseType.Color === this.courseTypeForm.controls['color'].value;
  }

  /**
   * Metoda uruchamiająca tryb modyfikacji zasobu.
   */
  Modify() {
    this.Reset();
    this.enableForm();
  }

  Reset() {
    this.courseTypeForm.reset(this.originalValues);
  }

  Cancel() {
    this.Reset();
    this.disableForm();
  }

  /**
   * Metoda wysyłająca żądanie modyfikacji zasobu na serwer (zgodnie z danymi podanymi w formularzu).
   */
  Save() {
    if (!this.courseTypeForm.valid) {
      return;
    }

    const name = this.courseTypeForm.controls['name'].value;
    const color = this.courseTypeForm.controls['color'].value;

    const courseType = {
      CourseTypeId: this.originalCourseType.CourseTypeId,
      Name: this.originalCourseType.Name === name ? undefined : name,
      Color: this.originalCourseType.Color === color ? undefined : color
    };
    this.disableForm();
    this.administratorApiService.UpdateCourseType(courseType).subscribe(() => {
      this.originalCourseType = new CourseType(
        this.originalCourseType.CourseTypeId, name, color
      );
      this.buildForm(this.originalCourseType);
      this.disableForm();
      
      this.onChange.emit();
      
      this.snackBar.open("Successfully updated course type.", "OK");
    }, response => {
      this.enableForm();
      
      if (response.error?.error?.message != undefined) {
        this.snackBar.open(response.error.error.message, "OK");
      } else if (typeof response.error !== 'object') {
        this.snackBar.open(response.error, "OK");
      }
    });
  }

  /**
   * Metoda wysyłająca żądanie utworzenia nowego zasobu na serwer (zgodnie z danymi podanymi w formularzu).
   */
  Create() {
    if (!this.courseTypeForm.valid) {
      return;
    }

    const name = this.courseTypeForm.controls['name'].value;
    const color = this.courseTypeForm.controls['color'].value;

    const courseType = {
      Name: name,
      Color: color
    };
    this.administratorApiService.CreateCourseType(courseType).subscribe((response) => {
      this.onCreate.emit(response.CourseTypeId);
      
      this.snackBar.open("Successfully created course type.", "OK");
    }, response => {
      if (response.error?.error?.message != undefined) {
        this.snackBar.open(response.error.error.message, "OK");
      } else if (typeof response.error !== 'object') {
        this.snackBar.open(response.error, "OK");
      }
    });
  }

  /**
   * Metoda wysyłająca żądanie usunięcia zasobu na serwer.
   */
  Remove() {
    this.administratorApiService.RemoveCourseType(this.originalCourseType.CourseTypeId).subscribe(() => {
      this.onRemove.emit();
      
      this.snackBar.open("Successfully removed course type.", "OK");
    }, response => {
      if (response.error?.error?.message != undefined) {
        this.snackBar.open(response.error.error.message, "OK");
      } else if (typeof response.error !== 'object') {
        this.snackBar.open(response.error, "OK");
      }
    });
  }
}
