import { Component, Inject, OnInit, Output } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { HubConnectionState } from '@microsoft/signalr';
import { Store } from '@ngrx/store';
import { forkJoin } from 'rxjs';
import { skip } from 'rxjs/operators';
import { Account } from 'src/app/others/Accounts';
import { ScheduleDesignerApiService } from 'src/app/services/ScheduleDesignerApiService/schedule-designer-api.service';
import { SignalrService } from 'src/app/services/SignalrService/signalr.service';
import { UsosApiService } from 'src/app/services/UsosApiService/usos-api.service';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css']
})
export class ProfileComponent implements OnInit {

  loading:boolean = true;
  connectionStatus:boolean = false;

  account:Account;

  constructor(
    private store:Store<{account: Account}>,
    private usosApiService:UsosApiService,
    private scheduleDesignerApiService:ScheduleDesignerApiService,
    private signalrService:SignalrService,
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
    this.signalrService.isConnected.pipe(skip(1)).subscribe((status) => {
      this.connectionStatus = status;
      if (!status && !this.signalrService.connectionIntentionallyStopped) {
        this.snackBar.open("Connection with server has been lost. Please refresh the page to possibly reconnect.", "OK");
      }
    });

    this.connectionStatus = this.signalrService.connection.state == HubConnectionState.Connected;
    this.loading = false;
  }

}
