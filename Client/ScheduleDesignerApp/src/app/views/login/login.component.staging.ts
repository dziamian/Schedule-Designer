import { Component, Inject, OnInit } from '@angular/core';
import { DOCUMENT } from '@angular/common';

import { UsosApiService } from 'src/app/services/UsosApiService/usos-api.service';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AccessToken } from 'src/app/others/AccessToken';
import { ScheduleDesignerApiService } from 'src/app/services/ScheduleDesignerApiService/schedule-designer-api.service';
import { setAccount } from 'src/app/store/account.actions';
import { Store } from '@ngrx/store';
import { Account } from 'src/app/others/Accounts';
import { Router } from '@angular/router';

@Component({
    selector: 'app-login',
    templateUrl: './login.component.staging.html',
    styleUrls: ['./login.component.css']
})
export class LoginComponent implements OnInit {

    constructor(
        private usosApiService:UsosApiService,
        private scheduleDesignerApiService:ScheduleDesignerApiService,
        private snackBar:MatSnackBar,
        @Inject(DOCUMENT) private document: Document,
        private store:Store<{account:Account}>,
        private router:Router
    ) { }

    ngOnInit(): void {

    }

    public LogIn():void {
        this.usosApiService.RequestToken().subscribe(
            token => {
                token.SaveRequest();
                
                this.usosApiService.Authorize(token.key).subscribe(data => {
                    this.document.location.href = data.requestMessage.requestUri;
                },
                () => {
                    this.snackBar.open('Connection to server failed. Please try again later.', 'OK');
                });
            },
            () => {
                this.snackBar.open('Connection to server failed. Please try again later.', 'OK');
            }
        );
    }

    public LogInWithTest():void {
        new AccessToken("qwerty", "qwerty").Save();

        this.scheduleDesignerApiService.GetMyAccount().subscribe((account) => {
            this.store.dispatch(setAccount({account}));
            this.router.navigate(['profile']);
        }, (error) => {
            if (error.status == 0) {
                AccessToken.Remove();
            } else {
                this.scheduleDesignerApiService.CreateMyAccount().subscribe(() => {
                    this.scheduleDesignerApiService.GetMyAccount().subscribe((account) => {
                        this.store.dispatch(setAccount({account}));
                        this.router.navigate(['profile']);
                    });
                }, (error) => {
                    this.router.navigate(['login']);
                });
            }
        });
    } 
}
