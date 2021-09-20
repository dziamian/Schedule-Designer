import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { NoAuthGuardService } from './services/AuthGuardService/no-auth-guard.service';
import { AuthGuardService } from './services/AuthGuardService/auth-guard.service';

import { LoginComponent } from './views/login/login.component';
import { AuthenticatedComponent } from './views/authenticated/authenticated.component';
import { ScheduleComponent } from './views/schedule/schedule.component';
import { ProfileComponent } from './views/profile/profile.component';

const routes: Routes = [
  { path: '', component: ScheduleComponent, canActivate: [AuthGuardService] },
  { path: 'login', component: LoginComponent, canActivate: [NoAuthGuardService] },
  { path: 'authenticated', component: AuthenticatedComponent, canActivate: [NoAuthGuardService] },
  { path: 'profile', component: ProfileComponent, canActivate: [AuthGuardService] }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
