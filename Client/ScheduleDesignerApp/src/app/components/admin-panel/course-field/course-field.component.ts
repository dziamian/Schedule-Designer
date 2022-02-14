import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormControl, FormGroup, FormGroupDirective, NgForm, Validators } from '@angular/forms';
import { ErrorStateMatcher } from '@angular/material/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { forkJoin } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { Course } from 'src/app/others/Course';
import { ResourceNode } from 'src/app/others/ResourcesTree';
import { Room } from 'src/app/others/Room';
import { Settings } from 'src/app/others/Settings';
import { CourseType, RoomType } from 'src/app/others/Types';
import { validUnitsMinutes } from 'src/app/others/Validators';
import { AdministratorApiService } from 'src/app/services/AdministratorApiService/administrator-api.service';
import { ScheduleDesignerApiService } from 'src/app/services/ScheduleDesignerApiService/schedule-designer-api.service';
import { SignalrService } from 'src/app/services/SignalrService/signalr.service';

/**
 * Klasa odpowiadająca za wyświetlanie błędu o źle dobranych wartościach
 * liczby minut, które są wymagane do odbycia w ciągu semestru.
 */
export class UnitsMinutesErrorMatcher implements ErrorStateMatcher {
  isErrorState(control: FormControl | null, form: FormGroupDirective | NgForm | null): boolean {
    const isSubmitted = form && form.submitted;
    const areUnitsMinutesInvalid = form && form.getError('invalidUnitsMinutes');
    return !!(control && (control.invalid || areUnitsMinutesInvalid) && (control.dirty || control.touched || isSubmitted || areUnitsMinutesInvalid));
  }
}

/**
 * Komponent zawierający widok obszaru roboczego panelu administracyjnego
 * dla sekcji wprowadzania i modyfikowania danych na temat przedmiotu.
 */
@Component({
  selector: 'app-course-field',
  templateUrl: './course-field.component.html',
  styleUrls: ['./course-field.component.css']
})
export class CourseFieldComponent implements OnInit {

