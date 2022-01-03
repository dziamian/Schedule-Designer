import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { NoAuthGuardService } from './services/AuthGuardService/no-auth-guard.service';
import { AuthGuardService } from './services/AuthGuardService/auth-guard.service';

import { LoginComponent } from './views/login/login.component';
import { AuthenticatedComponent } from './views/authenticated/authenticated.component';
import { ProfileComponent } from './views/profile/profile.component';
import { PersonalScheduleComponent } from './views/personal-schedule/personal-schedule.component';
import { FullScheduleComponent } from './views/full-schedule/full-schedule.component';

const routes: Routes = [
  { path: 'login', component: LoginComponent, canActivate: [NoAuthGuardService] },
  { path: 'authenticated', component: AuthenticatedComponent, canActivate: [NoAuthGuardService] },
  { path: 'profile', component: ProfileComponent, canActivate: [AuthGuardService] },
  { path: 'personal-schedule', component: PersonalScheduleComponent, canActivate: [AuthGuardService], data: {roles: ['Coordinator']} },
  { path: 'full-schedule', component: FullScheduleComponent, canActivate: [AuthGuardService] },
  { path: '**', redirectTo: '/profile' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
