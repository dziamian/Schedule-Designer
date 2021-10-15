import { Component, Inject, OnInit } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { AccessToken } from 'src/app/others/AccessToken';
import { ScheduleDesignerApiService } from 'src/app/services/ScheduleDesignerApiService/schedule-designer-api.service';
import { SignalrService } from 'src/app/services/SignalrService/signalr.service';
import { UsosApiService } from 'src/app/services/UsosApiService/usos-api.service';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css']
})
export class ProfileComponent implements OnInit {

  public user_id:string='';
  public first_name:string='';
  public last_name:string='';
  public test1:string='';
  public test2:string='';

  constructor(
    private usosApiService:UsosApiService, 
    private apiService:ScheduleDesignerApiService, 
    private signalrService:SignalrService,
    private snackBar:MatSnackBar, 
    private router:Router
    ) 
  { }

  ngOnInit(): void {
    this.signalrService.initConnection();

    this.usosApiService.GetUser().subscribe(
      data => {
        this.user_id = data.id;
        this.first_name = data.first_name;
        this.last_name = data.last_name;
      },
      response => {
        if (response.status == 401) {
          this.usosApiService.Deauthorize(this.snackBar);
          this.router.navigate(['login']);
        }
      }
    );

    this.apiService.Test1().subscribe(
      data => {
        this.test1 = data;
      },
      response => {
        if (response.status == 401) {
          this.usosApiService.Deauthorize(this.snackBar);
          this.router.navigate(['login']);
        }
      }
    );

    this.apiService.Test2().subscribe(
      data => {
        this.test2 = data;
      },
      response => {
        if (response.status == 401) {
          this.usosApiService.Deauthorize(this.snackBar);
          this.router.navigate(['login']);
        }
      }
    );
  }

}
