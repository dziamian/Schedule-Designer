import { Component, Inject, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { Store } from '@ngrx/store';
import { AccessToken } from 'src/app/others/AccessToken';
import { UserInfo } from 'src/app/others/Accounts';
import { ScheduleDesignerApiService } from 'src/app/services/ScheduleDesignerApiService/schedule-designer-api.service';

import { UsosApiService } from 'src/app/services/UsosApiService/usos-api.service';
import { setUserInfo } from 'src/app/store/userInfo.actions';

@Component({
  selector: 'app-authenticated',
  templateUrl: './authenticated.component.html',
  styleUrls: ['./authenticated.component.css']
})
export class AuthenticatedComponent implements OnInit {

  constructor(
    private router:Router, 
    private activatedRoute: ActivatedRoute, 
    private usosApiService:UsosApiService,
    private scheduleDesignerApiService:ScheduleDesignerApiService,
    private store:Store<{userInfo:UserInfo}>
  ) { }

  ngOnInit(): void {
    this.activatedRoute.queryParams.subscribe(params => {
      let oauth_verifier:string = params['oauth_verifier'];
      
      if (oauth_verifier == undefined) {
        return;
      }

      this.usosApiService.AccessToken(oauth_verifier).subscribe(
        token => {
          AccessToken.RemoveRequest();
          token.Save();

          this.scheduleDesignerApiService.GetMyAccount().subscribe((userInfo) => {
            this.store.dispatch(setUserInfo({userInfo}));
            this.router.navigate(['profile']);
          }, () => {
            this.scheduleDesignerApiService.CreateMyAccount().subscribe(() => {
              this.scheduleDesignerApiService.GetMyAccount().subscribe((userInfo) => {
                this.store.dispatch(setUserInfo({userInfo}));
                this.router.navigate(['profile']);
              });
            }, (error) => {
              this.router.navigate(['login']);
            })
          });
        }, (error) => {
          this.router.navigate(['login']);
        }
      )
    })
  }

}
