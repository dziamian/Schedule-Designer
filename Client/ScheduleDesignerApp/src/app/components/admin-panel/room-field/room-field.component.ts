import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ResourceNode } from 'src/app/others/ResourcesTree';
import { Room } from 'src/app/others/Room';
import { RoomType } from 'src/app/others/Types';
import { AdministratorApiService } from 'src/app/services/AdministratorApiService/administrator-api.service';
import { ScheduleDesignerApiService } from 'src/app/services/ScheduleDesignerApiService/schedule-designer-api.service';
import { SignalrService } from 'src/app/services/SignalrService/signalr.service';

/**
 * Komponent zawierający widok obszaru roboczego panelu administracyjnego
 * dla sekcji wprowadzania i modyfikowania danych na temat pokoju.
 */
@Component({
  selector: 'app-room-field',
  templateUrl: './room-field.component.html',
  styleUrls: ['./room-field.component.css']
})
export class RoomFieldComponent implements OnInit {

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
   * - wybrania typu pokoju.
   */
  @Input() set selectedResult(value: {type: string, node: ResourceNode} | null) {
    if (value == null || value == undefined) {
      return;
    }
    
    this._selectedResult = value;

    switch (value.type) {
      case 'room-type': {
        this.modifiableRoomTypeId = Number.parseInt(value.node.item.id!);
        if (this.roomForm) {
          this.roomForm.controls['type'].setValue(value.node.item.name);
        }

        this.treeVisible = {type: 'room-type', value: false};
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

  /** Emiter zdarzenia zapisu stanu modyfikacji zasobu. */
  @Output() onChange: EventEmitter<void> = new EventEmitter();
  /** 
   * Emiter zdarzenia dodania nowego zasobu do systemu.
   * Zdarzenie przechowuje informacje o identyfikatorach powstałego zasobu.
  */
  @Output() onCreate: EventEmitter<string> = new EventEmitter();
  /** Emiter zdarzenia usunięcia zasobu z systemu. */
  @Output() onRemove: EventEmitter<void> = new EventEmitter();

  /** Informacje o pobranym zasobie pokoju z systemu 
   * (posiada odpowiednie informacje w przypadku trybu podglądu). 
   */
  originalRoom: Room;

  /** Wartości początkowe formularza modyfikacji zasobu w celu 
   * możliwości późniejszego ich zresetowania. */
  originalValues: any;
  /** Identyfikator wybranego typu pokoju. */
  modifiableRoomTypeId: number;
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
    private snackBar: MatSnackBar
  ) { }

  /** Formularz modyfikacji oraz dodawania zasobu. */
  roomForm: FormGroup;

  ngOnInit(): void {

  }

  /**
   * Metoda budująca formularz z danymi początkowymi podanymi w parametrze.
   * @param room Dane początkowe zbudowanego formularza
   */
  private buildForm(room: Room) {
    this.roomForm = new FormGroup({
      type: new FormControl(room.RoomType.Name, [Validators.required]),
      name: new FormControl(room.Name, [Validators.required]),
      capacity: new FormControl(room.Capacity, [Validators.required])
    });
    this.originalValues = this.roomForm.value;
  }

  /** Metoda wyłączająca możliwość modyfikacji formularza. */
  private disableForm() {
    for (var controlName in this.roomForm.controls) {
      this.roomForm.controls[controlName].disable();
    }
    this.isModifying = false;
  }

  /** Metoda włączająca możliwość modyfikacji formularza. */
  private enableForm() {
    for (var controlName in this.roomForm.controls) {
      this.roomForm.controls[controlName].enable();
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
      this.scheduleDesignerApiService.GetRoom(Number.parseInt(this._data.id!)).subscribe((room) => {
        this.originalRoom = room;
        this.buildForm(this.originalRoom);
        
        this.disableForm();

        this.loading = false;
      }, () => {
        this.snackBar.open("Could not find room.", "OK");
      });
    } else if (this._data.actionType === 'add') {
      this.originalRoom = new Room(0);
      this.originalRoom.Capacity = 0;
      this.originalRoom.RoomType = new RoomType(0, '');
      this.buildForm(this.originalRoom);

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
    return this.originalRoom.RoomType.Name === this.roomForm.controls['type'].value 
      && this.originalRoom.Name === this.roomForm.controls['name'].value
      && this.originalRoom.Capacity === this.roomForm.controls['capacity'].value;
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
   * i załadowanie drugiego drzewka zasobów informacjami o typach pokojów,
   * które można wybrać.
   */
  SelectRoomType() {
    if (this.treeVisible.type === 'room-type') {
      this.treeVisible.value = !this.treeVisible.value;
    } else {
      this.treeVisible.type = 'room-type';
      this.treeVisible.value = true;
    }
    this.onSelect.emit({
      type: 'room-types', 
      header: 'Set type of the room:', 
      visible: this.treeVisible.value, 
      excludeTypes: [], 
      excludeIds: []
    });
  }

  Reset() {
    this.roomForm.reset(this.originalValues);
  }

  Cancel() {
    this.Reset();
    this.disableForm();
  }

  /**
   * Metoda wysyłająca żądanie modyfikacji zasobu na serwer (zgodnie z danymi podanymi w formularzu).
   */
  Save() {
    if (!this.roomForm.valid) {
      return;
    }

    const typeId = this.modifiableRoomTypeId;
    const typeName = this.roomForm.controls['type'].value;
    const name = this.roomForm.controls['name'].value;
    const capacity = this.roomForm.controls['capacity'].value;

    const room = {
      RoomId: this.originalRoom.RoomId,
      RoomTypeId: this.originalRoom.RoomType.RoomTypeId === typeId ? undefined : typeId,
      Name: this.originalRoom.Name === name ? undefined : name,
      Capacity: this.originalRoom.Capacity === capacity ? undefined : capacity
    };
    this.disableForm();
    this.administratorApiService.UpdateRoom(room).subscribe(() => {
      const newRoom = new Room(this.originalRoom.RoomId);
      newRoom.RoomType = new RoomType(typeId, typeName);
      newRoom.Capacity = capacity;
      this.originalRoom = newRoom;
      
      this.buildForm(this.originalRoom);
      this.disableForm();
      
      this.onChange.emit();
      
      this.snackBar.open("Successfully updated room.", "OK");
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
    if (!this.roomForm.valid) {
      return;
    }

    const typeId = this.modifiableRoomTypeId;
    const name = this.roomForm.controls['name'].value;
    const capacity = this.roomForm.controls['capacity'].value;

    const room = {
      RoomId: this.originalRoom.RoomId,
      RoomTypeId: this.originalRoom.RoomType.RoomTypeId === typeId ? undefined : typeId,
      Name: this.originalRoom.Name === name ? undefined : name,
      Capacity: this.originalRoom.Capacity === capacity ? undefined : capacity
    };

    this.administratorApiService.CreateRoom(room).subscribe((response) => {
      this.onCreate.emit(response.RoomId);
      
      this.snackBar.open("Successfully created room.", "OK");
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
    this.administratorApiService.RemoveRoom(this.originalRoom.RoomId).subscribe(() => {
      this.onRemove.emit();
      
      this.snackBar.open("Successfully removed room.", "OK");
    }, response => {
      if (response.error?.error?.message != undefined) {
        this.snackBar.open(response.error.error.message, "OK");
      } else if (typeof response.error !== 'object') {
        this.snackBar.open(response.error, "OK");
      }
    });
  }

}