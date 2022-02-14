import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { forkJoin } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { CoordinatorBasic } from 'src/app/others/Accounts';
import { CourseEditionInfo } from 'src/app/others/CourseInfo';
import { GroupInfo } from 'src/app/others/Group';
import { ResourceNode } from 'src/app/others/ResourcesTree';
import { AdministratorApiService } from 'src/app/services/AdministratorApiService/administrator-api.service';
import { ScheduleDesignerApiService } from 'src/app/services/ScheduleDesignerApiService/schedule-designer-api.service';
import { SignalrService } from 'src/app/services/SignalrService/signalr.service';

/**
 * Komponent zawierający widok obszaru roboczego panelu administracyjnego
 * dla sekcji wprowadzania i modyfikowania danych na temat edycji zajęć.
 */
@Component({
  selector: 'app-course-edition-field',
  templateUrl: './course-edition-field.component.html',
  styleUrls: ['./course-edition-field.component.css']
})
export class CourseEditionFieldComponent implements OnInit {

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
  /**
   * Metoda ustawiająca rezultat wyboru pochodzący z drugiego drzewka zasobów.
   * Po ustawieniu rezultatu wywoływana jest odpowiednia metoda 
   * - przypisania grupy do edycji zajęć lub prowadzącego.
   */
  @Input() set selectedResult(value: {type: string, node: ResourceNode} | null) {
    if (value == null || value == undefined) {
      return;
    }
    
    this._selectedResult = value;

    switch (value.type) {
      case 'user': {
        this.AddCoordinator(Number.parseInt(value.node.item.id!), value.node.item.name);
      } break;
      case 'group': {
        this.AddGroup(Number.parseInt(value.node.item.id!))
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

  /** Informacje o pobranym zasobie edycji zajęć z systemu 
   * (posiada odpowiednie informacje w przypadku trybu podglądu). 
   */
  originalCourseEdition: CourseEditionInfo;
  /** Informacje o przypisanych prowadzących do wyświetlonego zasobu edycji zajęć. */
  coordinators: CoordinatorBasic[];
  /** Informacje o przypisanych grupach do wyświetlonego zasobu edycji zajęć. */
  groups: GroupInfo[];
  
  /** Wartości początkowe formularza modyfikacji zasobu w celu 
   * możliwości późniejszego ich zresetowania. */
  originalValues: any;
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
  courseEditionForm: FormGroup;

  ngOnInit(): void {
  }

  private getIds(id: string): number[] {
    const indexes = id.split(',');
    const parsedFirst = Number.parseInt(indexes[0]);
    const parsedSecond = Number.parseInt(indexes[1]);
    return [
      isNaN(parsedFirst) ? -1 : parsedFirst,
      isNaN(parsedSecond) ? -1 : parsedSecond,
    ];
  }

  /**
   * Metoda budująca formularz z danymi początkowymi podanymi w parametrze.
   * @param courseEdition Dane początkowe zbudowanego formularza
   */
  private buildForm(courseEdition: CourseEditionInfo) {
    this.courseEditionForm = new FormGroup({
      course: new FormControl(courseEdition.Name, [Validators.required]),
      name: new FormControl(courseEdition.CourseEditionName, [Validators.required]),
    });
    this.originalValues = this.courseEditionForm.value;
    this.courseEditionForm.controls['course'].disable();
  }

  /** Metoda wyłączająca możliwość modyfikacji formularza. */
  private disableForm() {
    for (var controlName in this.courseEditionForm.controls) {
      this.courseEditionForm.controls[controlName].disable();
    }
    this.isModifying = false;
  }

  /** Metoda włączająca możliwość modyfikacji formularza. */
  private enableForm() {
    for (var controlName in this.courseEditionForm.controls) {
      this.courseEditionForm.controls[controlName].enable();
    }
    this.courseEditionForm.controls['course'].disable();
    this.isModifying = true;
  }

  /**
   * Metoda ładująca dane wymagane do wyświetlenia obszaru roboczego.
   * Różnią się one w zależności od trybu widoku - dodawania lub podglądu.
   */
  private loadView() {
    this.loading = true;

    if (this._data.actionType === 'view') {
      const ids = this.getIds(this._data.id!);
      forkJoin([
        this.scheduleDesignerApiService.GetCourseEditionBasicInfo(ids[0], ids[1]),
        this.scheduleDesignerApiService.GetCourseEditionCoordinatorsBasic(ids[1]),
        this.scheduleDesignerApiService.GetCourseEditionGroupsBasic(ids[1])
      ]).subscribe(([courseEdition, coordinators, groupBasics]) => {
        this.scheduleDesignerApiService.GetGroupsInfo(groupBasics.map(g => g.GroupId)).subscribe(groups => {
          this.originalCourseEdition = courseEdition;
          this.coordinators = coordinators;
          this.groups = groups;
          this.buildForm(this.originalCourseEdition);
          
          this.disableForm();

          this.loading = false;
        }, () => {
          this.snackBar.open("Could not find course edition.", "OK");
        });
      }, () => {
        this.snackBar.open("Could not find course edition.", "OK");
      });
    } else if (this._data.actionType === 'add') {
      const courseId = Number.parseInt(this._data.id!);
      this.scheduleDesignerApiService.GetCourse(courseId).subscribe(course => {
        this.originalCourseEdition = new CourseEditionInfo(
          course.CourseId, 0, course.CourseType.CourseTypeId, course.Name, ''
        );
        this.coordinators = [];
        this.groups = [];
        this.buildForm(this.originalCourseEdition);
  
        this.loading = false;
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
    return this.originalCourseEdition.CourseEditionName === this.courseEditionForm.controls['name'].value;
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
   * i załadowanie drugiego drzewka zasobów informacjami o prowadzących,
   * których można przypisać do edycji zajęć.
   */
  SelectCoordinator() {
    if (this.treeVisible.type === 'coordinator') {
      this.treeVisible.value = !this.treeVisible.value;
    } else {
      this.treeVisible.type = 'coordinator';
      this.treeVisible.value = true;
    }
    this.onSelect.emit({
      type: 'coordinators', 
      header: 'Assign new coordinator for course edition:', 
      visible: this.treeVisible.value, 
      excludeTypes: [], 
      excludeIds: this.coordinators.map(c => c.UserId.toString())
    });
  }

  /**
   * Metoda wysyłająca zdarzenie powodujące wyświetlenie (bądź ukrycie) 
   * i załadowanie drugiego drzewka zasobów informacjami o grupach,
   * które można przypisać do edycji zajęć.
   */
  SelectGroup() {
    if (this.treeVisible.type === 'group') {
      this.treeVisible.value = !this.treeVisible.value;
    } else {
      this.treeVisible.type = 'group';
      this.treeVisible.value = true;
    }
    var excludeIds: string[] = [];
    this.groups.forEach(group => {
      var allGroupsIds: string[] = [];
      allGroupsIds.push(group.GroupId.toString());
      allGroupsIds = allGroupsIds.concat(group.ParentIds.map(g => g.toString()));
      allGroupsIds = allGroupsIds.concat(group.ChildIds.map(g => g.toString()));
      excludeIds.push(...[...new Set(allGroupsIds)]);
    });
    this.onSelect.emit({
      type: 'groups', 
      header: 'Assign new group for course edition:', 
      visible: this.treeVisible.value, 
      excludeTypes: [], 
      excludeIds: excludeIds
    });
  }

  /**
   * Metoda wysyłająca żądanie przypisania prowadzącego do edycji zajęć na serwer.
   * @param userId Identyfikator prowadzącego do przypisania
   * @param userName Pełna nazwa prowadzącego do przypisania
   */
  async AddCoordinator(userId: number, userName: string) {
    const connectionId = this.signalrService.connection.connectionId;
    var isLocked = false;
    try {
      const lockingResult = await this.signalrService.LockAllCoordinatorCourses(
        userId, this.originalCourseEdition.CourseId, this.originalCourseEdition.CourseEditionId
      ).toPromise();
      
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
    this.administratorApiService.AddCoordinatorCourseEdition(
      this.originalCourseEdition.CourseId, this.originalCourseEdition.CourseEditionId, userId, connectionId ?? ''
    ).pipe(finalize(async () => {
      if (isLocked) {
        try {
          const unlockingResult = await this.signalrService.UnlockAllCoordinatorCourses(
            userId, this.originalCourseEdition.CourseId, this.originalCourseEdition.CourseEditionId
          ).toPromise();
    
          if (unlockingResult.StatusCode >= 400) {
            throw unlockingResult;
          }
  
        } catch (error:any) {
  
        }
      }
    })).subscribe(() => {
      const coordinator = new CoordinatorBasic(userId, userName);
      this.coordinators.push(coordinator);
      this.coordinators.sort((a,b) => a.UserId - b.UserId);

      this.onListAdd.emit({ids: [userId.toString()], type: 'coordinators'});

      this.snackBar.open("Successfully added coordinator for the course edition.", "OK");
    }, response => {
      if (response.error?.error?.message != undefined) {
        this.snackBar.open(response.error.error.message, "OK");
      } else if (typeof response.error !== 'object') {
        this.snackBar.open(response.error, "OK");
      }
    });
  }

  /**
   * Metoda wysyłająca żądanie usunięcia przypisania prowadzącego do edycji zajęć na serwer.
   * @param userId Identyfikator przypisanego prowadzącego
   */
  RemoveCoordinator(userId: number) {
    this.administratorApiService.RemoveCoordinatorCourseEdition(
      this.originalCourseEdition.CourseId, this.originalCourseEdition.CourseEditionId, userId
      ).subscribe(response => {
      
      const index = this.coordinators.findIndex(c => c.UserId == userId);
      if (index != -1) {
        this.coordinators.splice(index, 1);
      }

      this.onListRemove.emit({ids: [userId.toString()], type: 'coordinators'});
      
      this.snackBar.open("Successfully removed coordinator from the course edition.", "OK");
    }, response => {
      if (response.error?.error?.message != undefined) {
        this.snackBar.open(response.error.error.message, "OK");
      } else if (typeof response.error !== 'object') {
        this.snackBar.open(response.error, "OK");
      }
    });
  }

  /**
   * Metoda wysyłająca żądanie przypisania grupy do edycji zajęć na serwer.
   * @param groupId Identyfikator grupy do przypisania
   */
  async AddGroup(groupId: number) {
    const connectionId = this.signalrService.connection.connectionId;
    var isLocked = false;
    try {
      const lockingResult = await this.signalrService.LockAllGroupCourses(
        groupId, this.originalCourseEdition.CourseId, this.originalCourseEdition.CourseEditionId
      ).toPromise();
      
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
    this.administratorApiService.AddGroupCourseEdition(
      this.originalCourseEdition.CourseId, this.originalCourseEdition.CourseEditionId, groupId, connectionId ?? ''
    ).pipe(finalize(async () => {
      if (isLocked) {
        try {
          const unlockingResult = await this.signalrService.UnlockAllGroupCourses(
            groupId, this.originalCourseEdition.CourseId, this.originalCourseEdition.CourseEditionId
          ).toPromise();
    
          if (unlockingResult.StatusCode >= 400) {
            throw unlockingResult;
          }
  
        } catch (error:any) {
  
        }
      }
    })).subscribe(async () => {
      const group = await this.scheduleDesignerApiService.GetGroupInfo(groupId).toPromise();
      this.groups.push(group);
      this.groups.sort((a,b) => a.GroupId - b.GroupId);

      var allGroupsIds: string[] = [];
      allGroupsIds.push(group.GroupId.toString());
      allGroupsIds = allGroupsIds.concat(group.ParentIds.map(g => g.toString()));
      allGroupsIds = allGroupsIds.concat(group.ChildIds.map(g => g.toString()));
      allGroupsIds = [...new Set(allGroupsIds)];
      
      this.onListAdd.emit({ids: allGroupsIds, type: 'groups'});

      this.snackBar.open("Successfully added group for the course edition.", "OK");
    }, response => {
      if (response.error?.error?.message != undefined) {
        this.snackBar.open(response.error.error.message, "OK");
      } else if (typeof response.error !== 'object') {
        this.snackBar.open(response.error, "OK");
      }
    });
  }

  /**
   * Metoda wysyłająca żądanie usunięcia przypisania grupy do edycji zajęć na serwer.
   * @param groupId Identyfikator przypisanej grupy
   */
  RemoveGroup(groupId: number) {
    this.administratorApiService.RemoveGroupCourseEdition(
      this.originalCourseEdition.CourseId, this.originalCourseEdition.CourseEditionId, groupId
      ).subscribe(response => {
      
      const index = this.groups.findIndex(g => g.GroupId == groupId);
      if (index != -1) {
        const group = this.groups[index];
        var allGroupsIds: string[] = [];
        allGroupsIds.push(group.GroupId.toString());
        allGroupsIds = allGroupsIds.concat(group.ParentIds.map(g => g.toString()));
        allGroupsIds = allGroupsIds.concat(group.ChildIds.map(g => g.toString()));
        allGroupsIds = [...new Set(allGroupsIds)];
        
        this.groups.splice(index, 1);
        this.onListRemove.emit({ids: allGroupsIds, type: 'groups'});
      }
      
      this.snackBar.open("Successfully removed group from the course edition.", "OK");
    }, response => {
      if (response.error?.error?.message != undefined) {
        this.snackBar.open(response.error.error.message, "OK");
      } else if (typeof response.error !== 'object') {
        this.snackBar.open(response.error, "OK");
      }
    });
  }
  
  Reset() {
    this.courseEditionForm.reset(this.originalValues);
  }

  Cancel() {
    this.Reset();
    this.disableForm();
  }

  /**
   * Metoda wysyłająca żądanie modyfikacji zasobu na serwer (zgodnie z danymi podanymi w formularzu).
   */
  Save() {
    if (!this.courseEditionForm.valid) {
      return;
    }

    const name = this.courseEditionForm.controls['name'].value;

    const courseEdition = {
      CourseId: this.originalCourseEdition.CourseId,
      CourseEditionId: this.originalCourseEdition.CourseEditionId,
      Name: this.originalCourseEdition.CourseEditionName === name ? undefined : name,
    };

    this.disableForm();
    this.administratorApiService.UpdateCourseEdition(courseEdition).subscribe(response => {
      this.originalCourseEdition = new CourseEditionInfo(
        this.originalCourseEdition.CourseId, this.originalCourseEdition.CourseEditionId,
        this.originalCourseEdition.CourseTypeId, this.originalCourseEdition.Name, name
      );
      this.buildForm(this.originalCourseEdition);
      this.disableForm();
      
      this.onChange.emit();
      
      this.snackBar.open("Successfully updated course edition.", "OK");
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
    if (!this.courseEditionForm.valid) {
      return;
    }

    const name = this.courseEditionForm.controls['name'].value;

    const courseEdition = {
      CourseId: this.originalCourseEdition.CourseId,
      CourseEditionId: this.originalCourseEdition.CourseEditionId,
      Name: this.originalCourseEdition.CourseEditionName === name ? undefined : name,
    };

    this.administratorApiService.CreateCourseEdition(courseEdition).subscribe((response) => {
      this.onCreate.emit(`${response.CourseId},${response.CourseEditionId}`);
      
      this.snackBar.open("Successfully created course edition.", "OK");
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
    this.administratorApiService.RemoveCourseEdition(this.originalCourseEdition.CourseId, this.originalCourseEdition.CourseEditionId).subscribe(() => {
      this.onRemove.emit();
      
      this.snackBar.open("Successfully removed course edition.", "OK");
    }, response => {
      if (response.error?.error?.message != undefined) {
        this.snackBar.open(response.error.error.message, "OK");
      } else if (typeof response.error !== 'object') {
        this.snackBar.open(response.error, "OK");
      }
    });
  }
}
