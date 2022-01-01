import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBarModule, MAT_SNACK_BAR_DEFAULT_OPTIONS } from '@angular/material/snack-bar';
import { MatTabsModule } from '@angular/material/tabs';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { DragDropModule } from '@angular/cdk/drag-drop';
import { MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSelectModule } from '@angular/material/select';
import { MatBadgeModule } from '@angular/material/badge';
import { MatListModule } from '@angular/material/list';
import { MatTreeModule } from '@angular/material/tree';

import { HttpClient, HttpClientModule } from '@angular/common/http';
import { AppRoutingModule } from './app-routing.module';

import { AppComponent } from './app.component';
import { LoginComponent } from './views/login/login.component';
import { AuthenticatedComponent } from './views/authenticated/authenticated.component';
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
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { MyCoursesComponent } from './components/my-courses/my-courses.component';
import { ScheduleComponent } from './components/schedule/schedule.component';
import { SelectViewComponent } from './components/select-view/select-view.component';
import { AvailableResourcesComponent } from './components/available-resources/available-resources.component';
import { FullScheduleComponent } from './views/full-schedule/full-schedule.component';
import { PersonalScheduleComponent } from './views/personal-schedule/personal-schedule.component';

@NgModule({
  declarations: [
    AppComponent,
    LoginComponent,
    AuthenticatedComponent,
    PersonalScheduleComponent,
    ProfileComponent,
    CourseComponent,
    RoomSelectionComponent,
    RoomSelectionComponent,
    AddRoomSelectionComponent,
    ScheduledChangesViewComponent,
    MyCoursesComponent,
    ScheduleComponent,
    SelectViewComponent,
    AvailableResourcesComponent,
    FullScheduleComponent,
    PersonalScheduleComponent
  ],
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    AppRoutingModule,
    FormsModule,
    ReactiveFormsModule,
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
    MatTreeModule,
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
