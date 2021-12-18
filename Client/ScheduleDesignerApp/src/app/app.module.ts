import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBarModule, MAT_SNACK_BAR_DEFAULT_OPTIONS, MAT_SNACK_BAR_DEFAULT_OPTIONS_FACTORY } from '@angular/material/snack-bar';
import { MatTabsModule } from '@angular/material/tabs';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { DragDropModule } from '@angular/cdk/drag-drop';
import { MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSelectModule } from '@angular/material/select';
import { MatBadgeModule } from '@angular/material/badge';
import { MatListModule } from '@angular/material/list';

import { HttpClient, HttpClientModule } from '@angular/common/http';
import { AppRoutingModule } from './app-routing.module';

import { AppComponent } from './app.component';
import { LoginComponent } from './views/login/login.component';
import { AuthenticatedComponent } from './views/authenticated/authenticated.component';
import { ScheduleComponent } from './views/schedule/schedule.component';
import { ProfileComponent } from './views/profile/profile.component';

import { UsosApiService } from './services/UsosApiService/usos-api.service';
import { ScheduleDesignerApiService } from './services/ScheduleDesignerApiService/schedule-designer-api.service';
import { AuthGuardService } from './services/AuthGuardService/auth-guard.service';
import { CourseComponent } from './components/course/course.component';
import { SignalrService } from './services/SignalrService/signalr.service';
import { StoreModule } from '@ngrx/store';
import { accountReducer } from './store/account.reducer';
import { RoomSelectionComponent } from './components/room-selection/room-selection.component';
import { AddRoomSelectionComponent } from './components/add-room-selection/add-room-selection.component';
import { ScheduledChangesViewComponent } from './components/scheduled-changes-view/scheduled-changes-view.component';
import { FormsModule } from '@angular/forms';

@NgModule({
  declarations: [
    AppComponent,
    LoginComponent,
    AuthenticatedComponent,
    ScheduleComponent,
    ProfileComponent,
    CourseComponent,
    RoomSelectionComponent,
    RoomSelectionComponent,
    AddRoomSelectionComponent,
    ScheduledChangesViewComponent
  ],
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    AppRoutingModule,
    FormsModule,
    MatButtonModule,
    MatIconModule,
    MatSnackBarModule,
    MatTabsModule,
    MatSlideToggleModule,
    DragDropModule,
    MatDialogModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatSelectModule,
    MatBadgeModule,
    MatListModule,
    HttpClientModule,
    StoreModule.forRoot({ account: accountReducer})
  ],
  providers: [
    HttpClient, 
    UsosApiService, 
    ScheduleDesignerApiService, 
    SignalrService,
    AuthGuardService,
    {provide: MAT_SNACK_BAR_DEFAULT_OPTIONS, useValue: { duration: 10000 }}
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
