import { Injectable, OnDestroy } from '@angular/core';
import * as signalr from '@microsoft/signalr';
import { BehaviorSubject, from, Observable } from 'rxjs';
import { AccessToken } from 'src/app/others/AccessToken';

@Injectable({
  providedIn: 'root'
})
export class SignalrService implements OnDestroy {
  readonly connectionUrl = 'http://localhost:5000/scheduleHub';
  
  connection:signalr.HubConnection;
  connectionIntentionallyStopped:boolean = false;
  
  isConnected:BehaviorSubject<boolean>
  lastLockedCourseEdition:BehaviorSubject<{courseId:number, courseEditionId:number}>
  lastUnlockedCourseEdition:BehaviorSubject<{courseId:number, courseEditionId:number}>

  constructor() {
    this.isConnected = new BehaviorSubject<boolean>(false);
    this.lastLockedCourseEdition = new BehaviorSubject<{
      courseId:number, 
      courseEditionId:number
    }>({
      courseId: -1,
      courseEditionId: -1
    });
    this.lastUnlockedCourseEdition = new BehaviorSubject<{
      courseId:number, 
      courseEditionId:number
    }>({
      courseId: -1,
      courseEditionId: -1
    });
  }

  private GetAuthorizationHeader(token:any) {
    return {
      "AccessToken": token.key,
      "AccessTokenSecret": token.secret
    };
  }

  public InitConnection(): Observable<void> {
    return new Observable((observer) => {
      this.connectionIntentionallyStopped = false;
      
      if (this.connection?.state == "Connected") {
        observer.next();
        observer.complete();
        return;
      }
      var accessToken = AccessToken.Retrieve();

      console.log(accessToken);

      this.connection = new signalr.HubConnectionBuilder()
        .withUrl(this.connectionUrl, {
          transport: signalr.HttpTransportType.LongPolling,
          headers: this.GetAuthorizationHeader(accessToken),
        }).build();

      this.SetClientMethods();

      this.connection
        .start()
        .then(() => {
          this.isConnected.next(true);
          console.log(this.connection.connectionId);
          observer.next();
          observer.complete();
        })
        .catch(() => {
          observer.error({status: -1});
        });
    });
  }

  public LockCourseEdition(courseId:number, courseEditionId:number):Observable<any> {
    return from(this.connection.invoke('LockCourseEdition', courseId, courseEditionId));
  }

  public UnlockCourseEdition(courseId:number, courseEditionId:number):Observable<any> {
    return from(this.connection.invoke('UnlockCourseEdition', courseId, courseEditionId));
  }

  public Disconnect() {
    if (this.connection?.state == "Connected") {
      this.connectionIntentionallyStopped = true;
      this.connection.stop();
    }
  }

  private SetClientMethods(): void {
    this.connection.onclose((error) => {
      this.connectionIntentionallyStopped = false;
      this.isConnected.next(false);
    });
    
    this.connection.on('LockCourseEdition', (courseId, courseEditionId) => {
      this.lastLockedCourseEdition.next({
        courseId: courseId,
        courseEditionId: courseEditionId
      });
    });

    this.connection.on('UnlockCourseEdition', (courseId, courseEditionId) => {
      this.lastUnlockedCourseEdition.next({
        courseId: courseId,
        courseEditionId: courseEditionId
      });
    });
  }

  ngOnDestroy() {
    this.Disconnect();
  }
}
