import { DOCUMENT } from '@angular/common';
import { Component, Inject, OnInit } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { Store } from '@ngrx/store';

import { AccessToken } from './others/AccessToken';
import { Account } from './others/Accounts';
import { ScheduleDesignerApiService } from './services/ScheduleDesignerApiService/schedule-designer-api.service';
import { SignalrService } from './services/SignalrService/signalr.service';
import { UsosApiService } from './services/UsosApiService/usos-api.service';
import { setAccount } from './store/account.actions';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {
  title:string = 'Schedule Designer';

  snackBarDuration:number = 10 * 1000;

  constructor(
    private router:Router,
    private scheduleDesignerApiService:ScheduleDesignerApiService,
    private usosApiService:UsosApiService, 
    private signalrService:SignalrService,
    private snackBar:MatSnackBar,
    @Inject(DOCUMENT) private document:Document,
    private store:Store<{account:Account}>
  ) { }

  ngOnInit() {
    if (this.IsAuthenticated()) {
      this.scheduleDesignerApiService.GetMyAccount().subscribe((account) => {
        this.store.dispatch(setAccount({account}));
      }, (error) => {
        if (error?.status == 401) {
          this.usosApiService.Deauthorize();
  
          this.snackBar.open('Session expired. Please log in again.', 'OK', {
            duration: this.snackBarDuration
          });
          this.router.navigate(['login']);
        }
      });
    }
  }

  public IsAuthenticated():boolean {
    return AccessToken.isAuthenticated();
  }

  public Logout():void {
    this.signalrService.Disconnect();
    AccessToken.Remove();
    this.router.navigate(['login']);
    this.usosApiService.Logout(document);
  }

  public Profile():void {
    this.router.navigate(['profile']);
  }
}
