import { Component } from '@angular/core';
import { Router } from '@angular/router';

import { AccessToken } from './others/AccessToken';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {
  title:string = 'Schedule Designer';

  constructor(private router:Router) { }

  public IsAuthenticated():boolean {
    return AccessToken.isAuthenticated();
  }

  public Logout():void {
    AccessToken.Remove();
    this.router.navigate(['login']);
  }

  public Profile():void {
    this.router.navigate(['profile']);
  }
}
