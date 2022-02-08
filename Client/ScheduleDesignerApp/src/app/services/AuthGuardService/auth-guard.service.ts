import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, Router, RouterStateSnapshot } from '@angular/router';
import { Store } from '@ngrx/store';
import { skip } from 'rxjs/operators';
import { AccessToken } from 'src/app/others/AccessToken';
import { UserInfo } from 'src/app/others/Accounts';

@Injectable({
  providedIn: 'root'
})
export class AuthGuardService implements CanActivate {

  userInfo: UserInfo;

  constructor(
    private store: Store<{userInfo: UserInfo}>,
    private router:Router
  ) {
    this.store.select('userInfo').subscribe((userInfo) => {
      if (userInfo.UserId == 0) {
        return;
      }
      this.userInfo = userInfo;
    });
  }

  canActivate(next:ActivatedRouteSnapshot, state:RouterStateSnapshot): boolean {
    if (!AccessToken.isAuthenticated()) {
      this.router.navigate(['login']);
      return false;
    } else {
      const roles = next.data.roles;
      if (!roles || roles.length == 0) {
        return true;
      }
      for (var i = 0; i < roles.length; ++i) {
        if (roles[i] === 'Admin' && (this.userInfo?.IsStaff && this.userInfo?.IsAdmin)) {
          return true;
        }
        if (roles[i] === 'Coordinator' && this.userInfo?.IsCoordinator) {
          return true;
        }
        if (roles[i] === 'Student' && this.userInfo?.IsStudent) {
          return true;
        }
      }
      this.router.navigate(['profile']);
      return false;
    }
  }
}
