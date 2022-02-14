import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { ResourceNode } from 'src/app/others/ResourcesTree';
import { GroupInfo } from 'src/app/others/Group';
import { Student, StudentBasic } from 'src/app/others/Accounts';
import { ScheduleDesignerApiService } from 'src/app/services/ScheduleDesignerApiService/schedule-designer-api.service';
import { AdministratorApiService } from 'src/app/services/AdministratorApiService/administrator-api.service';
import { SignalrService } from 'src/app/services/SignalrService/signalr.service';
import { MatSnackBar } from '@angular/material/snack-bar';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { forkJoin } from 'rxjs';

/**
 * Komponent zawierający widok obszaru roboczego panelu administracyjnego
 * dla sekcji wprowadzania i modyfikowania danych na temat grupy studenckiej.
 */
@Component({
  selector: 'app-group-field',
  templateUrl: './group-field.component.html',
  styleUrls: ['./group-field.component.css']
})
export class GroupFieldComponent implements OnInit {

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
   * - przypisania studenta do grupy lub wyboru grupy nadrzędnej.
   */
  @Input() set selectedResult(value: {type: string, node: ResourceNode} | null) {
    if (value == null || value == undefined) {
      return;
    }
    
    this._selectedResult = value;

    switch (value.type) {
      case 'user': {
        this.AddStudent(Number.parseInt(value.node.item.id!), value.node.item.name);
      } break;
      case 'group': {
        if (this.groupForm) {
          this.groupForm.controls['parent'].setValue(value.node.item.id === 'root' ? '' : value.node.item.name);
        }
        this.modifiableParentGroupId = value.node.item.id === 'root' ? undefined : Number.parseInt(value.node.item.id!);

        this.treeVisible = {type: 'group', value: false};
        setTimeout(() => {
          this.onSelect.emit({type: '', header: '', visible: this.treeVisible.value, excludeTypes: [], excludeIds: []});
        });
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

  /** Informacje o pobranym zasobie grupy studenckiej z systemu 
   * (posiada odpowiednie informacje w przypadku trybu podglądu). 
   */
  originalGroup: GroupInfo;
  /** Identyfikator wybranej grupy nadrzędnej. */
  modifiableParentGroupId?: number;
  /** Informacje o studentach przypisanych do grupy. */
  groupStudents: StudentBasic[];
  /** Informacje o studentach przypisanych do grup podrzędnych. */
  childGroupsStudents: StudentBasic[];

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
  groupForm: FormGroup;

  ngOnInit(): void {

  }

  GetParentName(): string {
    return this.groupForm.controls['parent'].value;
  }

  /**
   * Metoda budująca formularz z danymi początkowymi podanymi w parametrze.
   * @param group Dane początkowe zbudowanego formularza
   */
  private buildForm(group: GroupInfo, isView: boolean) {
    this.groupForm = new FormGroup({
      parent: new FormControl(group.FullName.replace(group.BasicName, '')),
      name: new FormControl(group.BasicName, [Validators.required]),
    });
    this.originalValues = this.groupForm.value;
    if (!isView) {
      this.groupForm.controls['parent'].disable();
    }
  }

  /** Metoda wyłączająca możliwość modyfikacji formularza. */
  private disableForm() {
    for (var controlName in this.groupForm.controls) {
      this.groupForm.controls[controlName].disable();
    }
    this.isModifying = false;
  }

  /** Metoda włączająca możliwość modyfikacji formularza. */
  private enableForm(isView: boolean) {
    for (var controlName in this.groupForm.controls) {
      this.groupForm.controls[controlName].enable();
    }
    if (!isView) {
      this.groupForm.controls['parent'].disable();
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
      this.scheduleDesignerApiService.GetGroupInfo(Number.parseInt(this._data.id!)).subscribe(group => {
        forkJoin([
          this.administratorApiService.GetGroupsStudents(group.ChildIds.slice(1)),
          this.administratorApiService.GetGroupsStudents([group.GroupId]),
          this.administratorApiService.GetGroupRepresentativeRoles(group.GroupId)
        ]).subscribe(([childGroupsStudents, groupStudents, representativeIds]) => {
          this.originalGroup = group;
          this.modifiableParentGroupId = group.ParentIds[1];
          this.childGroupsStudents = childGroupsStudents;
          
          const childGroupsStudentIds = this.childGroupsStudents.map(e => e.UserId);
          this.groupStudents = groupStudents.filter(groupStudent => !childGroupsStudentIds.includes(groupStudent.UserId));
          
          this.childGroupsStudents.forEach(v => {
            if (representativeIds.includes(v.UserId)) {
              v.IsRepresentative = true;
            }
          });
          this.groupStudents.forEach(v => {
            if (representativeIds.includes(v.UserId)) {
              v.IsRepresentative = true;
            }
          });
          
          this.buildForm(this.originalGroup, true);

          this.disableForm();

          this.loading = false;
        }, () => {
          this.snackBar.open("Could not find group.", "OK");
        });
      }, () => {
        this.snackBar.open("Could not find group.", "OK");
      });
      
    } else if (this._data.actionType === 'add') {
      const groupId = Number.parseInt(this._data.id!);
      if (Number.isNaN(groupId)) {
        this.originalGroup = new GroupInfo(
          0, '', '', [0], [0]
        );
        this.modifiableParentGroupId = undefined;
        this.childGroupsStudents = [];
        this.groupStudents = [];
        
        this.buildForm(this.originalGroup, false);

        this.loading = false;
      } else {
        this.scheduleDesignerApiService.GetGroupInfo(groupId).subscribe(group => {
          this.originalGroup = new GroupInfo(
            group.GroupId, '', group.FullName, group.ParentIds, group.ChildIds
          );
          this.modifiableParentGroupId = group.ParentIds[0];
          this.childGroupsStudents = [];
          this.groupStudents = [];

          this.buildForm(this.originalGroup, false);

          this.loading = false;
        });
      }
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
    return this.originalGroup.BasicName === this.groupForm.controls['name'].value
      && this.originalGroup.FullName.replace(this.originalGroup.BasicName, '') === this.groupForm.controls['parent'].value;
  }

  /**
   * Metoda uruchamiająca tryb modyfikacji zasobu.
   */
  Modify(isView: boolean) {
    this.Reset();
    this.enableForm(isView);
  }

  /**
   * Metoda wysyłająca zdarzenie powodujące wyświetlenie (bądź ukrycie) 
   * i załadowanie drugiego drzewka zasobów informacjami o grupach,
   * które mogą zostać wybrane jako nadrzędne.
   */
  SelectParentGroup() {
    if (this.treeVisible.type === 'group') {
      this.treeVisible.value = !this.treeVisible.value;
    } else {
      this.treeVisible.type = 'group';
      this.treeVisible.value = true;
    }
    this.onSelect.emit({
      type: 'change-group', 
      header: 'Change parent of the group:', 
      visible: this.treeVisible.value, 
      excludeTypes: [], 
      excludeIds: this.originalGroup.ChildIds.map(e => e.toString())
    });
  }

  /**
   * Metoda wysyłająca zdarzenie powodujące wyświetlenie (bądź ukrycie) 
   * i załadowanie drugiego drzewka zasobów informacjami o studentach,
   * których można przypisać do grupy.
   */
  SelectStudent() {
    if (this.treeVisible.type === 'student') {
      this.treeVisible.value = !this.treeVisible.value;
    } else {
      this.treeVisible.type = 'student';
      this.treeVisible.value = true;
    }
    this.onSelect.emit({
      type: 'students', 
      header: 'Add new student for this group:', 
      visible: this.treeVisible.value, 
      excludeTypes: [], 
      excludeIds: this.groupStudents.map(s => s.UserId.toString())
        .concat(...this.childGroupsStudents.map(s => s.UserId.toString()))
    });
  }

  /**
   * Metoda wysyłająca żądanie przypisania studenta do grupy na serwer.
   * @param userId Identyfikator studenta do przypisania
   * @param userName Pełna nazwa studenta do przypisania
   */
  AddStudent(userId: number, userFullName: string) {
    this.administratorApiService.AddStudentToGroup(
      this.originalGroup.GroupId, userId
    ).subscribe(response => {
      const student = new StudentBasic(userId, userFullName);
      this.groupStudents.push(student);
      this.groupStudents.sort((a,b) => a.UserId - b.UserId);

      this.onListAdd.emit({ids: [userId.toString()], type: 'students'});
      
      this.snackBar.open("Successfully added student for this group.", "OK");
    }, response => {
      if (response.error?.error?.message != undefined) {
        this.snackBar.open(response.error.error.message, "OK");
      } else if (typeof response.error !== 'object') {
        this.snackBar.open(response.error, "OK");
      }
    });
  }

  /**
   * Metoda wysyłająca żądanie usunięcia przypisania studenta do grupy na serwer.
   * @param userId Identyfikator przypisanego studenta
   */
  RemoveStudent(userId: number) {
    this.administratorApiService.RemoveStudentFromGroup(
      this.originalGroup.GroupId, userId
    ).subscribe(response => {
      const index = this.groupStudents.findIndex(s => s.UserId == userId);
      if (index != -1) {
        this.groupStudents.splice(index, 1);
      }

      this.onListRemove.emit({ids: [userId.toString()], type: 'students'});
      
      this.snackBar.open("Successfully removed student from this group.", "OK");
    }, response => {
      if (response.error?.error?.message != undefined) {
        this.snackBar.open(response.error.error.message, "OK");
      } else if (typeof response.error !== 'object') {
        this.snackBar.open(response.error, "OK");
      }
    });
  }

  /**
   * Metoda wysyłająca żądanie nadania studentowi roli starosty grupy na serwer.
   * @param userId Identyfikator studenta
   */
  GiveRepresentativeRole(userId: number) {
    this.administratorApiService.GiveOrRemoveRepresentativeRole(
      this.originalGroup.GroupId, userId, true
    ).subscribe(response => {
      const index = this.groupStudents.findIndex(s => s.UserId == userId);
      if (index != -1) {
        this.groupStudents[index].IsRepresentative = true;
      } else {
        const index2 = this.childGroupsStudents.findIndex(s => s.UserId == userId);
        if (index2 != -1) {
          this.childGroupsStudents[index2].IsRepresentative = true;
        }
      }
      
      this.snackBar.open("Successfully given representative role for this student.", "OK");
    }, response => {
      if (response.error?.error?.message != undefined) {
        this.snackBar.open(response.error.error.message, "OK");
      } else if (typeof response.error !== 'object') {
        this.snackBar.open(response.error, "OK");
      }
    });
  }

  /**
   * Metoda wysyłająca żądanie odebrania studentowi roli starosty grupy na serwer.
   * @param userId Identyfikator studenta
   */
  TakeAwayRepresentativeRole(userId: number) {
    this.administratorApiService.GiveOrRemoveRepresentativeRole(
      this.originalGroup.GroupId, userId, false
    ).subscribe(response => {
      const index = this.groupStudents.findIndex(s => s.UserId == userId);
      if (index != -1) {
        this.groupStudents[index].IsRepresentative = false;
      } else {
        const index2 = this.childGroupsStudents.findIndex(s => s.UserId == userId);
        if (index2 != -1) {
          this.childGroupsStudents[index2].IsRepresentative = false;
        }
      }
      
      this.snackBar.open("Successfully took away representative role from this student.", "OK");
    }, response => {
      if (response.error?.error?.message != undefined) {
        this.snackBar.open(response.error.error.message, "OK");
      } else if (typeof response.error !== 'object') {
        this.snackBar.open(response.error, "OK");
      }
    });
  }

  Reset() {
    this.groupForm.reset(this.originalValues);
  }

  Cancel() {
    this.Reset();
    this.disableForm();
  }

  /**
   * Metoda wysyłająca żądanie modyfikacji zasobu na serwer (zgodnie z danymi podanymi w formularzu).
   */
  async Save() {
    if (!this.groupForm.valid) {
      return;
    }

    const parentId = this.modifiableParentGroupId ?? null;
    const originalParentId = this.originalGroup.ParentIds[1] ?? null;
    const parentName = this.groupForm.controls['parent'].value;
    const name = this.groupForm.controls['name'].value;

    const group = {
      GroupId: this.originalGroup.GroupId,
      Name: this.originalGroup.BasicName === name ? undefined : name,
      ParentGroupId: originalParentId === parentId ? undefined : parentId
    };

    const connectionId = this.signalrService.connection.connectionId;
    var isLocked = false;
    if (group.ParentGroupId || group.ParentGroupId == null) {
      if (!connectionId) {
        return;
      }

      try {
        const lockingResult = await this.signalrService.LockAllCoursesForGroupChange(this.originalGroup.GroupId, parentId != null ? parentId : undefined).toPromise();
        
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
    this.administratorApiService.UpdateGroup(group, connectionId ?? '').pipe(finalize(async () => {
      if (isLocked) {
        try {
          const unlockingResult = await this.signalrService.UnlockAllCoursesForGroupChange(this.originalGroup.GroupId, parentId != null ? parentId : undefined).toPromise();
    
          if (unlockingResult.StatusCode >= 400) {
            throw unlockingResult;
          }
  
        } catch (error:any) {
  
        }
      }
    })).subscribe(() => {
      if (group.ParentGroupId) {
        this.loadView();
      } else {
        this.originalGroup = new GroupInfo(
          this.originalGroup.GroupId, name, parentName + name, 
          this.originalGroup.ParentIds, this.originalGroup.ChildIds
        );
        this.buildForm(this.originalGroup, true);
        this.disableForm();
      }
      
      this.onChange.emit();
      
      this.snackBar.open("Successfully updated group.", "OK");
    }, response => {
      this.enableForm(true);
      
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
    if (!this.groupForm.valid) {
      return;
    }

    const parentId = this.modifiableParentGroupId ?? null;
    const name = this.groupForm.controls['name'].value;
    const group = {
      GroupId: this.originalGroup.GroupId,
      Name: this.originalGroup.BasicName === name ? undefined : name,
      ParentGroupId: parentId
    };

    this.administratorApiService.CreateGroup(group).subscribe((response) => {
      this.onCreate.emit(response.GroupId);
      
      this.snackBar.open("Successfully created group.", "OK");
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
    this.administratorApiService.RemoveGroup(this.originalGroup.GroupId).subscribe(() => {
      this.onRemove.emit();
      
      this.snackBar.open("Successfully removed group.", "OK");
    }, response => {
      if (response.error?.error?.message != undefined) {
        this.snackBar.open(response.error.error.message, "OK");
      } else if (typeof response.error !== 'object') {
        this.snackBar.open(response.error, "OK");
      }
    });
  }
}
