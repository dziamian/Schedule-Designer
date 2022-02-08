import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { finalize } from 'rxjs/operators';
import { UserInfo, SearchUser } from 'src/app/others/Accounts';
import { AdministratorApiService } from 'src/app/services/AdministratorApiService/administrator-api.service';
import { ScheduleDesignerApiService } from 'src/app/services/ScheduleDesignerApiService/schedule-designer-api.service';
import { SignalrService } from 'src/app/services/SignalrService/signalr.service';

@Component({
  selector: 'app-user-field',
  templateUrl: './user-field.component.html',
  styleUrls: ['./user-field.component.css']
})
export class UserFieldComponent implements OnInit {

  readonly PAGE_SIZE: number = 5;

  private _data: {id: string|undefined, type: string, actionType: string};

  @Input() set data(value: {id: string|undefined, type: string, actionType: string}) {
    this._data = value;
    this.loadView();
  } get data(): {id: string|undefined, type: string, actionType: string} {
    return this._data;
  }

  @Output() onCreate: EventEmitter<string> = new EventEmitter();
  @Output() onRemove: EventEmitter<void> = new EventEmitter();
  @Output() onRefresh: EventEmitter<void> = new EventEmitter();

  originalUserInfo: UserInfo;

  originalValues: any;
  isModifying: boolean = false;

  loading: boolean | null = null;
  resultsLoading: boolean = false;

  constructor(
    private scheduleDesignerApiService: ScheduleDesignerApiService,
    private administratorApiService: AdministratorApiService,
    private signalrService: SignalrService,
    private snackBar: MatSnackBar
  ) { }

  searchResults: SearchUser | null;
  searchStart: number = 0;
  currentPage: number = 0;
  maxPage: number = 0;
  currentQuery: string;

  searchForm: FormGroup;
  userInfoForm: FormGroup;

  ngOnInit(): void {
  }

  private buildSearchForm() {
    this.searchForm = new FormGroup({
      search: new FormControl('', [Validators.required])
    });
  }

  private buildAccountForm(userInfo: UserInfo) {
    this.userInfoForm = new FormGroup({
      firstName: new FormControl(userInfo.FirstName, [Validators.required]),
      lastName: new FormControl(userInfo.LastName, [Validators.required]),
      academicNumber: new FormControl(userInfo.AcademicNumber ?? ''),
      titleBefore: new FormControl(userInfo.TitleBefore ?? ''),
      titleAfter: new FormControl(userInfo.TitleAfter ?? ''),
      staff: new FormControl(userInfo.IsStaff),
      coordinator: new FormControl(userInfo.IsCoordinator),
      admin: new FormControl(userInfo.IsAdmin),
      student: new FormControl(userInfo.IsStudent)
    });
    this.originalValues = this.userInfoForm.value;
  }

  private disableForm() {
    for (var controlName in this.userInfoForm.controls) {
      this.userInfoForm.controls[controlName].disable();
    }
    this.isModifying = false;
  }

  private enableForm() {
    for (var controlName in this.userInfoForm.controls) {
      this.userInfoForm.controls[controlName].enable();
    }
    this.isModifying = true;
  }

  private loadView() {
    this.loading = true;

    if (this._data.actionType === 'view') {
      this.administratorApiService.GetUserAccount(Number.parseInt(this._data.id!)).subscribe(userInfo => {
        this.originalUserInfo = userInfo;

        this.buildAccountForm(this.originalUserInfo);

        this.disableForm();

        this.loading = false;
      }, () => {
        this.snackBar.open("Could not find user.", "OK");
      });
    } else if (this._data.actionType === 'add') {
      this.searchResults = null;
      this.searchStart = 0;
      this.currentQuery = '';
      
      this.buildSearchForm();
      
      this.loading = false;
    } else {
      this.loading = false;
    }
  }
  
  IsSameAsOriginal(): boolean {
    return this.originalUserInfo.FirstName === this.userInfoForm.controls['firstName'].value
      && this.originalUserInfo.LastName === this.userInfoForm.controls['lastName'].value
      && this.originalUserInfo.AcademicNumber === this.userInfoForm.controls['academicNumber'].value
      && this.originalUserInfo.TitleBefore === this.userInfoForm.controls['titleBefore'].value
      && this.originalUserInfo.TitleAfter === this.userInfoForm.controls['titleAfter'].value
      && this.originalUserInfo.IsStaff === this.userInfoForm.controls['staff'].value
      && ((this.userInfoForm.controls['staff'].value
        && this.originalUserInfo.IsAdmin === this.userInfoForm.controls['admin'].value
        && this.originalUserInfo.IsCoordinator === this.userInfoForm.controls['coordinator'].value)
        || !this.userInfoForm.controls['staff'].value)
      && this.originalUserInfo.IsStudent === this.userInfoForm.controls['student'].value;
  }

  Modify() {
    this.Reset();
    this.enableForm();
  }

  Reset() {
    this.userInfoForm.reset(this.originalValues);
  }

  Cancel() {
    this.Reset();
    this.disableForm();
  }

