import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, Router, RouterStateSnapshot } from '@angular/router';
import { Store } from '@ngrx/store';
import { skip } from 'rxjs/operators';
import { AccessToken } from 'src/app/others/AccessToken';
import { Account } from 'src/app/others/Accounts';

@Injectable({
  providedIn: 'root'
})
export class AuthGuardService implements CanActivate {

  account: Account;

  constructor(
    private store: Store<{account: Account}>,
    private router:Router
  ) {
    this.store.select('account').subscribe((account) => {
      if (account.UserId == 0) {
        return;
      }
      this.account = account;
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
        if (roles[i] === 'Admin' && this.account?.Admin) {
          return true;
        }
        if (roles[i] === 'Coordinator' && this.account?.Coordinator) {
          return true;
        }
        if (roles[i] === 'Student' && this.account?.Student) {
          return true;
        }
      }
      this.router.navigate(['profile']);
      return false;
    }
  }
}
