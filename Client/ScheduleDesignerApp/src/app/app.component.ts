import { DOCUMENT } from '@angular/common';
import { Component, Inject, OnInit } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { NavigationEnd, Router } from '@angular/router';
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

  account:Account;
  isAccountSet:boolean = false;

  constructor(
    private router:Router,
    private scheduleDesignerApiService:ScheduleDesignerApiService,
    private usosApiService:UsosApiService, 
    private signalrService:SignalrService,
    private snackBar:MatSnackBar,
    private store:Store<{account:Account}>
  ) { 
    this.router.events.subscribe((value) => {
      if (!this.isAccountSet && value instanceof NavigationEnd) {
        this.trySetAccount();
      }
    });
    this.store.select('account').subscribe((account) => {
      if (account.UserId == 0) {
        return;
      }
      this.account = account;
    });
  }

  ngOnInit() {
    this.trySetAccount();
    this.tryInitConnection();
  }

  private async tryInitConnection() {
    if (this.IsAuthenticated()) {
      await this.signalrService.InitConnection().toPromise();
    }
  }

  private trySetAccount() {
    if (this.IsAuthenticated()) {
      this.scheduleDesignerApiService.GetMyAccount().subscribe((account) => {
        this.store.dispatch(setAccount({account}));
        this.account = account;
        this.isAccountSet = true;
      }, (error) => {
        if (error?.status == 401) {
          this.usosApiService.Deauthorize();
  
          this.snackBar.open('Session expired. Please log in again.', 'OK');
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

  public PersonalSchedule(): void {
    this.router.navigate(['personal-schedule']);
  }

  public MyGroupsSchedule(): void {
    this.router.navigate(['student-schedule']);
  }

  public FullSchedule(): void {
    this.router.navigate(['full-schedule']);
  }

  public AdminPanel(): void {
    this.router.navigate(['admin-panel']);
  }
}
