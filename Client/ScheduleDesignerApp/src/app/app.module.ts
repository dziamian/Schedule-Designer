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
import { MatInputModule } from '@angular/material/input';
import { TextFieldModule } from '@angular/cdk/text-field';
import { MatChipsModule } from '@angular/material/chips';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatCheckboxModule } from '@angular/material/checkbox';

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
import { userInfoReducer } from './store/userInfo.reducer';
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
import { StudentScheduleComponent } from './views/student-schedule/student-schedule.component';
import { AdministratorPanelComponent } from './views/administrator-panel/administrator-panel.component';
import { AdminResourcesComponent } from './components/admin-panel/admin-resources/admin-resources.component';
import { CourseFieldComponent } from './components/admin-panel/course-field/course-field.component';
import { CourseEditionFieldComponent } from './components/admin-panel/course-edition-field/course-edition-field.component';
import { CourseTypeFieldComponent } from './components/admin-panel/course-type-field/course-type-field.component';
import { GroupFieldComponent } from './components/admin-panel/group-field/group-field.component';
import { RoomFieldComponent } from './components/admin-panel/room-field/room-field.component';
import { RoomTypeFieldComponent } from './components/admin-panel/room-type-field/room-type-field.component';
import { SettingsFieldComponent } from './components/admin-panel/settings-field/settings-field.component';
import { UserFieldComponent } from './components/admin-panel/user-field/user-field.component';
import { ImportFieldComponent } from './components/admin-panel/import-field/import-field.component';
import { ExportFieldComponent } from './components/admin-panel/export-field/export-field.component';
import { ClearFieldComponent } from './components/admin-panel/clear-field/clear-field.component';

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
    AdminResourcesComponent,
    CourseFieldComponent,
    CourseEditionFieldComponent,
    CourseTypeFieldComponent,
    GroupFieldComponent,
    RoomFieldComponent,
    RoomTypeFieldComponent,
    SettingsFieldComponent,
    UserFieldComponent,
    ImportFieldComponent,
    ExportFieldComponent,
    ClearFieldComponent,
    FullScheduleComponent,
    PersonalScheduleComponent,
    StudentScheduleComponent,
    AdministratorPanelComponent,
    AdminResourcesComponent
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
    MatInputModule,
    TextFieldModule,
    MatChipsModule,
    MatExpansionModule,
    MatCheckboxModule,
    HttpClientModule,
    StoreModule.forRoot({ userInfo: userInfoReducer})
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
