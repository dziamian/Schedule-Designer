import { Component, OnInit } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Observable } from 'rxjs';
import { AdministratorApiService } from 'src/app/services/AdministratorApiService/administrator-api.service';

@Component({
  selector: 'app-export-field',
  templateUrl: './export-field.component.html',
  styleUrls: ['./export-field.component.css']
})
export class ExportFieldComponent implements OnInit {

  list: {
    label: string,
    method: (() => Observable<any>)
  }[] = [
    {label: 'Schedule Positions', method: this.administratorApiService.Export.bind(this.administratorApiService, "schedulePositions")},
    {label: 'Coordinators (Course Editions)', method: this.administratorApiService.Export.bind(this.administratorApiService, "coordinatorCourseEditions")},
    {label: 'Course Editions', method: this.administratorApiService.Export.bind(this.administratorApiService, "courseEditions")},
    {label: 'Groups (CourseEditions)', method: this.administratorApiService.Export.bind(this.administratorApiService, "groupCourseEditions")},
    {label: 'Coordinators', method: this.administratorApiService.Export.bind(this.administratorApiService, "coordinators")},
    {label: 'Rooms (Courses)', method: this.administratorApiService.Export.bind(this.administratorApiService, "courseRooms")},
    {label: 'Courses', method: this.administratorApiService.Export.bind(this.administratorApiService, "courses")},
    {label: 'Course Types', method: this.administratorApiService.Export.bind(this.administratorApiService, "courseTypes")},
    {label: 'Groups', method: this.administratorApiService.Export.bind(this.administratorApiService, "groups")},
    {label: 'Rooms', method: this.administratorApiService.Export.bind(this.administratorApiService, "rooms")},
    {label: 'Room Types', method: this.administratorApiService.Export.bind(this.administratorApiService, "roomTypes")},
    {label: 'Staffs', method: this.administratorApiService.Export.bind(this.administratorApiService, "staffs")},
    {label: 'Students (Groups)', method: this.administratorApiService.Export.bind(this.administratorApiService, "studentGroups")},
    {label: 'Students', method: this.administratorApiService.Export.bind(this.administratorApiService, "students")},
    {label: 'Schedule Timestamps', method: this.administratorApiService.Export.bind(this.administratorApiService, "timestamps")},
    {label: 'Users', method: this.administratorApiService.Export.bind(this.administratorApiService, "users")},
  ];

  constructor(
    private administratorApiService: AdministratorApiService,
    private snackBar:MatSnackBar,
  ) { }

  ngOnInit(): void {
    
  }

  private exportReaction(response: any) {
    const blob = new Blob([response.body], { type: "text/csv" });
    const fileName = response.headers.get('Content-Disposition').split(';')[1].trim().split('=')[1];
    const objectUrl = URL.createObjectURL(blob);
    const a = document.createElement('a') as HTMLAnchorElement;

    a.href = objectUrl;
    a.download = fileName;
    document.body.appendChild(a);
    
    a.click();
    
    document.body.removeChild(a);
    URL.revokeObjectURL(objectUrl);
  }

  public Export(method: () => Observable<any>) {
    method().subscribe((response) => {
      this.exportReaction(response);
    }, response => {
      if (response.error?.error?.message != undefined) {
        this.snackBar.open(response.error.error.message, "OK");
      } else if (typeof response.error !== 'object') {
        this.snackBar.open(response.error, "OK");
      }
    });
  }


}
