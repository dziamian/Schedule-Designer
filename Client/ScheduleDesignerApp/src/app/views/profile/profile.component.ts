import { Component, OnInit } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { HubConnectionState } from '@microsoft/signalr';
import { Store } from '@ngrx/store';
import { Subscription } from 'rxjs';
import { skip } from 'rxjs/operators';
import { UserInfo } from 'src/app/others/Accounts';
import { SignalrService } from 'src/app/services/SignalrService/signalr.service';

/**
 * Komponent zawierający widok profilu użytkownika.
 */
@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css']
})
export class ProfileComponent implements OnInit {

  /** Informuje czy dane zostały załadowane. */
  loading:boolean = true;
  /** Informuje o statusie połączenia z centrum. */
  connectionStatus:boolean = false;
  isConnectedSubscription: Subscription;

  /** Informacje o zalogowanym użytkowniku. */
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

  /**
   * Metoda przygotowująca komponent.
   */
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
