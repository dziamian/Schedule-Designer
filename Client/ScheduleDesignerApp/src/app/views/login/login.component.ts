import { Component, Inject, OnInit } from '@angular/core';
import { DOCUMENT } from '@angular/common';

import { UsosApiService } from 'src/app/services/UsosApiService/usos-api.service';
import { MatSnackBar } from '@angular/material/snack-bar';

/**
 * Komponent zawierający widok ekranu logowania (startowego).
 */
@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent implements OnInit {

  constructor(
    private usosApiService:UsosApiService,
    private snackBar:MatSnackBar,
    @Inject(DOCUMENT) private document: Document
  ) { }

  ngOnInit(): void {

  }

  /**
   * Metoda rozpoczynająca proces autoryzacyjny z systemem USOS.
   * Wywoływana jest po naciśnięciu przycisku na środkowym panelu.
   */
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
    )
  }

}