  /**
   * Rezultat wyboru pochodzący z drugiego drzewka zasobów.
   * Zawiera informacje na temat identyfikatora zasobu oraz wybranego węzła drzewka.
   */
  private _selectedResult: {type: string, node: ResourceNode} | null;
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
    this.treeVisible = {type: '', value: false};
    this.loadView();
  } get data(): {id: string|undefined, type: string, actionType: string} {
    return this._data;
  }
  /** Ustawienia aplikacji */
  @Input() settings: Settings;
  /**
   * Metoda ustawiająca rezultat wyboru pochodzący z drugiego drzewka zasobów.
   * Po ustawieniu rezultatu wywoływana jest odpowiednia metoda 
   * - przypisania pokoju do przedmiotu lub wyboru typu przedmiotu.
   */
  @Input() set selectedResult(value: {type: string, node: ResourceNode} | null) {
    if (value == null || value == undefined) {
      return;
    }
    
    this._selectedResult = value;

    switch (value.type) {
      case 'course-type': {
        this.modifiableCourseTypeId = Number.parseInt(value.node.item.id!);
        if (this.courseForm) {
          this.courseForm.controls['type'].setValue(value.node.item.name);
        }

        this.treeVisible = {type: 'course-type', value: false};
        setTimeout(() => {
          this.onSelect.emit({type: '', header: '', visible: this.treeVisible.value, excludeTypes: [], excludeIds: []});
        });
      } break;
      case 'room': {
        this.AddRoom(Number.parseInt(value.node.item.id!), value.node.item.name);
      } break;
    }
  } get selectedResult(): {type: string, node: ResourceNode} | null { 
    return this._selectedResult;
  }

  /**
   * Emiter zdarzenia wyświetlenia (bądź ukrycia) i załadowania drugiego drzewka zasobów.
   * Zdarzenie posiada informacje o typie ładowanych zasobów, nagłówku drzewka,
   * czy powinno zostać wyświetlone na ekranie, które typy zasobów powinny zostać pominięte (wykluczone) w drzewku
   * oraz identyfikatory węzłów, które powinny zostać pominięte (wykluczone).
   */
  @Output() onSelect: EventEmitter<{
    type: string, 
    header: string,
    visible: boolean,
    excludeTypes: string[],
    excludeIds: string[]
  }> = new EventEmitter();
  
  /** 
   * Emiter zdarzenia dodania do listy rezultatu wybranego z drugiego drzewka zasobów.
   * Zdarzenie posiada informacje o identyfikatorach dodanych zasobów oraz ich typ.
   */
  @Output() onListAdd: EventEmitter<{ids: string[], type: string}> = new EventEmitter();
  /**
   * Emiter zdarzenia usunięcia z listy rezultatu, który może być 
   * teraz dostępny do wyboru w drugim drzewku zasobów.
   * Zdarzenie posiada informacje o identyfikatorach usuniętych zasobów oraz ich typ.
   */
  @Output() onListRemove: EventEmitter<{ids: string[], type: string}> = new EventEmitter();

  /** Emiter zdarzenia zapisu stanu modyfikacji zasobu. */
  @Output() onChange: EventEmitter<void> = new EventEmitter();
  /** 
   * Emiter zdarzenia dodania nowego zasobu do systemu.
   * Zdarzenie przechowuje informacje o identyfikatorach powstałego zasobu.
  */
  @Output() onCreate: EventEmitter<string> = new EventEmitter();
  /** Emiter zdarzenia usunięcia zasobu z systemu. */
  @Output() onRemove: EventEmitter<void> = new EventEmitter();

  /** Informacje o pobranym zasobie przedmiotu z systemu 
   * (posiada odpowiednie informacje w przypadku trybu podglądu). 
   */
  originalCourse: Course;
  /** Informacje o przypisanych pokojach do wyświetlonego zasobu przedmiotu. */
  courseRooms: Room[];

  /** Wartości początkowe formularza modyfikacji zasobu w celu 
   * możliwości późniejszego ich zresetowania. */
  originalValues: any;
  /** Identyfikator wybranego typu przedmiotu. */
  modifiableCourseTypeId: number;
  /** Określa czy włączony został tryb modyfikacji zasobu. */
  isModifying: boolean = false;

  /** Określa aktualny stan widoczności drugiego drzewka zasobów 
   * (oraz aktualnie wyświetlany typ zasobów). 
   */
  treeVisible: {type: string, value: boolean} = {type: '', value: false};

  /** Informuje czy dane zostały załadowane. */
  loading: boolean | null = null;

  constructor(
    private scheduleDesignerApiService: ScheduleDesignerApiService,
    private administratorApiService: AdministratorApiService,
    private signalrService: SignalrService,
    private snackBar: MatSnackBar
  ) { }

  /** Formularz modyfikacji oraz dodawania zasobu. */
  courseForm: FormGroup;
  errorMatcher: UnitsMinutesErrorMatcher = new UnitsMinutesErrorMatcher();

  GetCourseDurationHours(): number {
    return Math.floor(this.settings.CourseDurationMinutes / 60);
  }

  GetCourseDurationMinutes(): number {
    return this.settings.CourseDurationMinutes % 60;
  }

  ngOnInit(): void {

  }

  /**
   * Metoda budująca formularz z danymi początkowymi podanymi w parametrze.
   * @param course Dane początkowe zbudowanego formularza
   */
  private buildForm(course: Course) {
    this.courseForm = new FormGroup({
      type: new FormControl(course.CourseType.Name, [Validators.required]),
      name: new FormControl(course.Name, [Validators.required]),
      hours: new FormControl(Math.floor(course.UnitsMinutes / 60), [Validators.required]),
      minutes: new FormControl(course.UnitsMinutes % 60, [Validators.required]),
    }, [validUnitsMinutes(this.settings)]);
    this.originalValues = this.courseForm.value;
  }

  /** Metoda wyłączająca możliwość modyfikacji formularza. */
  private disableForm() {
    for (var controlName in this.courseForm.controls) {
      this.courseForm.controls[controlName].disable();
    }
    this.isModifying = false;
  }

  /** Metoda włączająca możliwość modyfikacji formularza. */
  private enableForm() {
    for (var controlName in this.courseForm.controls) {
      this.courseForm.controls[controlName].enable();
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
      const courseId = Number.parseInt(this._data.id!);
      forkJoin([
        this.scheduleDesignerApiService.GetCourse(courseId),
        this.scheduleDesignerApiService.GetCourseRooms(courseId, new Map<number, RoomType>())
      ]).subscribe(([course, rooms]) => {
        this.originalCourse = course;
        this.courseRooms = rooms;
        this.buildForm(this.originalCourse);
        
        this.disableForm();

        this.loading = false;
      }, () => {
        this.snackBar.open("Could not find course.", "OK");
      });
    } else if (this._data.actionType === 'add') {
      this.originalCourse = new Course(
        0, new CourseType(0, '', '#ffffff'), '', 0
      );
      this.courseRooms = [];
      this.buildForm(this.originalCourse);

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
    return this.originalCourse.CourseType.Name === this.courseForm.controls['type'].value 
      && this.originalCourse.Name === this.courseForm.controls['name'].value
      && this.originalCourse.UnitsMinutes === this.courseForm.controls['hours'].value * 60 
        + this.courseForm.controls['minutes'].value;
  }

  /**
   * Metoda uruchamiająca tryb modyfikacji zasobu.
   */
  Modify() {
    this.Reset();
    this.enableForm();
  }

  /**
   * Metoda wysyłająca zdarzenie powodujące wyświetlenie (bądź ukrycie) 
   * i załadowanie drugiego drzewka zasobów informacjami o typach przedmiotów,
   * które można wybrać.
   */
  SelectCourseType() {
    if (this.treeVisible.type === 'course-type') {
      this.treeVisible.value = !this.treeVisible.value;
    } else {
      this.treeVisible.type = 'course-type';
      this.treeVisible.value = true;
    }
    this.onSelect.emit({
      type: 'course-types', 
      header: 'Set type of the course:', 
      visible: this.treeVisible.value, 
      excludeTypes: [], 
      excludeIds: []
    });
  }

  /**
   * Metoda wysyłająca zdarzenie powodujące wyświetlenie (bądź ukrycie) 
   * i załadowanie drugiego drzewka zasobów informacjami o pokojach,
   * które można przypisać do przedmiotu.
   */
  SelectRoom() {
    if (this.treeVisible.type === 'room') {
      this.treeVisible.value = !this.treeVisible.value;
    } else {
      this.treeVisible.type = 'room';
      this.treeVisible.value = true;
    }
    this.onSelect.emit({
      type: 'rooms-on-types', 
      header: 'Add available rooms for course:', 
      visible: this.treeVisible.value, 
      excludeTypes: ['room-type'], 
      excludeIds: this.courseRooms.map(r => r.RoomId.toString())
    });
  }

  /**
   * Metoda wysyłająca żądanie przypisania pokoju do przedmiotu na serwer.
   * @param roomId Identyfikator pokoju do przypisania
   * @param roomName Pełna nazwa pokoju do przypisania
   */
  AddRoom(roomId: number, roomName: string) {
    this.scheduleDesignerApiService.AddCourseRoom(this.originalCourse.CourseId, roomId).subscribe(response => {
      const room = new Room(roomId);
      room.Name = roomName;
      this.courseRooms.push(room);
      this.courseRooms.sort((a,b) => a.RoomId - b.RoomId);

      this.onListAdd.emit({ids: [roomId.toString()], type: 'rooms-on-types'});
      
      this.snackBar.open("Successfully added room for the course.", "OK");
    }, response => {
      if (response.error?.error?.message != undefined) {
        this.snackBar.open(response.error.error.message, "OK");
      } else if (typeof response.error !== 'object') {
        this.snackBar.open(response.error, "OK");
      }
    });
  }

  /**
   * Metoda wysyłająca żądanie usunięcia przypisania pokoju do przedmiotu na serwer.
   * @param roomId Identyfikator przypisanego pokoju
   */
  RemoveRoom(roomId: number) {
    this.administratorApiService.RemoveCourseRoom(this.originalCourse.CourseId, roomId).subscribe(response => {
      
      const index = this.courseRooms.findIndex(r => r.RoomId == roomId);
      if (index != -1) {
        this.courseRooms.splice(index, 1);
      }

      this.onListRemove.emit({ids: [roomId.toString()], type: 'rooms-on-types'});
      
      this.snackBar.open("Successfully removed room from the course.", "OK");
    }, response => {
      if (response.error?.error?.message != undefined) {
        this.snackBar.open(response.error.error.message, "OK");
      } else if (typeof response.error !== 'object') {
        this.snackBar.open(response.error, "OK");
      }
    });
  }

  Reset() {
    this.courseForm.reset(this.originalValues);
  }

  Cancel() {
    this.Reset();
    this.disableForm();
  }

  /**
   * Metoda wysyłająca żądanie modyfikacji zasobu na serwer (zgodnie z danymi podanymi w formularzu).
   */
  async Save() {
    if (!this.courseForm.valid) {
      return;
    }

    const typeId = this.modifiableCourseTypeId;
    const typeName = this.courseForm.controls['type'].value;
    const name = this.courseForm.controls['name'].value;
    const unitsMinutes = this.courseForm.controls['hours'].value * 60 
      + this.courseForm.controls['minutes'].value;

    const course = {
      CourseId: this.originalCourse.CourseId,
      CourseTypeId: this.originalCourse.CourseType.CourseTypeId === typeId ? undefined : typeId,
      Name: this.originalCourse.Name === name ? undefined : name,
      UnitsMinutes: this.originalCourse.UnitsMinutes === unitsMinutes ? undefined : unitsMinutes
    };

    const connectionId = this.signalrService.connection.connectionId;
    var isLocked = false;
    if (course.UnitsMinutes) {
      if (!connectionId) {
        return;
      }

      try {
        const lockingResult = await this.signalrService.LockAllCourseEditionsForCourse(course.CourseId).toPromise();
        
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
    this.administratorApiService.UpdateCourse(course, connectionId ?? '').pipe(finalize(async () => {
      if (isLocked) {
        try {
          const unlockingResult = await this.signalrService.UnlockAllCourseEditionsForCourse(course.CourseId).toPromise();
    
          if (unlockingResult.StatusCode >= 400) {
            throw unlockingResult;
          }
  
        } catch (error:any) {
  
        }
      }
    })).subscribe(() => {
      this.originalCourse = new Course(
        this.originalCourse.CourseId, new CourseType(
          typeId, typeName, '#ffffff'
        ), name, unitsMinutes
      );
      this.buildForm(this.originalCourse);
      this.disableForm();
      
      this.onChange.emit();
      
      this.snackBar.open("Successfully updated course.", "OK");
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
    if (!this.courseForm.valid) {
      return;
    }

    const typeId = this.modifiableCourseTypeId;
    const name = this.courseForm.controls['name'].value;
    const unitsMinutes = this.courseForm.controls['hours'].value * 60 
      + this.courseForm.controls['minutes'].value;

    const course = {
      CourseId: this.originalCourse.CourseId,
      CourseTypeId: this.originalCourse.CourseType.CourseTypeId === typeId ? undefined : typeId,
      Name: this.originalCourse.Name === name ? undefined : name,
      UnitsMinutes: this.originalCourse.UnitsMinutes === unitsMinutes ? undefined : unitsMinutes
    };

    this.administratorApiService.CreateCourse(course).subscribe((response) => {
      this.onCreate.emit(response.CourseId);
      
      this.snackBar.open("Successfully created course.", "OK");
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
    this.administratorApiService.RemoveCourse(this.originalCourse.CourseId).subscribe(() => {
      this.onRemove.emit();
      
      this.snackBar.open("Successfully removed course.", "OK");
    }, response => {
      if (response.error?.error?.message != undefined) {
        this.snackBar.open(response.error.error.message, "OK");
      } else if (typeof response.error !== 'object') {
        this.snackBar.open(response.error, "OK");
      }
    });
  }
}
