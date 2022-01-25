import { Component, OnInit } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { HubConnectionState } from '@microsoft/signalr';
import { Subscription } from 'rxjs';
import { skip } from 'rxjs/operators';
import { AdministratorApiService } from 'src/app/services/AdministratorApiService/administrator-api.service';
import { ScheduleDesignerApiService } from 'src/app/services/ScheduleDesignerApiService/schedule-designer-api.service';
import { SignalrService } from 'src/app/services/SignalrService/signalr.service';

@Component({
  selector: 'app-administrator-panel',
  templateUrl: './administrator-panel.component.html',
  styleUrls: ['./administrator-panel.component.css']
})
export class AdministratorPanelComponent implements OnInit {

  selectedFile:File;

  connectionStatus:boolean = false;
  isConnectedSubscription: Subscription;

  constructor(
    private scheduleDesignerApiService: ScheduleDesignerApiService,
    private administratorApiService: AdministratorApiService,
    private signalrService: SignalrService,
    private snackBar:MatSnackBar,
  ) { }

  ngOnInit(): void {
    this.isConnectedSubscription = this.signalrService.isConnected.pipe(skip(1)).subscribe((status) => {
      this.connectionStatus = status;
      if (!status && !this.signalrService.connectionIntentionallyStopped) {
        this.snackBar.open("Connection with server has been lost. Please refresh the page to possibly reconnect.", "OK");
      }
    });

    this.connectionStatus = this.signalrService.connection?.state == HubConnectionState.Connected;
  }

  csvInputChange(fileInputEvent: any) {
    this.selectedFile = fileInputEvent.target.files[0];
  }

  public async UploadSchedule() {
    const connectionId = this.signalrService.connection.connectionId;
    if (!this.selectedFile || !connectionId) {
      return;
    }
    
    var isLocked = false;
    try {
      const lockingResult = await this.signalrService.LockAllCourseEditions().toPromise();
      
      if (lockingResult.StatusCode >= 400) {
        throw lockingResult;
      }
      isLocked = true;

      await this.administratorApiService.UploadSchedule(this.selectedFile, connectionId).toPromise();
      this.snackBar.open("Schedule has been uploaded successfully.", "OK");


    } catch (error: any) {
      console.log(error);
      if (error.Message != undefined) {
        this.snackBar.open(error.Message, "OK");
      } else if (error.error != undefined) {
        if (typeof error.error == "object") {
          this.snackBar.open("File cannot be uploaded because has changed recently.", "OK");
        } else {
          this.snackBar.open(error.error, "OK");
        }
      } else {
        this.snackBar.open("You are not authorized to do this.", "OK");
      }
    }

    if (isLocked) {
      try {
        const unlockingResult = await this.signalrService.UnlockAllCourseEditions().toPromise();
  
        if (unlockingResult.StatusCode >= 400) {
          throw unlockingResult;
        }

      } catch (error:any) {

      }
    }
  }

  public DownloadSchedule() {
    this.administratorApiService.DownloadSchedule().subscribe((response) => {
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
    });
  }
}
