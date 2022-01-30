import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { CourseType } from 'src/app/others/Types';
import { AdministratorApiService } from 'src/app/services/AdministratorApiService/administrator-api.service';
import { ScheduleDesignerApiService } from 'src/app/services/ScheduleDesignerApiService/schedule-designer-api.service';

@Component({
  selector: 'app-course-type-field',
  templateUrl: './course-type-field.component.html',
  styleUrls: ['./course-type-field.component.css']
})
export class CourseTypeFieldComponent implements OnInit {

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

  originalCourseType: CourseType;

  originalValues: any;
  isModifying: boolean = false;

  loading: boolean | null = null;

  constructor(
    private scheduleDesignerApiService: ScheduleDesignerApiService,
    private administratorApiService: AdministratorApiService,
    private snackBar: MatSnackBar
  ) { }

  courseTypeForm: FormGroup;

  ngOnInit(): void {

  }

  private buildForm(courseType: CourseType) {
    this.courseTypeForm = new FormGroup({
      name: new FormControl(courseType.Name, [Validators.required]),
      color: new FormControl(courseType.Color, [Validators.required])
    });
    this.originalValues = this.courseTypeForm.value;
  }

  private disableForm() {
    for (var controlName in this.courseTypeForm.controls) {
      this.courseTypeForm.controls[controlName].disable();
    }
    this.isModifying = false;
  }

  private enableForm() {
    for (var controlName in this.courseTypeForm.controls) {
      this.courseTypeForm.controls[controlName].enable();
    }
    this.isModifying = true;
  }

  private loadView() {
    this.loading = true;

    if (this._data.actionType === 'view') {
      this.scheduleDesignerApiService.GetCourseType(Number.parseInt(this._data.id!)).subscribe((courseType) => {
        this.originalCourseType = courseType;
        this.buildForm(this.originalCourseType);
        
        this.disableForm();

        this.loading = false;
      });
    } else if (this._data.actionType === 'add') {
      this.originalCourseType = new CourseType(
        0, '', '#ffffff'
      );
      this.buildForm(this.originalCourseType);

      this.loading = false;
    }
  }

  IsDifferentThanOriginal(): boolean {
    return this.originalCourseType.Name === this.courseTypeForm.controls['name'].value 
      && this.originalCourseType.Color === this.courseTypeForm.controls['color'].value;
  }

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
      
      if (response.error.error.message != undefined) {
        this.snackBar.open(response.error.error.message, "OK");
      } else if (typeof response.error !== 'object') {
        this.snackBar.open(response.error, "OK");
      }
    });
  }

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
      if (response.error.error.message != undefined) {
        this.snackBar.open(response.error.error.message, "OK");
      } else if (typeof response.error !== 'object') {
        this.snackBar.open(response.error, "OK");
      }
    });
  }

  Remove() {
    this.administratorApiService.RemoveCourseType(this.originalCourseType.CourseTypeId).subscribe(() => {
      this.onRemove.emit();
      
      this.snackBar.open("Successfully removed course type.", "OK");
    }, response => {
      if (response.error.error.message != undefined) {
        this.snackBar.open(response.error.error.message, "OK");
      } else if (typeof response.error !== 'object') {
        this.snackBar.open(response.error, "OK");
      }
    });
  }
}
