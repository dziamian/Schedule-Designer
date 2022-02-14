import { Injectable, OnDestroy } from '@angular/core';
import * as signalr from '@microsoft/signalr';
import { BehaviorSubject, from, Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { AccessToken } from 'src/app/others/AccessToken';
import { AddedSchedulePositions, MessageObject, ModifiedSchedulePositions, RemovedSchedulePositions, SchedulePosition } from 'src/app/others/CommunicationObjects';
import { ScheduledMove } from 'src/app/others/ScheduledMove';
import { environment } from 'src/environments/environment';

/**
 * Serwis odpowiadający za połączenie z centrum SignalR serwera.
 */
@Injectable({
  providedIn: 'root'
})
export class SignalrService implements OnDestroy {
  readonly connectionUrl = environment.baseSignalrUrl;
  
  /** Reprezentacja połączenia z centrum SignalR. */
  connection:signalr.HubConnection;
  /** Czy rozłączenie z centrum było celowe. */
  connectionIntentionallyStopped:boolean = false;
  /** Czy połączenie z centrum jest inicjowane. */
  connectionInitializing:boolean = false;
  
  /** Strumień, który przechowuje najnowszą informację na temat stanu połączenia z centrum. */
  isConnected:BehaviorSubject<boolean>
  /** Strumień, który przechowuje najnowszą informację na temat ostatnio zablokowanej edycji zajęć. */
  lastLockedCourseEdition:BehaviorSubject<{courseId:number, courseEditionId:number, byAdmin:boolean}>
  /** Strumień, który przechowuje najnowszą informację na temat ostatnio zablokowanych pozycji w planie. */
  lastLockedSchedulePositions:BehaviorSubject<SchedulePosition>
  /** Strumień, który przechowuje najnowszą informację na temat ostatnio odblokowanej edycji zajęć. */
  lastUnlockedCourseEdition:BehaviorSubject<{courseId:number, courseEditionId:number}>
  /** Strumień, który przechowuje najnowszą informację na temat ostatnio odblokowanych pozycji w planie. */
  lastUnlockedSchedulePositions:BehaviorSubject<SchedulePosition>
  /** Strumień, który przechowuje najnowszą informację na temat ostatnio dodanych pozycji na planie. */
  lastAddedSchedulePositions:BehaviorSubject<AddedSchedulePositions>
  /** Strumień, który przechowuje najnowszą informację na temat ostatnich zmian w planie. */
  lastModifiedSchedulePositions:BehaviorSubject<ModifiedSchedulePositions>
  /** Strumień, który przechowuje najnowszą informację na temat ostatnio usuniętych zajęciach z planu. */
  lastRemovedSchedulePositions:BehaviorSubject<RemovedSchedulePositions>

  /** Strumień, który przechowuje najnowszą informację na temat ostatnio dodanego zaplanowanego ruchu lub propozycji. */
  lastAddedScheduledMove:BehaviorSubject<{scheduledMove:ScheduledMove, sourceSchedulePosition:SchedulePosition}>
  /** Strumień, który przechowuje najnowszą informację na temat ostatnio usuniętego zaplanowanego ruchu lub propozycji. */
  lastRemovedScheduledMove:BehaviorSubject<{moveId:number, sourceSchedulePosition:SchedulePosition}>
  /** Strumień, który przechowuje najnowszą informację na temat ostatnio zaakceptowanej propozycji. */
  lastAcceptedScheduledMove:BehaviorSubject<{moveId:number, sourceSchedulePosition:SchedulePosition}>

  /** Strumień, który przechowuje najnowszą informację na temat ostatniej odpowiedzi o statusie wykonanej operacji. */
  lastResponse:BehaviorSubject<MessageObject>

  /**
   * Konstruktor ustawiający początkowe wartości obiektów, które emitują zdarzenia w momencie zmiany ich wartości.
   */
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

  /**
   * Pobranie nagłówka autoryzującego.
   * @param token Token dostępu
   * @returns Nagłówek autoryzujący
   */
  private GetAuthorizationHeader(token:any) {
    return {
      "AccessToken": token.key,
      "AccessTokenSecret": token.secret
    };
  }

  /**
   * Metoda ustanawiająca połączenie z centrum SignalR.
   * @returns 
   */
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
          observer.next();
          observer.complete();
        })
        .catch(() => {
          this.connectionInitializing = false;
          observer.error({status: -1});
        });
    });
  }

  /**
   * Metoda wywołująca wykonanie operacji zablokowania edycji zajęć w centrum.
   * @param courseId ID przedmiotu
   * @param courseEditionId ID edycji zajęć
   * @returns Strumień emitujący odpowiedź centrum na wywołanie operacji
   */
  public LockCourseEdition(courseId:number, courseEditionId:number):Observable<MessageObject> {
    return from(this.connection.invoke<MessageObject>('LockCourseEdition', courseId, courseEditionId))
    .pipe(map((result : any) => {
      const message = new MessageObject(result.statusCode);
      message.Message = result.message;
      return message;
    }));
  }

  /**
   * Metoda wywołująca wykonanie operacji zablokowania pozycji na planie w centrum.
   * @param roomId Identyfikator pokoju
   * @param periodIndex Indeks okienka czasowego w ciągu dnia
   * @param day Indeks dnia tygodnia
   * @param weeks Tygodnie, które mają być wzięte pod uwagę
   * @returns Strumień emitujący odpowiedź centrum na wywołanie operacji
   */
  public LockSchedulePositions(roomId:number, periodIndex:number, day:number, weeks:number[]) {
    return from(this.connection.invoke<MessageObject>('LockSchedulePositions', roomId, periodIndex, day, weeks))
    .pipe(map((result : any) => {
      const message = new MessageObject(result.statusCode);
      message.Message = result.message;
      return message;
    }));
  }

  /**
   * Metoda wywołująca wykonanie operacji odblokowania edycji zajęć w centrum.
   * @param courseId ID przedmiotu
   * @param courseEditionId ID edycji zajęć
   * @returns Strumień emitujący odpowiedź centrum na wywołanie operacji
   */
  public UnlockCourseEdition(courseId:number, courseEditionId:number):Observable<MessageObject> {
    return from(this.connection.invoke<MessageObject>('UnlockCourseEdition', courseId, courseEditionId))
    .pipe(map((result : any) => {
      const message = new MessageObject(result.statusCode);
      message.Message = result.message;
      return message;
    }));
  }

  /**
   * Metoda wywołująca wykonanie operacji odblokowania pozycji na planie w centrum.
   * @param roomId Identyfikator pokoju
   * @param periodIndex Indeks okienka czasowego w ciągu dnia
   * @param day Indeks dnia tygodnia
   * @param weeks Tygodnie, które mają być wzięte pod uwagę
   * @returns Strumień emitujący odpowiedź centrum na wywołanie operacji
   */
  public UnlockSchedulePositions(roomId:number, periodIndex:number, day:number, weeks:number[]) {
    return from(this.connection.invoke<MessageObject>('UnlockSchedulePositions', roomId, periodIndex, day, weeks))
    .pipe(map((result : any) => {
      const message = new MessageObject(result.statusCode);
      message.Message = result.message;
      return message;
    }));
  }

  /**
   * Metoda wywołująca wykonanie operacji dodania nowych pozycji na planie w centrum.
   * @param courseId ID przedmiotu
   * @param courseEditionId ID edycji zajęć
   * @param roomId Identyfikator pokoju
   * @param periodIndex Indeks okienka czasowego w ciągu dnia
   * @param day Indeks dnia tygodnia
   * @param weeks Tygodnie, które będą brane pod uwagę
   */
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

  /**
   * Metoda wywołująca wykonanie operacji wprowadzenia zmian na planie w centrum.
   * @param roomId Źródłowy identyfikator pokoju
   * @param periodIndex Źródłowy indeks okienka czasowego w ciągu dnia
   * @param day Źródłowy indeks dnia tygodnia
   * @param weeks Źródłowe tygodnie, które będą brane pod uwagę
   * @param destRoomId Docelowy identyfikator pokoju
   * @param destPeriodIndex Docelowy indeks okienka czasowego w ciągu dnia
   * @param destDay Docelowy indeks dnia tygodnia
   * @param destWeeks Docelowe tygodnie, które będą brane pod uwagę
   */
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

  /**
   * Metoda wywołująca wykonanie operacji usunięcia pozycji z planu w centrum.
   * @param roomId Identyfikator pokoju
   * @param periodIndex Indeks okienka czasowego w ciągu dnia
   * @param day Indeks dnia tygodnia
   * @param weeks Tygodnie, które będą brane pod uwagę
   */
  public RemoveSchedulePositions(
    roomId:number,
    periodIndex:number,
    day:number,
    weeks:number[]
  ):void {
    this.connection.invoke('RemoveSchedulePositions',
      roomId, periodIndex, day, weeks);
  }

  /**
   * Metoda wywołująca wykonanie operacji dodania zaplanowanej zmiany lub propozycji w centrum.
   * @param roomId Źródłowy identyfikator pokoju
   * @param periodIndex Źródłowy indeks okienka czasowego w ciągu dnia
   * @param day Źródłowy indeks dnia tygodnia
   * @param weeks Źródłowe tygodnie, które będą brane pod uwagę
   * @param destRoomId Docelowy identyfikator pokoju
   * @param destPeriodIndex Docelowy indeks okienka czasowego w ciągu dnia
   * @param destDay Docelowy indeks dnia tygodnia
   * @param destWeeks Docelowe tygodnie, które będą brane pod uwagę
   * @param isProposition Czy ruch jest propozycją
   * @param message Wiadomość załączona do propozycji
   * @returns Strumień emitujący odpowiedź centrum na wywołanie operacji
   */
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

  /**
   * Metoda wywołująca wykonanie operacji usunięcia zaplanowanej zmiany lub propozycji w centrum.
   * @param roomId Źródłowy identyfikator pokoju
   * @param periodIndex Źródłowy indeks okienka czasowego w ciągu dnia
   * @param day Źródłowy indeks dnia tygodnia
   * @param weeks Źródłowe tygodnie, które będą brane pod uwagę
   * @param destRoomId Docelowy identyfikator pokoju
   * @param destPeriodIndex Docelowy indeks okienka czasowego w ciągu dnia
   * @param destDay Docelowy indeks dnia tygodnia
   * @param destWeeks Docelowe tygodnie, które będą brane pod uwagę
   * @returns Strumień emitujący odpowiedź centrum na wywołanie operacji
   */
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

  /**
   * Metoda wywołująca wykonanie operacji zaakceptowania propozycji w centrum.
   * @param roomId Źródłowy identyfikator pokoju
   * @param periodIndex Źródłowy indeks okienka czasowego w ciągu dnia
   * @param day Źródłowy indeks dnia tygodnia
   * @param weeks Źródłowe tygodnie, które będą brane pod uwagę
   * @param destRoomId Docelowy identyfikator pokoju
   * @param destPeriodIndex Docelowy indeks okienka czasowego w ciągu dnia
   * @param destDay Docelowy indeks dnia tygodnia
   * @param destWeeks Docelowe tygodnie, które będą brane pod uwagę
   * @returns Strumień emitujący odpowiedź centrum na wywołanie operacji
   */
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

  /**
   * Metoda wywołująca wykonanie operacji zablokowania wszystkich edycji zajęć w centrum.
   * @returns Strumień emitujący odpowiedź centrum na wywołanie operacji
   */
  public LockAllCourseEditions() {
    return from(this.connection.invoke<MessageObject>('LockAllCourseEditions'))
    .pipe(map((result : any) => {
      const message = new MessageObject(result.statusCode);
      message.Message = result.message;
      return message;
    }));
  }

  /**
   * Metoda wywołująca wykonanie operacji odblokowania wszystkich edycji zajęć w centrum.
   * @returns Strumień emitujący odpowiedź centrum na wywołanie operacji
   */
  public UnlockAllCourseEditions() {
    return from(this.connection.invoke<MessageObject>('UnlockAllCourseEditions'))
    .pipe(map((result : any) => {
      const message = new MessageObject(result.statusCode);
      message.Message = result.message;
      return message;
    }));
  }

  /**
   * Metoda wywołująca wykonanie operacji zablokowania edycji zajęć dla konkretnego przedmiotu w centrum.
   * @param courseId ID przedmiotu
   * @returns Strumień emitujący odpowiedź centrum na wywołanie operacji
   */
  public LockAllCourseEditionsForCourse(courseId: number) {
    return from(this.connection.invoke<MessageObject>('LockAllCourseEditionsForCourse', 
    courseId))
      .pipe(map((result : any) => {
        const message = new MessageObject(result.statusCode);
        message.Message = result.message;
        return message;
      }));
  }

  /**
   * Metoda wywołująca wykonanie operacji odblokowania edycji zajęć dla konkretnego przedmiotu w centrum.
   * @param courseId ID przedmiotu
   * @returns Strumień emitujący odpowiedź centrum na wywołanie operacji
   */
  public UnlockAllCourseEditionsForCourse(courseId: number) {
    return from(this.connection.invoke<MessageObject>('UnlockAllCourseEditionsForCourse', 
    courseId))
      .pipe(map((result : any) => {
        const message = new MessageObject(result.statusCode);
        message.Message = result.message;
        return message;
      }));
  }

  /**
   * Metoda wywołująca wykonanie operacji zablokowania edycji zajęć i ich pozycji na planie, które dotyczą danego prowadzącego w centrum.
   * @param coordinatorId Identyfikator prowadzącego
   * @param courseId ID przedmiotu
   * @param courseEditionId ID edycji zajęć
   * @returns Strumień emitujący odpowiedź centrum na wywołanie operacji
   */
  public LockAllCoordinatorCourses(coordinatorId: number, courseId: number, courseEditionId: number) {
    return from(this.connection.invoke<MessageObject>('LockAllCoordinatorCourses', 
    coordinatorId, courseId, courseEditionId))
      .pipe(map((result : any) => {
        const message = new MessageObject(result.statusCode);
        message.Message = result.message;
        return message;
      }));
  }

  /**
   * Metoda wywołująca wykonanie operacji odblokowania edycji zajęć i ich pozycji na planie, które dotyczą danego prowadzącego w centrum.
   * @param coordinatorId Identyfikator prowadzącego
   * @param courseId ID przedmiotu
   * @param courseEditionId ID edycji zajęć
   * @returns Strumień emitujący odpowiedź centrum na wywołanie operacji
   */
  public UnlockAllCoordinatorCourses(coordinatorId: number, courseId: number, courseEditionId: number) {
    return from(this.connection.invoke<MessageObject>('UnlockAllCoordinatorCourses', 
    coordinatorId, courseId, courseEditionId))
      .pipe(map((result : any) => {
        const message = new MessageObject(result.statusCode);
        message.Message = result.message;
        return message;
      }));
  }

  /**
   * Metoda wywołująca wykonanie operacji zablokowania edycji zajęć i ich pozycji na planie, 
   * które dotyczą danego grupy (i jej grup nadrzędnych i podrzędnych) w centrum.
   * @param groupId Identyfikator grupy
   * @param courseId ID przedmiotu
   * @param courseEditionId ID edycji zajęć
   * @returns Strumień emitujący odpowiedź centrum na wywołanie operacji
   */
  public LockAllGroupCourses(groupId: number, courseId: number, courseEditionId: number) {
    return from(this.connection.invoke<MessageObject>('LockAllGroupCourses', 
    groupId, courseId, courseEditionId))
      .pipe(map((result : any) => {
        const message = new MessageObject(result.statusCode);
        message.Message = result.message;
        return message;
      }));
  }

  /**
   * Metoda wywołująca wykonanie operacji odblokowania edycji zajęć i ich pozycji na planie, 
   * które dotyczą danego grupy (i jej grup nadrzędnych i podrzędnych) w centrum.
   * @param groupId Identyfikator grupy
   * @param courseId ID przedmiotu
   * @param courseEditionId ID edycji zajęć
   * @returns Strumień emitujący odpowiedź centrum na wywołanie operacji
   */
  public UnlockAllGroupCourses(groupId: number, courseId: number, courseEditionId: number) {
    return from(this.connection.invoke<MessageObject>('UnlockAllGroupCourses', 
    groupId, courseId, courseEditionId))
      .pipe(map((result : any) => {
        const message = new MessageObject(result.statusCode);
        message.Message = result.message;
        return message;
      }));
  }

  /**
   * Metoda wywołująca wykonanie operacji zablokowania edycji zajęć i ich pozycji na planie, 
   * które dotyczą pierwszej grupy i jej grup podrzędnych oraz drugiej grupy i jej grup nadrzędnych w centrum.
   * @param originGroupId Identyfikator grupy źródłowej
   * @param destinationGroupId Identyfikator grupy docelowej
   * @returns Strumień emitujący odpowiedź centrum na wywołanie operacji
   */
  public LockAllCoursesForGroupChange(originGroupId: number, destinationGroupId?: number) {
    return from(this.connection.invoke<MessageObject>('LockAllCoursesForGroupChange', 
    originGroupId, destinationGroupId))
      .pipe(map((result : any) => {
        const message = new MessageObject(result.statusCode);
        message.Message = result.message;
        return message;
      }));
  }

  /**
   * Metoda wywołująca wykonanie operacji odblokowania edycji zajęć i ich pozycji na planie, 
   * które dotyczą pierwszej grupy i jej grup podrzędnych oraz drugiej grupy i jej grup nadrzędnych w centrum.
   * @param originGroupId Identyfikator grupy źródłowej
   * @param destinationGroupId Identyfikator grupy docelowej
   * @returns Strumień emitujący odpowiedź centrum na wywołanie operacji
   */
  public UnlockAllCoursesForGroupChange(originGroupId: number, destinationGroupId?: number) {
    return from(this.connection.invoke<MessageObject>('UnlockAllCoursesForGroupChange', 
    originGroupId, destinationGroupId))
      .pipe(map((result : any) => {
        const message = new MessageObject(result.statusCode);
        message.Message = result.message;
        return message;
      }));
  }

  /**
   * Metoda rozłączająca z centrum SignalR.
   */
  public Disconnect() {
    if (this.connection?.state == "Connected") {
      this.connectionIntentionallyStopped = true;
      this.connection.stop();
    }
  }

  /**
   * Metoda ustawiająca reakcje na odebranie poszczególnych informacji z centrum SignalR.
   */
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
