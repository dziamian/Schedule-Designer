import { Component, OnInit } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Observable } from 'rxjs';
import { AdministratorApiService } from 'src/app/services/AdministratorApiService/administrator-api.service';
import { SignalrService } from 'src/app/services/SignalrService/signalr.service';

/**
 * Komponent zawierający widok obszaru roboczego panelu administracyjnego
 * dla sekcji importowania danych.
 */
@Component({
  selector: 'app-import-field',
  templateUrl: './import-field.component.html',
  styleUrls: ['./import-field.component.css']
})
export class ImportFieldComponent implements OnInit {

  /** Tablica plików wybranych w obrębie całej sekcji. */
  selectedFiles: File[] = [];

  /**
   * Lista dostępnych opcji do wyboru.
   * Posiada informacje o wyświetlanej etykiecie, indeksach wybranych plików 
   * (znajdują się one w tablicy {@link selectedFiles}), wywoływanych metodach
   * centrum SignalR (które są konieczne do zakończenia operacji powodzeniem),
   * nazwy importowanych zasobów oraz 
   * wywoływanej metodzie po wciśnięciu przycisku akcji.
   */
  list: {
    label: string,
    fileIndexes: {label: string, index: number, name: string}[],
    hubMethods: (() => Observable<any>)[],
    resource: string,
    responseLabel: string
  }[] = [
    {
      label: 'Schedule Positions', 
      fileIndexes: [{label: '', index: 0, name: 'file'}],
      hubMethods: [
        this.signalrService.LockAllCourseEditions.bind(this.signalrService),
        this.signalrService.UnlockAllCourseEditions.bind(this.signalrService)
      ],
      resource: "schedulePositions",
      responseLabel: 'Schedule has been uploaded successfully.'
    },
    {
      label: 'Course Editions', 
      fileIndexes: [{label: 'Editions:', index: 1, name: 'courseEditionsFile'},{label: 'Coordinators:', index: 2, name: 'coordinatorsFile'},{label: 'Groups:', index: 3, name: 'groupsFile'}],
      hubMethods: [],
      resource: "courseEditions",
      responseLabel: 'Course editions have been uploaded successfully.'
    },
    {
      label: 'Groups', 
      fileIndexes: [{label: '', index: 4, name: 'file'}],
      hubMethods: [],
      resource: "groups",
      responseLabel: 'Groups have been uploaded successfully.'
    },
    {
      label: 'Courses', 
      fileIndexes: [{label: 'Courses:', index: 5, name: 'coursesFile'},{label: 'Rooms:', index: 6, name: 'roomsFile'}],
      hubMethods: [],
      resource: "courses",
      responseLabel: 'Courses have been uploaded successfully.'
    },
    {
      label: 'Course Types', 
      fileIndexes: [{label: '', index: 7, name: 'file'}],
      hubMethods: [],
      resource: "courseTypes",
      responseLabel: 'Course types have been uploaded successfully.'
    },
    {
      label: 'Room Types', 
      fileIndexes: [{label: '', index: 8, name: 'file'}],
      hubMethods: [],
      resource: "roomTypes",
      responseLabel: 'Room types have been uploaded successfully.'
    },
    {
      label: 'Rooms', 
      fileIndexes: [{label: '', index: 9, name: 'file'}],
      hubMethods: [],
      resource: "rooms",
      responseLabel: 'Rooms have been uploaded successfully.'
    },
    {
      label: 'Students (Groups)', 
      fileIndexes: [{label: '', index: 10, name: 'file'}],
      hubMethods: [],
      resource: "studentGroups",
      responseLabel: 'Students\' groups have been uploaded successfully.'
    },
  ];

  constructor(
    private administratorApiService: AdministratorApiService,
    private signalrService: SignalrService,
    private snackBar:MatSnackBar,
  ) { }

  ngOnInit(): void {
  }

  csvClick(index: number) {
    document.getElementById('csvInput' + index)?.click();
  }

  csvInputChange(fileInputEvent: any, index: number) {
    this.selectedFiles[index] = fileInputEvent.target.files[0];
  }

  /**
   * Wywołuje właściwe metody po wciśnięciu przycisku akcji.
   * @param fileIndexes Indeksy plików wymaganych do akcji importowania
   * @param resource Nazwa importowanych zasobów
   * @param hubMethods Wywoływane metody centrum SignalR przed i po procesie importowania (blokujące i odblokowujące zasoby)
   * @param responseLabel Treść wiadomości w przypadku powodzenia operacji
   */
  public async Import(
    fileIndexes: {index: number, name: string}[],
    resource: string,
    hubMethods: (() => Observable<any>)[],
    responseLabel: string
  ) {
    const connectionId = hubMethods.length > 0 ? this.signalrService.connection.connectionId : undefined;
    if (hubMethods.length > 0 && !connectionId) {
      return;
    }

    const files: {name: string, file: File}[] = [];

    for (var fileIndex in fileIndexes) {
      if (!this.selectedFiles[fileIndexes[fileIndex].index]) {
        return;
      }
      files.push({
        name: fileIndexes[fileIndex].name,
        file: this.selectedFiles[fileIndexes[fileIndex].index]
      });
    }

    var isLocked = false;
    if (hubMethods[0] != undefined) {
      try {
        const lockingResult = await hubMethods[0]().toPromise();
        
        if (lockingResult.StatusCode >= 400) {
          throw lockingResult;
        }
        isLocked = true;
      } catch (error: any) {
        if (error.Message != undefined) {
          this.snackBar.open(error.Message, "OK");
        } else if (error.error != undefined) {
          this.snackBar.open(error.error, "OK");
        } else {
          this.snackBar.open("You are not authorized to do this.", "OK");
        }
        return;
      }
    }

    try {
      await this.administratorApiService.Import(
        files, resource, connectionId
      ).toPromise();
      
      this.snackBar.open(responseLabel, "OK");
    } catch (response : any) {
      if (response.error?.error?.message != undefined) {
        this.snackBar.open(response.error.error.message, "OK");
      } else if (typeof response.error !== 'object') {
        this.snackBar.open(response.error, "OK");
      }
    }

    if (isLocked && hubMethods[1] != undefined) {
      try {
        const unlockingResult = await hubMethods[1]().toPromise();
  
        if (unlockingResult.StatusCode >= 400) {
          throw unlockingResult;
        }

      } catch (error:any) {

      }
    }
  }

}
