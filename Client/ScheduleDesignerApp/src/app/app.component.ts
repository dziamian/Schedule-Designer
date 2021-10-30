import { DOCUMENT } from '@angular/common';
import { Component, Inject, OnInit } from '@angular/core';
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

  constructor(
    private router:Router,
    private scheduleDesignerApiService:ScheduleDesignerApiService,
    private usosApiService:UsosApiService, 
    private signalrService:SignalrService,
    @Inject(DOCUMENT) private document:Document,
    private store:Store<{account:Account}>
  ) { }

  ngOnInit() {
    if (this.IsAuthenticated()) {
      this.scheduleDesignerApiService.GetMyAccount().subscribe((account) => {
        this.store.dispatch(setAccount({account}));
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
