import { Component, OnInit } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { HubConnectionState } from '@microsoft/signalr';
import { Store } from '@ngrx/store';
import { Subscription } from 'rxjs';
import { skip } from 'rxjs/operators';
import { UserInfo } from 'src/app/others/Accounts';
import { SignalrService } from 'src/app/services/SignalrService/signalr.service';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css']
})
export class ProfileComponent implements OnInit {

  loading:boolean = true;
  connectionStatus:boolean = false;
  isConnectedSubscription: Subscription;

  userInfo:UserInfo;

  constructor(
    private store:Store<{userInfo: UserInfo}>,
    private signalrService:SignalrService,
    private snackBar:MatSnackBar
  ) { 
    this.store.select('userInfo').subscribe((userInfo) => {
      if (userInfo.UserId == 0) {
        return;
      }
      this.userInfo = userInfo;
      this.loading = false;
    });
  }

  ngOnInit(): void {
    this.isConnectedSubscription = this.signalrService.isConnected.pipe(skip(1)).subscribe((status) => {
      this.connectionStatus = status;
      if (!status && !this.signalrService.connectionIntentionallyStopped) {
        this.snackBar.open("Connection with server has been lost. Please refresh the page to possibly reconnect.", "OK");
      }
    });

    this.connectionStatus = this.signalrService.connection?.state == HubConnectionState.Connected;
  }

  ngOnDestroy() {
    this.isConnectedSubscription.unsubscribe();
  }
}
