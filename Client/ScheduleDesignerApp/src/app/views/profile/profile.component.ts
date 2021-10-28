import { Component, Inject, OnInit, Output } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { skip } from 'rxjs/operators';
import { Account } from 'src/app/others/Accounts';
import { ScheduleDesignerApiService } from 'src/app/services/ScheduleDesignerApiService/schedule-designer-api.service';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css']
})
export class ProfileComponent implements OnInit {

  loading:boolean = true;
  account:Account;

  constructor(
    private store:Store<{account: Account}>,
    private scheduleDesignerApiService:ScheduleDesignerApiService,
    private router:Router,
    private snackBar:MatSnackBar
  ) { 
    this.store.select('account').subscribe((account) => {
      if (account.UserId == 0) {
        return;
      }
      this.account = account;
      this.loading = false;
    });
  }

  ngOnInit(): void {
    
  }

}