  async Save() {
    if (!this.userInfoForm.valid) {
      return;
    }

    const firstName = this.userInfoForm.controls['firstName'].value;
    const lastName = this.userInfoForm.controls['lastName'].value;
    const academicNumber = this.userInfoForm.controls['academicNumber'].value;
    const titleBefore = this.userInfoForm.controls['titleBefore'].value;
    const titleAfter = this.userInfoForm.controls['titleAfter'].value;
    const staff = this.userInfoForm.controls['staff'].value;
    const coordinator = staff ? this.userInfoForm.controls['coordinator'].value : undefined;
    const admin = staff ? this.userInfoForm.controls['admin'].value : undefined;
    const student = this.userInfoForm.controls['student'].value;

    const userInfo = {
      UserId: this.originalUserInfo.UserId,
      FirstName: this.originalUserInfo.FirstName === firstName ? undefined : firstName,
      LastName: this.originalUserInfo.LastName === lastName ? undefined : lastName,
      AcademicNumber: this.originalUserInfo.AcademicNumber === academicNumber ? undefined : academicNumber,
      TitleBefore: this.originalUserInfo.TitleBefore === titleBefore ? undefined : titleBefore,
      TitleAfter: this.originalUserInfo.TitleAfter === titleAfter ? undefined : titleAfter,
      IsStudent: this.originalUserInfo.IsStudent === student ? undefined : student,
      IsStaff: this.originalUserInfo.IsStaff === staff ? undefined : staff,
      IsCoordinator: this.originalUserInfo.IsCoordinator === coordinator ? undefined : coordinator,
      IsAdmin: this.originalUserInfo.IsAdmin === admin ? undefined : admin,
    }

    this.disableForm();
    this.administratorApiService.UpdateUser(userInfo).subscribe(() => {
      this.onRefresh.emit();
      
      this.snackBar.open("Successfully updated user.", "OK");

      this.loadView();
    }, response => {
      this.enableForm();
      
      if (response.error?.error?.message != undefined) {
        this.snackBar.open(response.error.error.message, "OK");
      } else if (typeof response.error !== 'object') {
        this.snackBar.open(response.error, "OK");
      }
    });
  }

  Remove() {
    this.administratorApiService.RemoveUser(this.originalUserInfo.UserId).subscribe(() => {
      this.onRemove.emit();
      
      this.snackBar.open("Successfully removed user.", "OK");
    }, response => {
      if (response.error?.error?.message != undefined) {
        this.snackBar.open(response.error.error.message, "OK");
      } else if (typeof response.error !== 'object') {
        this.snackBar.open(response.error, "OK");
      }
    });
  }

  Create(userId: number) {
    this.administratorApiService.CreateAccountFromUsos(userId).subscribe(response => {
      this.onCreate.emit(`${userId}`);
      
      this.snackBar.open("Successfully created account for this user.", "OK");
    }, response => {
      if (response.error?.error?.message != undefined) {
        this.snackBar.open(response.error.error.message, "OK");
      } else if (typeof response.error !== 'object') {
        this.snackBar.open(response.error, "OK");
      }
    });
  }

  Search() {
    if (!this.searchForm.valid) {
      return;
    }
    if (this.currentQuery === this.searchForm.controls['search'].value) {
      return;
    }

    this.resultsLoading = true;
    this.searchResults = null;

    this.currentQuery = this.searchForm.controls['search'].value;

    this.administratorApiService.SearchForUserFromUsos(
      this.currentQuery, this.PAGE_SIZE, 0
    ).pipe(finalize(() => {
      this.resultsLoading = false;
    })).subscribe(result => {
      this.searchResults = result;
      this.searchStart = 0;
      this.currentPage = 1;
      this.maxPage = this.searchResults.NextPage ? this.currentPage + 1 : this.currentPage;
    }, response => {
      if (response.error?.error?.message != undefined) {
        this.snackBar.open(response.error.error.message, "OK");
      } else if (typeof response.error !== 'object') {
        this.snackBar.open(response.error, "OK");
      }
    });
  }

  async SearchNextPage() {
    this.resultsLoading = true;
    this.searchResults = null;

    try {
      const result = await this.administratorApiService.SearchForUserFromUsos(
        this.currentQuery, this.PAGE_SIZE, this.searchStart + this.PAGE_SIZE
      ).toPromise();

      this.searchResults = result;
      this.searchStart += this.PAGE_SIZE;
      ++this.currentPage;
      if (this.maxPage <= this.currentPage) {
        this.maxPage = this.searchResults.NextPage ? this.currentPage + 1 : this.currentPage;
      }
    } catch (response : any) {
      if (response.error?.error?.message != undefined) {
        this.snackBar.open(response.error.error.message, "OK");
      } else if (typeof response.error !== 'object') {
        this.snackBar.open(response.error, "OK");
      }
    }
    this.resultsLoading = false;
  }

  async SearchPreviousPage() {
    this.resultsLoading = true;
    this.searchResults = null;

    try {
      const result = await this.administratorApiService.SearchForUserFromUsos(
        this.currentQuery, this.PAGE_SIZE, this.searchStart - this.PAGE_SIZE
      ).toPromise();

      this.searchResults = result;
      this.searchStart -= this.PAGE_SIZE;
      --this.currentPage;
    } catch (response : any) {
      if (response.error?.error?.message != undefined) {
        this.snackBar.open(response.error.error.message, "OK");
      } else if (typeof response.error !== 'object') {
        this.snackBar.open(response.error, "OK");
      }
    }
    this.resultsLoading = false;
  }
}
