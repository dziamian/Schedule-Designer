import { Injectable, OnDestroy } from '@angular/core';
import * as signalr from '@microsoft/signalr';
import { BehaviorSubject, from, Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { AccessToken } from 'src/app/others/AccessToken';
import { AddedSchedulePositions, MessageObject, ModifiedSchedulePositions, RemovedSchedulePositions, SchedulePosition } from 'src/app/others/CommunicationObjects';

@Injectable({
  providedIn: 'root'
})
export class SignalrService implements OnDestroy {
  readonly connectionUrl = 'http://localhost:5000/scheduleHub';
  
  connection:signalr.HubConnection;
  connectionIntentionallyStopped:boolean = false;
  
  isConnected:BehaviorSubject<boolean>
  lastLockedCourseEdition:BehaviorSubject<{courseId:number, courseEditionId:number}>
  lastLockedSchedulePositions:BehaviorSubject<SchedulePosition>
  lastUnlockedCourseEdition:BehaviorSubject<{courseId:number, courseEditionId:number}>
  lastUnlockedSchedulePositions:BehaviorSubject<SchedulePosition>
  lastAddedSchedulePositions:BehaviorSubject<AddedSchedulePositions>
  lastModifiedSchedulePositions:BehaviorSubject<ModifiedSchedulePositions>
  lastRemovedSchedulePositions:BehaviorSubject<RemovedSchedulePositions>

  lastResponse:BehaviorSubject<MessageObject>

  constructor() {
    this.isConnected = new BehaviorSubject<boolean>(false);
    
    this.lastLockedCourseEdition = new BehaviorSubject<{courseId:number, courseEditionId:number}>(
      {courseId: -1,courseEditionId: -1}
    );
    
    this.lastLockedSchedulePositions = new BehaviorSubject<SchedulePosition>(
      new SchedulePosition(-1,-1,-1,-1,-1,[])
    );
    
    this.lastUnlockedCourseEdition = new BehaviorSubject<{courseId:number, courseEditionId:number}>(
      {courseId: -1,courseEditionId: -1}
    );

    this.lastUnlockedSchedulePositions = new BehaviorSubject<SchedulePosition>(
      new SchedulePosition(-1,-1,-1,-1,-1,[])
    );
    
    this.lastAddedSchedulePositions = new BehaviorSubject<AddedSchedulePositions>(
      new AddedSchedulePositions([],-1,[], new SchedulePosition(-1,-1,-1,-1,-1,[]))
    );
    
    this.lastModifiedSchedulePositions = new BehaviorSubject<ModifiedSchedulePositions>(
      new ModifiedSchedulePositions([],-1,[],
      new SchedulePosition(-1,-1,-1,-1,-1,[]),
      new SchedulePosition(-1,-1,-1,-1,-1,[]))
    );
    
    this.lastRemovedSchedulePositions = new BehaviorSubject<RemovedSchedulePositions>(
      new RemovedSchedulePositions([],-1,[],new SchedulePosition(-1,-1,-1,-1,-1,[]))
    );

    this.lastResponse = new BehaviorSubject<MessageObject>(
      new MessageObject(-1)
    );
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

  public LockCourseEdition(courseId:number, courseEditionId:number):Observable<MessageObject> {
    return from(this.connection.invoke<MessageObject>('LockCourseEdition', courseId, courseEditionId))
    .pipe(map((result : any) => {
      const message = new MessageObject(result.statusCode);
      message.Message = result.message;
      return message;
    }));
  }

  public LockSchedulePositions(roomId:number, periodIndex:number, day:number, weeks:number[]) {
    return from(this.connection.invoke<MessageObject>('LockSchedulePositions', roomId, periodIndex, day, weeks))
    .pipe(map((result : any) => {
      const message = new MessageObject(result.statusCode);
      message.Message = result.message;
      return message;
    }));
  }

  public UnlockCourseEdition(courseId:number, courseEditionId:number):Observable<MessageObject> {
    return from(this.connection.invoke<MessageObject>('UnlockCourseEdition', courseId, courseEditionId))
    .pipe(map((result : any) => {
      const message = new MessageObject(result.statusCode);
      message.Message = result.message;
      return message;
    }));
  }

  public UnlockSchedulePositions(roomId:number, periodIndex:number, day:number, weeks:number[]) {
    return from(this.connection.invoke<MessageObject>('UnlockSchedulePositions', roomId, periodIndex, day, weeks))
    .pipe(map((result : any) => {
      const message = new MessageObject(result.statusCode);
      message.Message = result.message;
      return message;
    }));
  }

  public AddSchedulePositions(
    courseId:number,
    courseEditionId:number,
    roomId:number,
    periodIndex:number,
    day:number,
    weeks:number[]
  ):void {
    this.connection.invoke<MessageObject>('AddSchedulePositions', 
      courseId, courseEditionId, roomId, periodIndex, day, weeks);
  }

  public ModifySchedulePositions(
    roomId:number,
    periodIndex:number,
    day:number,
    weeks:number[],
    destRoomId:number,
    destPeriodIndex:number,
    destDay:number,
    destWeeks:number[]
  ):void {
    this.connection.invoke<MessageObject>('ModifySchedulePositions',
      roomId, periodIndex, day, weeks, destRoomId, destPeriodIndex, destDay, destWeeks);
  }

  public RemoveSchedulePositions(
    roomId:number,
    periodIndex:number,
    day:number,
    weeks:number[]
  ):void {
    this.connection.invoke<MessageObject>('RemoveSchedulePositions',
      roomId, periodIndex, day, weeks);
  }

  public AddScheduledMove(
    roomId:number,
    periodIndex:number,
    day:number,
    weeks:number[],
    destRoomId:number,
    destPeriodIndex:number,
    destDay:number,
    destWeeks:number[]
  ):Observable<MessageObject> {
    return from(this.connection.invoke<MessageObject>('AddScheduledMove', 
    roomId, periodIndex, day, weeks, destRoomId, destPeriodIndex, destDay, destWeeks))
      .pipe(map((result : any) => {
        const message = new MessageObject(result.statusCode);
        message.Message = result.message;
        return message;
      }));
  }

  public RemoveScheduledMove(
    roomId:number,
    periodIndex:number,
    day:number,
    weeks:number[],
    destRoomId:number,
    destPeriodIndex:number,
    destDay:number,
    destWeeks:number[]
  ):Observable<MessageObject> {
    return from(this.connection.invoke<MessageObject>('RemoveScheduledMove', 
    roomId, periodIndex, day, weeks, destRoomId, destPeriodIndex, destDay, destWeeks))
      .pipe(map((result : any) => {
        const message = new MessageObject(result.statusCode);
        message.Message = result.message;
        return message;
      }));
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

    this.connection.on('LockSchedulePositions', (
      courseId, courseEditionId,
      roomId, periodIndex, 
      day, weeks
    ) => {
      this.lastLockedSchedulePositions.next(new SchedulePosition(
        courseId, courseEditionId,
        roomId, periodIndex, 
        day, weeks
      ));
    });

    this.connection.on('UnlockCourseEdition', (courseId, courseEditionId) => {
      this.lastUnlockedCourseEdition.next({
        courseId: courseId,
        courseEditionId: courseEditionId
      });
    });

    this.connection.on('UnlockSchedulePositions', (
      courseId, courseEditionId,
      roomId, periodIndex, 
      day, weeks
    ) => {
      this.lastUnlockedSchedulePositions.next(new SchedulePosition(
        courseId, courseEditionId,
        roomId, periodIndex, 
        day, weeks
      ));
    });

    this.connection.on('AddedSchedulePositions', (
      courseId, courseEditionId,
      groupsIds, mainGroupsAmount, coordinatorsIds,
      roomId, periodIndex,
      day, weeks
    ) => {
      this.lastAddedSchedulePositions.next(
        new AddedSchedulePositions(
          groupsIds, mainGroupsAmount, coordinatorsIds, 
          new SchedulePosition(
            courseId, courseEditionId,
            roomId, periodIndex,
            day, weeks
          )
        )
      );
    });

    this.connection.on('ModifiedSchedulePositions', (
      courseId, courseEditionId,
      groupsIds, mainGroupsAmount, coordinatorsIds,
      previousRoomId, newRoomId,
      previousPeriodIndex, newPeriodIndex,
      previousDay, newDay,
      previousWeeks, newWeeks
    ) => {
      this.lastModifiedSchedulePositions.next(
        new ModifiedSchedulePositions(
          groupsIds, mainGroupsAmount, coordinatorsIds,
          new SchedulePosition(
            courseId, courseEditionId,
            previousRoomId, previousPeriodIndex,
            previousDay, previousWeeks
          ),
          new SchedulePosition(
            courseId, courseEditionId,
            newRoomId, newPeriodIndex,
            newDay, newWeeks
          )
        )
      );
    });

    this.connection.on('RemovedSchedulePositions', (
      courseId, courseEditionId,
      groupsIds, mainGroupsAmount, coordinatorsIds,
      roomId, periodIndex,
      day, weeks
    ) => {
      this.lastRemovedSchedulePositions.next(
        new RemovedSchedulePositions(
          groupsIds, mainGroupsAmount, coordinatorsIds, 
          new SchedulePosition(
            courseId, courseEditionId,
            roomId, periodIndex,
            day, weeks
          )
        )
      );
    });

    this.connection.on('SendResponse', (messageObject) => {
      const message = new MessageObject(messageObject.StatusCode);
      message.Message = messageObject.Message;
      this.lastResponse.next(message);
    });
  }

  ngOnDestroy() {
    this.Disconnect();
  }
}
