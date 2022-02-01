import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Account, SearchUser } from 'src/app/others/Accounts';
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

  originalAccount: Account;

  originalValues: any;
  isModifying: boolean = false;

  loading: boolean | null = null;

  constructor(
    private scheduleDesignerApiService: ScheduleDesignerApiService,
    private administratorApiService: AdministratorApiService,
    private signalrService: SignalrService,
    private snackBar: MatSnackBar
  ) { }

  searchResults: SearchUser | null;
  searchStart: number = 0;
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
    //this.accountForm = new FormGroup();
    //this.originalValues = this.courseEditionForm.value;
  }

  private loadView() {
    this.loading = true;

    if (this._data.actionType === 'view') {
      
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

  Search() {
    if (!this.searchForm.valid) {
      return;
    }

    this.currentQuery = this.searchForm.controls['search'].value;

    this.administratorApiService.SearchForUserFromUsos(
      this.currentQuery, this.PAGE_SIZE, 0
    ).subscribe(result => {
      this.searchResults = result;
      this.searchStart = 0;
    }, response => {
      if (response.error.error.message != undefined) {
        this.snackBar.open(response.error.error.message, "OK");
      } else if (typeof response.error !== 'object') {
        this.snackBar.open(response.error, "OK");
      }
    });
  }

  SearchNextPage() {
    this.administratorApiService.SearchForUserFromUsos(
      this.currentQuery, this.PAGE_SIZE, this.searchStart + this.PAGE_SIZE
    ).subscribe(result => {
      this.searchResults = result;
      this.searchStart += this.PAGE_SIZE;
    }, response => {
      if (response.error.error.message != undefined) {
        this.snackBar.open(response.error.error.message, "OK");
      } else if (typeof response.error !== 'object') {
        this.snackBar.open(response.error, "OK");
      }
    });
  }

  SearchPreviousPage() {
    this.administratorApiService.SearchForUserFromUsos(
      this.currentQuery, this.PAGE_SIZE, this.searchStart - this.PAGE_SIZE
    ).subscribe(result => {
      this.searchResults = result;
      this.searchStart -= this.PAGE_SIZE;
    }, response => {
      if (response.error.error.message != undefined) {
        this.snackBar.open(response.error.error.message, "OK");
      } else if (typeof response.error !== 'object') {
        this.snackBar.open(response.error, "OK");
      }
    });
  }
}
