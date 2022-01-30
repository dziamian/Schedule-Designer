import { Injectable, OnDestroy } from '@angular/core';
import * as signalr from '@microsoft/signalr';
import { BehaviorSubject, from, Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { AccessToken } from 'src/app/others/AccessToken';
import { AddedSchedulePositions, MessageObject, ModifiedSchedulePositions, RemovedSchedulePositions, SchedulePosition } from 'src/app/others/CommunicationObjects';
import { ScheduledMove } from 'src/app/others/ScheduledMove';

@Injectable({
  providedIn: 'root'
})
export class SignalrService implements OnDestroy {
  readonly connectionUrl = 'http://localhost:5000/scheduleHub';
  
  connection:signalr.HubConnection;
  connectionIntentionallyStopped:boolean = false;
  connectionInitializing:boolean = false;
  
  isConnected:BehaviorSubject<boolean>
  lastLockedCourseEdition:BehaviorSubject<{courseId:number, courseEditionId:number, byAdmin:boolean}>
  lastLockedSchedulePositions:BehaviorSubject<SchedulePosition>
  lastUnlockedCourseEdition:BehaviorSubject<{courseId:number, courseEditionId:number}>
  lastUnlockedSchedulePositions:BehaviorSubject<SchedulePosition>
  lastAddedSchedulePositions:BehaviorSubject<AddedSchedulePositions>
  lastModifiedSchedulePositions:BehaviorSubject<ModifiedSchedulePositions>
  lastRemovedSchedulePositions:BehaviorSubject<RemovedSchedulePositions>

  lastAddedScheduledMove:BehaviorSubject<{scheduledMove:ScheduledMove, sourceSchedulePosition:SchedulePosition}>
  lastRemovedScheduledMove:BehaviorSubject<{moveId:number, sourceSchedulePosition:SchedulePosition}>
  lastAcceptedScheduledMove:BehaviorSubject<{moveId:number, sourceSchedulePosition:SchedulePosition}>

  lastResponse:BehaviorSubject<MessageObject>

  constructor() {
    this.isConnected = new BehaviorSubject<boolean>(false);
    
    this.lastLockedCourseEdition = new BehaviorSubject<{courseId:number, courseEditionId:number, byAdmin:boolean}>(
      {courseId: -1,courseEditionId: -1,byAdmin: false}
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
      new SchedulePosition(-1,-1,-1,-1,-1,[]),
      [])
    );
    
    this.lastRemovedSchedulePositions = new BehaviorSubject<RemovedSchedulePositions>(
      new RemovedSchedulePositions(
        [],-1,[],
        new SchedulePosition(-1,-1,-1,-1,-1,[]),
        [])
    );

    this.lastAddedScheduledMove = new BehaviorSubject<{scheduledMove:ScheduledMove, sourceSchedulePosition:SchedulePosition}>(
      {scheduledMove: new ScheduledMove(-1,-1,true),sourceSchedulePosition: new SchedulePosition(-1,-1,-1,-1,-1,[])}
    );

    this.lastRemovedScheduledMove = new BehaviorSubject<{moveId:number, sourceSchedulePosition:SchedulePosition}>(
      {moveId: -1,sourceSchedulePosition: new SchedulePosition(-1,-1,-1,-1,-1,[])}
    );

    this.lastAcceptedScheduledMove = new BehaviorSubject<{moveId:number, sourceSchedulePosition:SchedulePosition}>(
      {moveId: -1,sourceSchedulePosition: new SchedulePosition(-1,-1,-1,-1,-1,[])}
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
      this.connectionInitializing = true;
      
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
          this.connectionInitializing = false;
          console.log(this.connection.connectionId);
          observer.next();
          observer.complete();
        })
        .catch(() => {
          this.connectionInitializing = false;
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
    this.connection.invoke('AddSchedulePositions', 
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
    this.connection.invoke('ModifySchedulePositions',
      roomId, periodIndex, day, weeks, destRoomId, destPeriodIndex, destDay, destWeeks);
  }

  public RemoveSchedulePositions(
    roomId:number,
    periodIndex:number,
    day:number,
    weeks:number[]
  ):void {
    this.connection.invoke('RemoveSchedulePositions',
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
    destWeeks:number[],
    isProposition:boolean,
    message:string|null
  ):Observable<MessageObject> {
    return from(this.connection.invoke<MessageObject>('AddScheduledMove', 
    roomId, periodIndex, day, weeks, destRoomId, destPeriodIndex, destDay, destWeeks, isProposition, message))
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

  public AcceptProposition(
    roomId:number,
    periodIndex:number,
    day:number,
    weeks:number[],
    destRoomId:number,
    destPeriodIndex:number,
    destDay:number,
    destWeeks:number[]
  ):Observable<MessageObject> {
    return from(this.connection.invoke<MessageObject>('AcceptProposition', 
    roomId, periodIndex, day, weeks, destRoomId, destPeriodIndex, destDay, destWeeks))
      .pipe(map((result : any) => {
        const message = new MessageObject(result.statusCode);
        message.Message = result.message;
        return message;
      }));
  }

  public LockAllCourseEditions() {
    return from(this.connection.invoke<MessageObject>('LockAllCourseEditions'))
    .pipe(map((result : any) => {
      const message = new MessageObject(result.statusCode);
      message.Message = result.message;
      return message;
    }));
  }

  public UnlockAllCourseEditions() {
    return from(this.connection.invoke<MessageObject>('UnlockAllCourseEditions'))
    .pipe(map((result : any) => {
      const message = new MessageObject(result.statusCode);
      message.Message = result.message;
      return message;
    }));
  }

  public LockAllCourseEditionsForCourse(courseId: number) {
    return from(this.connection.invoke<MessageObject>('LockAllCourseEditionsForCourse', 
    courseId))
      .pipe(map((result : any) => {
        const message = new MessageObject(result.statusCode);
        message.Message = result.message;
        return message;
      }));
  }

  public UnlockAllCourseEditionsForCourse(courseId: number) {
    return from(this.connection.invoke<MessageObject>('UnlockAllCourseEditionsForCourse', 
    courseId))
      .pipe(map((result : any) => {
        const message = new MessageObject(result.statusCode);
        message.Message = result.message;
        return message;
      }));
  }

  public LockAllCoordinatorCourses(coordinatorId: number, courseId: number, courseEditionId: number) {
    return from(this.connection.invoke<MessageObject>('LockAllCoordinatorCourses', 
    coordinatorId, courseId, courseEditionId))
      .pipe(map((result : any) => {
        const message = new MessageObject(result.statusCode);
        message.Message = result.message;
        return message;
      }));
  }

  public UnlockAllCoordinatorCourses(coordinatorId: number, courseId: number, courseEditionId: number) {
    return from(this.connection.invoke<MessageObject>('UnlockAllCoordinatorCourses', 
    coordinatorId, courseId, courseEditionId))
      .pipe(map((result : any) => {
        const message = new MessageObject(result.statusCode);
        message.Message = result.message;
        return message;
      }));
  }

  public LockAllGroupCourses(groupId: number, courseId: number, courseEditionId: number) {
    return from(this.connection.invoke<MessageObject>('LockAllGroupCourses', 
    groupId, courseId, courseEditionId))
      .pipe(map((result : any) => {
        const message = new MessageObject(result.statusCode);
        message.Message = result.message;
        return message;
      }));
  }

  public UnlockAllGroupCourses(groupId: number, courseId: number, courseEditionId: number) {
    return from(this.connection.invoke<MessageObject>('UnlockAllGroupCourses', 
    groupId, courseId, courseEditionId))
      .pipe(map((result : any) => {
        const message = new MessageObject(result.statusCode);
        message.Message = result.message;
        return message;
      }));
  }

  public LockAllCoursesForGroupChange(originGroupId: number, destinationGroupId: number) {
    return from(this.connection.invoke<MessageObject>('LockAllCoursesForGroupChange', 
    originGroupId, destinationGroupId))
      .pipe(map((result : any) => {
        const message = new MessageObject(result.statusCode);
        message.Message = result.message;
        return message;
      }));
  }

  public UnlockAllCoursesForGroupChange(originGroupId: number, destinationGroupId: number) {
    return from(this.connection.invoke<MessageObject>('UnlockAllCoursesForGroupChange', 
    originGroupId, destinationGroupId))
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
    
    this.connection.on('LockCourseEdition', (courseId, courseEditionId, byAdmin) => {
      this.lastLockedCourseEdition.next({
        courseId: courseId,
        courseEditionId: courseEditionId,
        byAdmin: byAdmin
      });
    });

    this.connection.on('LockSchedulePositions', (
      courseId, courseEditionId,
      roomId, periodIndex, 
      day, weeks, byAdmin
    ) => {
      const schedulePosition = new SchedulePosition(
        courseId, courseEditionId,
        roomId, periodIndex, 
        day, weeks
      );
      schedulePosition.IsLocked = true;
      schedulePosition.IsLockedByAdmin = byAdmin;
      this.lastLockedSchedulePositions.next(schedulePosition);
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
      const schedulePosition = new SchedulePosition(
        courseId, courseEditionId,
        roomId, periodIndex, 
        day, weeks
      );
      schedulePosition.IsLocked = false;
      schedulePosition.IsLockedByAdmin = false;
      this.lastUnlockedSchedulePositions.next(schedulePosition);
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
      previousWeeks, newWeeks,
      movesIds
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
          ),
          movesIds
        )
      );
    });

    this.connection.on('RemovedSchedulePositions', (
      courseId, courseEditionId,
      groupsIds, mainGroupsAmount, coordinatorsIds,
      roomId, periodIndex,
      day, weeks,
      movesIds
    ) => {
      this.lastRemovedSchedulePositions.next(
        new RemovedSchedulePositions(
          groupsIds, mainGroupsAmount, coordinatorsIds, 
          new SchedulePosition(
            courseId, courseEditionId,
            roomId, periodIndex,
            day, weeks
          ),
          movesIds
        )
      );
    });

    this.connection.on('AddedScheduledMove', (
      moveId, userId, isConfirmed,
      courseId, courseEditionId,
      roomId, periodIndex,
      day, weeks
    ) => {
      this.lastAddedScheduledMove.next(
        {
          scheduledMove: new ScheduledMove(
            moveId, userId, isConfirmed
          ),
          sourceSchedulePosition: new SchedulePosition(
            courseId, courseEditionId,
            roomId, periodIndex,
            day, weeks
          )
        }
      );
    });

    this.connection.on('RemovedScheduledMove', (
      moveId,
      courseId, courseEditionId,
      roomId, periodIndex,
      day, weeks
    ) => {
      this.lastRemovedScheduledMove.next(
        {
          moveId: moveId,
          sourceSchedulePosition: new SchedulePosition(
            courseId, courseEditionId,
            roomId, periodIndex,
            day, weeks
          )
        }
      );
    });

    this.connection.on('AcceptedScheduledMove', (
      moveId,
      courseId, courseEditionId,
      roomId, periodIndex,
      day, weeks
    ) => {
      this.lastAcceptedScheduledMove.next(
        {
          moveId: moveId,
          sourceSchedulePosition: new SchedulePosition(
            courseId, courseEditionId,
            roomId, periodIndex,
            day, weeks
          )
        }
      );
    });

    this.connection.on('SendResponse', (messageObject) => {
      const message = new MessageObject(messageObject.statusCode);
      message.Message = messageObject.message;
      this.lastResponse.next(message);
    });
  }

  ngOnDestroy() {
    this.Disconnect();
  }
}
