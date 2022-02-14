import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, Router, RouterStateSnapshot } from '@angular/router';
import { Observable } from 'rxjs';
import { AccessToken } from 'src/app/others/AccessToken';

/**
 * Serwis zapewniający ochronę przed niepoprawną nawigacją do ścieżek aplikacji wymagających bycia niezalogowanym.
 */
@Injectable({
  providedIn: 'root'
})
export class NoAuthGuardService implements CanActivate {

  constructor(private router:Router) { }

  canActivate(next:ActivatedRouteSnapshot, state:RouterStateSnapshot):Observable<boolean> | Promise<boolean> | boolean {
    if (AccessToken.isAuthenticated()) {
      return false;
    } else {
      return true;
    }
  }
}
