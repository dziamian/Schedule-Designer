import { Component, Inject, OnInit, Output } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
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
    let isConnectedSubscription = this.signalrService.isConnected.pipe(skip(1)).subscribe((status) => {
      this.connectionStatus = status;
      if (!status && !this.signalrService.connectionIntentionallyStopped) {
        this.snackBar.open("Connection with server has been lost. Please refresh the page to possibly reconnect.", "OK");
      }
    });

    forkJoin([
      this.signalrService.InitConnection()
    ]).subscribe(([]) => {
      this.connectionStatus = true;
    }, (error) => {
      if (error?.status == 401) {
        this.usosApiService.Deauthorize();

        this.snackBar.open('Session expired. Please log in again.', 'OK');
        this.router.navigate(['login']);
      } else if (!isConnectedSubscription.closed) {
        this.snackBar.open("Connection with server failed. Please refresh the page to try again.", "OK");
      }
    });

    //TEST
    forkJoin([
      //this.signalrService.LockCourseEdition(1,1)
      this.signalrService.LockSchedulePositions(2, 3, 1, [1,2,3,4,5,6,7,8,9,10,11,12,13,14,15])
    ]).subscribe(([result1]) => {
      console.log(result1);
      forkJoin([
        //this.scheduleDesignerApiService.AddSchedulePositions(1,1,2,2,1,[1,2,3,4,5,6,7,8,9,10,11,12,13,14,15]),
        this.signalrService.ModifySchedulePositions(2, 3, 1, [1,2,3,4,5,6,7,8,9,10,11,12,13,14,15],
          2,1,4,[1,2,3,4,5,6,7,8,9,10,11,12,13,14,15])
      ]).subscribe(([result1]) => {
        console.log(result1);
      });
    });
  }

}
