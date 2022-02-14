import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { RoomType } from 'src/app/others/Types';
import { AdministratorApiService } from 'src/app/services/AdministratorApiService/administrator-api.service';
import { ScheduleDesignerApiService } from 'src/app/services/ScheduleDesignerApiService/schedule-designer-api.service';

/**
 * Komponent zawierający widok obszaru roboczego panelu administracyjnego
 * dla sekcji wprowadzania i modyfikowania danych na temat typu pokoju.
 */
@Component({
  selector: 'app-room-type-field',
  templateUrl: './room-type-field.component.html',
  styleUrls: ['./room-type-field.component.css']
})
export class RoomTypeFieldComponent implements OnInit {

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

  /** Informacje o pobranym zasobie typu pokoju z systemu 
   * (posiada odpowiednie informacje w przypadku trybu podglądu). 
   */
  originalRoomType: RoomType;

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
  roomTypeForm: FormGroup;

  ngOnInit(): void {

  }

  /**
   * Metoda budująca formularz z danymi początkowymi podanymi w parametrze.
   * @param roomType Dane początkowe zbudowanego formularza
   */
  private buildForm(roomType: RoomType) {
    this.roomTypeForm = new FormGroup({
      name: new FormControl(roomType.Name, [Validators.required]),
    });
    this.originalValues = this.roomTypeForm.value;
  }

  /** Metoda wyłączająca możliwość modyfikacji formularza. */
  private disableForm() {
    for (var controlName in this.roomTypeForm.controls) {
      this.roomTypeForm.controls[controlName].disable();
    }
    this.isModifying = false;
  }

  /** Metoda włączająca możliwość modyfikacji formularza. */
  private enableForm() {
    for (var controlName in this.roomTypeForm.controls) {
      this.roomTypeForm.controls[controlName].enable();
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
      this.scheduleDesignerApiService.GetRoomType(Number.parseInt(this._data.id!)).subscribe((roomType) => {
        this.originalRoomType = roomType;
        this.buildForm(this.originalRoomType);
        
        this.disableForm();

        this.loading = false;
      }, () => {
        this.snackBar.open("Could not find room type.", "OK");
      });
    } else if (this._data.actionType === 'add') {
      this.originalRoomType = new RoomType(
        0, ''
      );
      this.buildForm(this.originalRoomType);

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
    return this.originalRoomType.Name === this.roomTypeForm.controls['name'].value;
  }

  /**
   * Metoda uruchamiająca tryb modyfikacji zasobu.
   */
  Modify() {
    this.Reset();
    this.enableForm();
  }

  Reset() {
    this.roomTypeForm.reset(this.originalValues);
  }

  Cancel() {
    this.Reset();
    this.disableForm();
  }

  /**
   * Metoda wysyłająca żądanie modyfikacji zasobu na serwer (zgodnie z danymi podanymi w formularzu).
   */
  Save() {
    if (!this.roomTypeForm.valid) {
      return;
    }

    const name = this.roomTypeForm.controls['name'].value;

    const roomType = {
      RoomTypeId: this.originalRoomType.RoomTypeId,
      Name: this.originalRoomType.Name === name ? undefined : name,
    };
    this.disableForm();
    this.administratorApiService.UpdateRoomType(roomType).subscribe(() => {
      this.originalRoomType = new RoomType(
        this.originalRoomType.RoomTypeId, name
      );
      this.buildForm(this.originalRoomType);
      this.disableForm();
      
      this.onChange.emit();
      
      this.snackBar.open("Successfully updated room type.", "OK");
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
    if (!this.roomTypeForm.valid) {
      return;
    }

    const name = this.roomTypeForm.controls['name'].value;

    const roomType = {
      RoomTypeId: this.originalRoomType.RoomTypeId,
      Name: this.originalRoomType.Name === name ? undefined : name,
    };
    this.administratorApiService.CreateRoomType(roomType).subscribe((response) => {
      this.onCreate.emit(response.RoomTypeId);
      
      this.snackBar.open("Successfully created room type.", "OK");
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
    this.administratorApiService.RemoveRoomType(this.originalRoomType.RoomTypeId).subscribe(() => {
      this.onRemove.emit();
      
      this.snackBar.open("Successfully removed room type.", "OK");
    }, response => {
      if (response.error?.error?.message != undefined) {
        this.snackBar.open(response.error.error.message, "OK");
      } else if (typeof response.error !== 'object') {
        this.snackBar.open(response.error, "OK");
      }
    });
  }
}
