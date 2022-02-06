import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { RoomType } from 'src/app/others/Types';
import { AdministratorApiService } from 'src/app/services/AdministratorApiService/administrator-api.service';
import { ScheduleDesignerApiService } from 'src/app/services/ScheduleDesignerApiService/schedule-designer-api.service';

@Component({
  selector: 'app-room-type-field',
  templateUrl: './room-type-field.component.html',
  styleUrls: ['./room-type-field.component.css']
})
export class RoomTypeFieldComponent implements OnInit {

  private _data: {id: string|undefined, type: string, actionType: string};

  @Input() set data(value: {id: string|undefined, type: string, actionType: string}) {
    this._data = value;
    this.loadView();
  } get data(): {id: string|undefined, type: string, actionType: string} {
    return this._data;
  }

  @Output() onChange: EventEmitter<void> = new EventEmitter();
  @Output() onCreate: EventEmitter<string> = new EventEmitter();
  @Output() onRemove: EventEmitter<void> = new EventEmitter();

  originalRoomType: RoomType;

  originalValues: any;
  isModifying: boolean = false;

  loading: boolean | null = null;

  constructor(
    private scheduleDesignerApiService: ScheduleDesignerApiService,
    private administratorApiService: AdministratorApiService,
    private snackBar: MatSnackBar
  ) { }

  roomTypeForm: FormGroup;

  ngOnInit(): void {

  }

  private buildForm(roomType: RoomType) {
    this.roomTypeForm = new FormGroup({
      name: new FormControl(roomType.Name, [Validators.required]),
    });
    this.originalValues = this.roomTypeForm.value;
  }

  private disableForm() {
    for (var controlName in this.roomTypeForm.controls) {
      this.roomTypeForm.controls[controlName].disable();
    }
    this.isModifying = false;
  }

  private enableForm() {
    for (var controlName in this.roomTypeForm.controls) {
      this.roomTypeForm.controls[controlName].enable();
    }
    this.isModifying = true;
  }

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

  IsSameAsOriginal(): boolean {
    return this.originalRoomType.Name === this.roomTypeForm.controls['name'].value;
  }

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
