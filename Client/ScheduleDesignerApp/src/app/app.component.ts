import { Component, OnInit } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { NavigationEnd, Router } from '@angular/router';
import { Store } from '@ngrx/store';

import { AccessToken } from './others/AccessToken';
import { UserInfo } from './others/Accounts';
import { ScheduleDesignerApiService } from './services/ScheduleDesignerApiService/schedule-designer-api.service';
import { SignalrService } from './services/SignalrService/signalr.service';
import { UsosApiService } from './services/UsosApiService/usos-api.service';
import { setUserInfo } from './store/userInfo.actions';

/**
 * Główny komponent aplikacji zawierający pasek górny z menu wyboru opcji.
 */
@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {
  /** Nazwa aplikacji. */
  title:string = 'Schedule Designer';

  /** Informacje o zalogowanym użytkowniku. */
  userInfo:UserInfo;
  /** Określa czy informacje o zalogowanym użytkowniku zostały ustawione. */
  isUserInfoSet:boolean = false;

  constructor(
    private router:Router,
    private scheduleDesignerApiService:ScheduleDesignerApiService,
    private usosApiService:UsosApiService, 
    private signalrService:SignalrService,
    private snackBar:MatSnackBar,
    private store:Store<{userInfo:UserInfo}>
  ) { 
    this.router.events.subscribe((value) => {
      if (!this.isUserInfoSet && value instanceof NavigationEnd) {
        this.trySetAccount();
        this.tryInitConnection();
      }
    });
    this.store.select('userInfo').subscribe((userInfo) => {
      if (userInfo.UserId == 0) {
        return;
      }
      this.userInfo = userInfo;
      this.isUserInfoSet = true;
      this.tryInitConnection();
    });
  }

  ngOnInit() {
    this.trySetAccount();
    this.tryInitConnection();
  }

  /**
   * Metoda próbująca nawiązać połączenie z centrum SignalR (jest to możliwe tylko, gdy użytkownik jest zalogowany).
   */
  private async tryInitConnection() {
    if (this.IsAuthenticated() && (this.signalrService.connection == null && !this.signalrService.connectionInitializing)) {
      await this.signalrService.InitConnection().toPromise();
    }
  }

  /**
   * Metoda próbująca pozyskać informacje na temat zalogowanego użytkownika.
   */
  private trySetAccount() {
    if (this.IsAuthenticated()) {
      this.scheduleDesignerApiService.GetMyAccount().subscribe((userInfo) => {
        this.store.dispatch(setUserInfo({userInfo}));
        this.userInfo = userInfo;
        this.isUserInfoSet = true;
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
