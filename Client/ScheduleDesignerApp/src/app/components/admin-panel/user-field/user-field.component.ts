import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { finalize } from 'rxjs/operators';
import { Account, SearchUser, User } from 'src/app/others/Accounts';
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

  @Output() onChange: EventEmitter<void> = new EventEmitter();
  @Output() onCreate: EventEmitter<string> = new EventEmitter();
  @Output() onRemove: EventEmitter<void> = new EventEmitter();
  @Output() onRefresh: EventEmitter<void> = new EventEmitter();

  originalAccount: Account;

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
  accountForm: FormGroup;

  ngOnInit(): void {
  }

  private buildSearchForm() {
    this.searchForm = new FormGroup({
      search: new FormControl('', [Validators.required])
    });
  }

  private buildAccountForm(account: Account) {
    this.accountForm = new FormGroup({
      firstName: new FormControl(account.User.FirstName, [Validators.required]),
      lastName: new FormControl(account.User.LastName, [Validators.required]),
      staff: new FormControl(account.Staff != null),
      coordinator: new FormControl(account.Coordinator != null),
      titleBefore: new FormControl(account.Coordinator?.Titles.TitleBefore ?? ''),
      titleAfter: new FormControl(account.Coordinator?.Titles.TitleAfter ?? ''),
      admin: new FormControl(account.Staff?.IsAdmin ?? false),
      student: new FormControl(account.Student != null),
      studentNumber: new FormControl(account.Student?.StudentNumber ?? '')
    });
    this.originalValues = this.accountForm.value;
  }

  private disableForm() {
    for (var controlName in this.accountForm.controls) {
      this.accountForm.controls[controlName].disable();
    }
    this.isModifying = false;
  }

  private enableForm() {
    for (var controlName in this.accountForm.controls) {
      this.accountForm.controls[controlName].enable();
    }
    this.isModifying = true;
  }

  private loadView() {
    this.loading = true;

    if (this._data.actionType === 'view') {
      this.administratorApiService.GetUserAccount(Number.parseInt(this._data.id!)).subscribe(account => {
        this.originalAccount = account;

        this.buildAccountForm(this.originalAccount);

        this.disableForm();

        this.loading = false;
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
    console.log()
    return this.originalAccount.User.FirstName === this.accountForm.controls['firstName'].value
      && this.originalAccount.User.LastName === this.accountForm.controls['lastName'].value
      && !!this.originalAccount.Staff === this.accountForm.controls['staff'].value
      && ((this.accountForm.controls['staff'].value 
        && this.originalAccount.Staff?.IsAdmin === this.accountForm.controls['admin'].value
        && !!this.originalAccount.Coordinator === this.accountForm.controls['coordinator'].value
        && ((this.accountForm.controls['coordinator'].value 
          && this.originalAccount.Coordinator?.Titles.TitleBefore === this.accountForm.controls['titleBefore'].value
          && this.originalAccount.Coordinator?.Titles.TitleAfter === this.accountForm.controls['titleAfter'].value) 
            || !this.accountForm.controls['coordinator'].value)
        || !this.accountForm.controls['staff'].value))
      && !!this.originalAccount.Student === this.accountForm.controls['student'].value
      && ((this.accountForm.controls['student'].value
        && this.originalAccount.Student?.StudentNumber === this.accountForm.controls['studentNumber'].value)
        || !this.accountForm.controls['student'].value);
  }

  Modify() {
    this.Reset();
    this.enableForm();
  }

  Reset() {
    this.accountForm.reset(this.originalValues);
  }

  Cancel() {
    this.Reset();
    this.disableForm();
  }

  async Save() {
    if (!this.accountForm.valid) {
      return;
    }

    const firstName = this.accountForm.controls['firstName'].value;
    const lastName = this.accountForm.controls['lastName'].value;
    const staff = this.accountForm.controls['staff'].value;
    const coordinator = this.accountForm.controls['coordinator'].value;
    const titleBefore = coordinator ? this.accountForm.controls['titleBefore'].value : undefined;
    const titleAfter = coordinator ? this.accountForm.controls['titleAfter'].value : undefined;
    const admin = staff ? this.accountForm.controls['admin'].value : undefined;
    const student = this.accountForm.controls['student'].value;
    const studentNumber = student ? this.accountForm.controls['studentNumber'].value : undefined;

    const user = {
      UserId: this.originalAccount.User.UserId,
      FirstName: this.originalAccount.User.FirstName === firstName ? undefined : firstName,
      LastName: this.originalAccount.User.LastName === lastName ? undefined : lastName
    };
    const staffRole = {
      UserId: this.originalAccount.User.UserId,
      IsAdmin: this.originalAccount.Staff?.IsAdmin === admin ? undefined : admin
    };
    const coordinatorRole = {
      UserId: this.originalAccount.User.UserId,
      TitleBefore: this.originalAccount.Coordinator?.Titles.TitleBefore === titleBefore ? undefined : titleBefore,
      TitleAfter: this.originalAccount.Coordinator?.Titles.TitleAfter === titleAfter ? undefined : titleAfter,
    };
    const studentRole = {
      UserId: this.originalAccount.User.UserId,
      StudentNumber: this.originalAccount.Student?.StudentNumber === studentNumber ? undefined : studentNumber
    };

    this.disableForm();
    const errorsEntities: string[] = [];
    var additionalInfo: string = '';
    if (user.FirstName != undefined || user.LastName != undefined) {
      try {
        await this.administratorApiService.UpdateUser(user).toPromise();
      } catch (response: any) {
        errorsEntities.push("User");
        if (additionalInfo.length == 0) {
          if (response.error.error.message != undefined) {
            additionalInfo = response.error.error.message;
          } else if (typeof response.error !== 'object') {
            additionalInfo = response.error;
          }
        }
      }
    }

    if (!!this.originalAccount.Coordinator != coordinator) {
      try {
        if (coordinator) {
          await this.administratorApiService.CreateCoordinator(coordinatorRole).toPromise();
        } else {
          await this.administratorApiService.RemoveCoordinator(coordinatorRole.UserId).toPromise();
        }
      } catch (response: any) {
        errorsEntities.push("Coordinator");
        if (additionalInfo.length == 0) {
          if (response.error.error.message != undefined) {
            additionalInfo = response.error.error.message;
          } else if (typeof response.error !== 'object') {
            additionalInfo = response.error;
          }
        }
      }
    } else if (coordinatorRole?.TitleBefore != undefined || coordinatorRole?.TitleAfter != undefined) {
      try {
        await this.administratorApiService.UpdateCoordinator(coordinatorRole).toPromise();
      } catch (response: any) {
        errorsEntities.push("Coordinator");
        if (additionalInfo.length == 0) {
          if (response.error.error.message != undefined) {
            additionalInfo = response.error.error.message;
          } else if (typeof response.error !== 'object') {
            additionalInfo = response.error;
          }
        }
      }
    }
    
    if (!!this.originalAccount.Staff != staff) {
      var coordinatorError = false;
      try {
        if (staff) {
          await this.administratorApiService.CreateStaff(staffRole).toPromise();
        } else {
          await this.administratorApiService.RemoveStaff(staffRole.UserId).toPromise();
          if (coordinator) {
            coordinatorError = true;
            await this.administratorApiService.RemoveCoordinator(coordinatorRole.UserId).toPromise();
            coordinatorError = false;
          }
        }
      } catch (response: any) {
        if (coordinatorError) {
          errorsEntities.push("Coordinator");
        } else {
          errorsEntities.push("Staff");
        }
        if (additionalInfo.length == 0) {
          if (response.error.error.message != undefined) {
            additionalInfo = response.error.error.message;
          } else if (typeof response.error !== 'object') {
            additionalInfo = response.error;
          }
        }
      }
    } else if (staffRole?.IsAdmin != undefined) {
      try {
        await this.administratorApiService.UpdateStaff(staffRole).toPromise();
      } catch (response: any) {
        errorsEntities.push("Staff");
        if (additionalInfo.length == 0) {
          if (response.error.error.message != undefined) {
            additionalInfo = response.error.error.message;
          } else if (typeof response.error !== 'object') {
            additionalInfo = response.error;
          }
        }
      }
    }
    
    if (!!this.originalAccount.Student != student) {
      try {
        if (student) {
          await this.administratorApiService.CreateStudent(studentRole).toPromise();
        } else {
          await this.administratorApiService.RemoveStudent(studentRole.UserId).toPromise();
        }
      } catch (response: any) {
        errorsEntities.push("Student");
        if (additionalInfo.length == 0) {
          if (response.error.error.message != undefined) {
            additionalInfo = response.error.error.message;
          } else if (typeof response.error !== 'object') {
            additionalInfo = response.error;
          }
        }
      }
    } else if (studentRole?.StudentNumber != undefined) {
      try {
        await this.administratorApiService.UpdateStudent(studentRole).toPromise();
      } catch (response: any) {
        errorsEntities.push("Student");
        if (additionalInfo.length == 0) {
          if (response.error.error.message != undefined) {
            additionalInfo = response.error.error.message;
          } else if (typeof response.error !== 'object') {
            additionalInfo = response.error;
          }
        }
      }
    }

    if (errorsEntities.length > 0) {
      this.snackBar.open(`Some information about ${errorsEntities.toString()} has not been updated. Please check committed changes: ${additionalInfo}`, "OK");
    }

    this.onRefresh.emit();
    this.loadView();
  }

  Remove() {
    this.administratorApiService.RemoveUser(this.originalAccount.User.UserId).subscribe(() => {
      this.onRemove.emit();
      
      this.snackBar.open("Successfully removed user.", "OK");
    }, response => {
      if (response.error.error.message != undefined) {
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
      if (response.error.error.message != undefined) {
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
      if (response.error.error.message != undefined) {
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
      if (response.error.error.message != undefined) {
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
      if (response.error.error.message != undefined) {
        this.snackBar.open(response.error.error.message, "OK");
      } else if (typeof response.error !== 'object') {
        this.snackBar.open(response.error, "OK");
      }
    }
    this.resultsLoading = false;
  }
}
