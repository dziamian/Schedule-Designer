import { Component, OnInit } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Observable } from 'rxjs';
import { AdministratorApiService } from 'src/app/services/AdministratorApiService/administrator-api.service';

/**
 * Komponent zawierający widok obszaru roboczego panelu administracyjnego
 * dla sekcji czyszczenia danych.
 */
@Component({
  selector: 'app-clear-field',
  templateUrl: './clear-field.component.html',
  styleUrls: ['./clear-field.component.css']
})
export class ClearFieldComponent implements OnInit {

  /** 
   * Lista dostępnych opcji do wyboru.
   * Posiada informacje o wyświetlanej etykiecie, 
   * wywoływanej metodzie po wciśnięciu przycisku akcji oraz treść wiadomości
   * w przypadku powodzenia operacji czyszczenia danych.
   */
  list: {
    label: string,
    method: (() => Observable<any>),
    responseLabel: string
  }[] = [
    {
      label: 'Schedule Positions', 
      method: this.administratorApiService.Clear.bind(this.administratorApiService, "schedulePositions", "ClearSchedule"),
      responseLabel: 'Schedule has been cleared successfully.'
    },
    {
      label: 'Course Editions', 
      method: this.administratorApiService.Clear.bind(this.administratorApiService, "courseEditions", "ClearCourseEditions"),
      responseLabel: 'Course editions have been cleared successfully.'
    },
    {
      label: 'Courses', 
      method: this.administratorApiService.Clear.bind(this.administratorApiService, "courses", "ClearCourses"),
      responseLabel: 'Courses have been cleared successfully.'
    },
    {
      label: 'Course Types', 
      method: this.administratorApiService.Clear.bind(this.administratorApiService, "courseTypes", "ClearCourseTypes"),
      responseLabel: 'Course types have been cleared successfully.'
    },
    {
      label: 'Groups', 
      method: this.administratorApiService.Clear.bind(this.administratorApiService, "groups", "ClearGroups"),
      responseLabel: 'Groups have been cleared successfully.'
    },
    {
      label: 'Rooms', 
      method: this.administratorApiService.Clear.bind(this.administratorApiService, "rooms", "ClearRooms"),
      responseLabel: 'Rooms have been cleared successfully.'
    },
    {
      label: 'Room Types', 
      method: this.administratorApiService.Clear.bind(this.administratorApiService, "roomTypes", "ClearRoomTypes"),
      responseLabel: 'Room types have been cleared successfully.'
    },
    {
      label: 'Students (Groups)', 
      method: this.administratorApiService.Clear.bind(this.administratorApiService, "studentGroups", "ClearStudentGroups"),
      responseLabel: 'Students\' groups have been cleared successfully.'
    }
  ];

  constructor(
    private administratorApiService: AdministratorApiService,
    private snackBar:MatSnackBar,
  ) { }

  ngOnInit(): void {
  }

  /**
   * Wywołuje właściwą metodę po wciśnięciu przycisku akcji.
   * @param method Właściwa metoda, która ma zostać wywołana
   * @param responseLabel Treść wiadomości w przypadku powodzenia operacji
   */
  public Clear(method: () => Observable<any>, responseLabel: string) {
    method().subscribe((response) => {
      this.snackBar.open(responseLabel, "OK");
    }, response => {
      if (response.error?.error?.message != undefined) {
        this.snackBar.open(response.error.error.message, "OK");
      } else if (typeof response.error !== 'object') {
        this.snackBar.open(response.error, "OK");
      }
    });
  }

}
