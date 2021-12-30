import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { NoAuthGuardService } from './services/AuthGuardService/no-auth-guard.service';
import { AuthGuardService } from './services/AuthGuardService/auth-guard.service';

import { LoginComponent } from './views/login/login.component';
import { AuthenticatedComponent } from './views/authenticated/authenticated.component';
import { ProfileComponent } from './views/profile/profile.component';
import { ModifyScheduleComponent } from './views/modify-schedule/modify-schedule.component';

const routes: Routes = [
  { path: '', component: ModifyScheduleComponent, canActivate: [AuthGuardService] },
  { path: 'login', component: LoginComponent, canActivate: [NoAuthGuardService] },
  { path: 'authenticated', component: AuthenticatedComponent, canActivate: [NoAuthGuardService] },
  { path: 'profile', component: ProfileComponent, canActivate: [AuthGuardService] }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
