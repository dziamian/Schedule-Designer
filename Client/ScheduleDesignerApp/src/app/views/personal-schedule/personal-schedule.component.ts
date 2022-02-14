import { CdkDragDrop, CdkDragEnter, CdkDragRelease, CdkDragStart, transferArrayItem } from '@angular/cdk/drag-drop';
import { Component, OnInit, ViewChild } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { forkJoin, Subscription } from 'rxjs';
import { skip } from 'rxjs/operators';
import { MyCoursesComponent } from 'src/app/components/my-courses/my-courses.component';
import { ScheduleComponent } from 'src/app/components/schedule/schedule.component';
import { UserInfo } from 'src/app/others/Accounts';
import { SchedulePosition } from 'src/app/others/CommunicationObjects';
import { CourseEdition } from 'src/app/others/CourseEdition';
import { ModifyingScheduleData } from 'src/app/others/ModifyingScheduleData';
import { Settings } from 'src/app/others/Settings';
import { CourseType, RoomType } from 'src/app/others/Types';
import { ScheduleDesignerApiService } from 'src/app/services/ScheduleDesignerApiService/schedule-designer-api.service';
import { SignalrService } from 'src/app/services/SignalrService/signalr.service';
import { UsosApiService } from 'src/app/services/UsosApiService/usos-api.service';
import { SelectViewComponent } from 'src/app/components/select-view/select-view.component';
import { SelectViewDialogData, SelectViewDialogResult } from 'src/app/others/dialogs/SelectViewDialogData';
import { Filter } from 'src/app/others/Filter';
import { ScheduleInteractionService } from 'src/app/services/ScheduleInteractionService/schedule-interaction.service';
import { HubConnectionState } from '@microsoft/signalr';

/**
 * Komponent zawierający widok opcji Personal Schedule.
 */
@Component({
  selector: 'app-personal-schedule',
  templateUrl: './personal-schedule.component.html',
  styleUrls: ['./personal-schedule.component.css']
})
export class PersonalScheduleComponent implements OnInit {

  /** Instancja komponentu wyświetlającego nieułożone zajęcia na planie. */
  @ViewChild(MyCoursesComponent) myCoursesComponent!: MyCoursesComponent;
  /** Instancja komponentu wyświetlającego bieżący plan zajęć. */
  @ViewChild(ScheduleComponent) scheduleComponent!: ScheduleComponent;

  /** Informacje o zalogowanym użytkowniku. */
  userInfo: UserInfo
  /** Dane trybu modyfikacji. */
  data: ModifyingScheduleData = new ModifyingScheduleData();
  /** Określa czy tryb modyfikacji jest włączony. */
  isModifying: boolean = false;

  /** Ustawienia aplikacji. */
  settings: Settings;
  /** Kolekcja typów przedmiotów. */
  courseTypes: Map<number, CourseType>;
  /** Kolekcja typów pokojów. */
  roomTypes: Map<number, RoomType>;

  /** Filtr początkowy */
  filter: Filter;

  /** Tablica przechowująca tygodnie, które dotyczą poszczególnych widoków. */
  tabWeeks: number[][];
  /** Tablica przechowująca etykiety poszczególnych widoków. */
  tabLabels: string[];
  /** Indeks wybranego widoku. */
  currentTabIndex: number = 0;
  /** Aktualny filtr widoku (przechowuje informacje także o tym, czy widok powinien zostać przeładowany czy nie). */
  currentFilter: {weeks: number[], filter: Filter, tabSwitched: boolean, editable: boolean};
  
  /** Informuje czy dane zostały załadowane. */
  loading: boolean = true;
  /** Informuje o statusie połączenia z centrum. */
  connectionStatus: boolean = false;

  /** Utworzone subskrypcje odbierające powiadomienia z centrum SignalR. */
  signalrSubscriptions: Subscription[];
  isConnectedSubscription: Subscription;

  constructor(
    private store: Store<{userInfo: UserInfo}>,
    private signalrService: SignalrService,
    private scheduleDesignerApiService: ScheduleDesignerApiService,
    private usosApiService: UsosApiService,
    private scheduleInteractionService: ScheduleInteractionService,
    private snackBar: MatSnackBar,
    private dialogService: MatDialog,
    private router: Router
  ) {
    this.store.select('userInfo').subscribe((userInfo) => {
      if (userInfo.UserId == 0) {
        return;
      }
      this.userInfo = userInfo;
      this.filter = new Filter([this.userInfo.UserId], [], []);
      if (this.currentFilter) {
        this.currentFilter = {
          weeks: this.currentFilter.weeks,
          filter: this.filter,
          tabSwitched: this.currentFilter.tabSwitched,
          editable: this.currentFilter.editable
        };
      }
    });
  }

  /**
   * Metoda wywołująca jej odpowiednik z serwisu {@link ScheduleInteractionService}.
   */
  private updateBusyPeriods(): void {
    this.scheduleInteractionService.updateBusyPeriods(
      this.data, this.tabWeeks, this.currentTabIndex, this.settings, this.scheduleComponent
    );
  }

  /**
   * Metoda wywołująca jej odpowiednik z serwisu {@link ScheduleInteractionService}.
   */
  private selectCourseIfPossible(): void {
    this.scheduleInteractionService.selectCourseIfPossible(
      this.data, this.tabWeeks, this.currentTabIndex, this.scheduleComponent
    );
  }

  /**
   * Metoda inicjalizująca pasek wyboru widoków planu.
   */
  private initializeTabs(): void {
    this.tabLabels = ['Semester', 'Even Weeks', 'Odd Weeks', 'Custom (1)', 'Custom (2)'];
    this.tabWeeks = [[],[],[],[],[]];
    
    for (let i:number = 0; i < this.settings.TermDurationWeeks; ++i) {
      const weekNumber = i + 1;
      
      this.tabLabels.push('Week ' + weekNumber);
      
      this.tabWeeks[0].push(weekNumber);
      this.tabWeeks.push([weekNumber]);
      if (weekNumber % 2 == 0) {
        this.tabWeeks[1].push(weekNumber);
      } else {
        this.tabWeeks[2].push(weekNumber);
      }
    }
  }

  /**
   * Metoda inicjalizująca tabelę planu zajęć.
   */
  private initialize(): void {
    const periods = this.settings.Periods;
    const numberOfSlots = periods.length - 1;

    this.settings.DayLabels = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday'];
    this.settings.TimeLabels = [];
    
    for (let i:number = 0; i < numberOfSlots; ++i) {
      this.settings.TimeLabels.push(
        periods[i] + ' - ' + periods[i + 1]
      );
    }

    this.data.scheduleSlotsValidity = [];
    for (let j:number = 0; j < 5; ++j) {
      this.data.scheduleSlotsValidity.push([]);
      for (let i:number = 0; i < numberOfSlots; ++i) {
        this.data.scheduleSlotsValidity[j].push(false);
      }
    }
  }

  /**
   * Metoda wywołująca jej odpowiednik z serwisu {@link ScheduleInteractionService}.
   */
  private updateLockInMyCourses(
    courseId: number,
    courseEditionId: number
  ) {
    this.scheduleInteractionService.updateLockInMyCourses(
      courseId, courseEditionId, this.data, this.loading, this.snackBar
    );
  }

  /**
   * Metoda wywołująca jej odpowiednik z serwisu {@link ScheduleInteractionService}.
   */
  private updateLockInSchedule(position:SchedulePosition) {
    this.scheduleInteractionService.updateLockInSchedule(
      position, this.data, this.isModifying, this.settings, this.scheduleComponent, this.loading, this.snackBar
    );
  }

  /**
   * Metoda rozpoczynająca odbieranie bieżących informacji z centrum SignalR.
   */
  private setSignalrSubscriptions(): void {
    this.signalrSubscriptions = [];

    this.signalrSubscriptions.push(this.signalrService.lastLockedCourseEdition.pipe(skip(1)).subscribe(({courseId, courseEditionId, byAdmin}) => {
      this.updateLockInMyCourses(courseId, courseEditionId);
    }));

    this.signalrSubscriptions.push(this.signalrService.lastUnlockedCourseEdition.pipe(skip(1)).subscribe(({courseId, courseEditionId}) => {
      this.updateLockInMyCourses(courseId, courseEditionId);
    }));

    this.signalrSubscriptions.push(this.signalrService.lastLockedSchedulePositions.pipe(skip(1)).subscribe((lockedSchedulePositions) => {
      this.updateLockInSchedule(lockedSchedulePositions);
    }));

    this.signalrSubscriptions.push(this.signalrService.lastAddedSchedulePositions.pipe(skip(1)).subscribe((addedSchedulePositions) => {
      this.scheduleInteractionService.lastAddedSchedulePositionsReaction(
        addedSchedulePositions, this.data, this.tabWeeks, this.currentTabIndex, this.loading
      );
    }));

    this.signalrSubscriptions.push(this.signalrService.lastModifiedSchedulePositions.pipe(skip(1)).subscribe((modifiedSchedulePositions) => {
      this.scheduleInteractionService.lastModifiedSchedulePositionsReaction(
        modifiedSchedulePositions, this.data, this.tabWeeks, this.currentTabIndex, this.settings, this.scheduleComponent, this.loading, this.snackBar
      );
    }));

    this.signalrSubscriptions.push(this.signalrService.lastRemovedSchedulePositions.pipe(skip(1)).subscribe((removedSchedulePositions) => {
      this.scheduleInteractionService.lastRemovedSchedulePositionsReaction(
        removedSchedulePositions, this.data, this.tabWeeks, this.currentTabIndex, this.loading
      );
    }));
  }

  /**
   * Metoda przygotowująca komponent.
   * Pobiera dane niezbędne do wyświetlenia widoku opcji (ustawienia aplikacji, typy przedmiotów, etykiety ram czasowych itp.).
   * Rozpoczyna odbieranie bieżących informacji z centrum SignalR.
   */
  ngOnInit(): void {
    this.isConnectedSubscription = this.signalrService.isConnected.pipe(skip(1)).subscribe((status) => {
      this.connectionStatus = status;
      if (!status && !this.signalrService.connectionIntentionallyStopped) {
        this.snackBar.open("Connection with server has been lost. Please refresh the page to possibly reconnect.", "OK");
      }
    });

    this.setSignalrSubscriptions();

    forkJoin([
      this.scheduleDesignerApiService.GetSettings(),
      this.scheduleDesignerApiService.GetPeriods(),
      this.scheduleDesignerApiService.GetCourseTypes(),
      this.scheduleDesignerApiService.GetRoomTypes()
    ]).subscribe(([settings,periods,courseTypes,roomTypes]) => {
      this.connectionStatus = this.signalrService.connection?.state == HubConnectionState.Connected;

      this.settings = settings;
      this.settings.Periods = periods;
      this.courseTypes = courseTypes;
      this.roomTypes = roomTypes;

      this.initializeTabs();
      this.initialize();

      this.loading = false;

      this.OnTabChange(0, true);
    }, (error) => {
      if (error?.status == 401) {
        this.usosApiService.Deauthorize();

        this.snackBar.open('Session expired. Please log in again.', 'OK');
        this.router.navigate(['login']);
      } else if (!this.isConnectedSubscription.closed) {
        this.snackBar.open("Connection with server failed. Please refresh the page to try again.", "OK");
      }
    });
  }

  /**
   * Metoda wywoływana w trakcie zmiany widoku planu.
   * Powoduje zmianę filtra wyświetlanego planu oraz jego przeładowanie.
   * @param index Indeks zakładki w pasku
   * @param isFirst Określa czy dla załadowanego planu widok zmienia się po raz pierwszy
   */
  async OnTabChange(index: number, isFirst: boolean): Promise<void> {
    var tabSwitched = !isFirst;
    this.currentFilter = {
      weeks: [],
      filter: this.filter,
      tabSwitched: tabSwitched, 
      editable: false
    };
    
    const previousIndex = this.currentTabIndex;
    this.currentTabIndex = index;
    
    if (this.tabWeeks[index].length == 0) {
      var dialogData = new SelectViewDialogData(
        this.settings
      );
      
      var dialog = this.dialogService.open(SelectViewComponent, {
        disableClose: true,
        data: dialogData
      });
      var dialogResult: SelectViewDialogResult = await dialog.afterClosed().toPromise();

      if (dialogResult.SelectedWeeks.length == 0) {
        this.currentTabIndex = previousIndex;
        return;
      }

      this.tabWeeks[this.currentTabIndex] = dialogResult.SelectedWeeks;
      tabSwitched = false;
    }

    this.currentFilter = {
      weeks: this.tabWeeks[this.currentTabIndex], 
      filter: this.filter,
      tabSwitched: tabSwitched, 
      editable: (index == 3 || index == 4)
    };
  }

  /**
   * Metoda wywoływana w momencie załadowania widoku planu zajęć.
   * Jeśli potrzeba to nadpisuje stan pól w planie powodujących konflikty.
   * Jeśli to możliwe to próbuje odszukać zaznaczone wcześniej zajęcia przez użytkownika.
   */
  OnTabLoaded(): void {
    this.updateBusyPeriods();
    this.selectCourseIfPossible();
  }

  /**
   * Metoda uruchamiająca okno dialogowe w celu zmiany utworzonego wcześniej widoku niestandardowego.
   */
  async OnEditView(): Promise<void> {
    var dialogData = new SelectViewDialogData(
      this.settings
    );
    
    var dialog = this.dialogService.open(SelectViewComponent, {
      disableClose: true,
      data: dialogData
    });
    var dialogResult: SelectViewDialogResult = await dialog.afterClosed().toPromise();

    if (dialogResult.SelectedWeeks.length == 0) {
      return;
    }

    if (this.currentFilter.weeks.sort((a,b) => a - b).join(',') 
        === dialogResult.SelectedWeeks.sort((a,b) => a - b).join(',')) {
          return;
    }

    this.tabWeeks[this.currentTabIndex] = dialogResult.SelectedWeeks;

    this.currentFilter = {
      weeks: dialogResult.SelectedWeeks,
      filter: this.filter,
      tabSwitched: false, 
      editable: true
    };
  }

  /**
   * Metoda wywołująca jej odpowiednik z serwisu {@link ScheduleInteractionService}.
   */
  ModifySchedule() {
    this.isModifying = this.scheduleInteractionService.ModifySchedule(
      this.data, this.isModifying
    );
  }

  /**
   * Metoda wywołująca otworzenie nowego okna z bieżącym widokiem planu w celu wydrukowania go.
   */
  PrintSchedule() {
    if (this.scheduleComponent != null) {
      this.scheduleComponent.PrintSchedule();
    }
  }

  /**
   * Metoda wywołująca jej odpowiednik z serwisu {@link ScheduleInteractionService}.
   * @param event Zdarzenie upuszczenia panelu z zajęciami w strefie
   */
  async OnMyCoursesDrop(event: CdkDragDrop<CourseEdition[], CourseEdition[], CourseEdition>): Promise<void> {
    this.scheduleInteractionService.onMyCoursesDrop(
      event, this.data, this.tabWeeks, this.currentTabIndex, this.myCoursesComponent, this.scheduleComponent, this.snackBar
    );
  }

  /**
   * Metoda wywołująca jej odpowiednik z serwisu {@link ScheduleInteractionService}.
   * @param drag Zdarzenie najechania panelu z zajęciami na strefę
   */
  OnMyCoursesEnter(drag: CdkDragEnter<CourseEdition[]>): void {
    this.scheduleInteractionService.onMyCoursesEnter(
      drag, this.data
    );
  }

  /**
   * Metoda wywołująca jej odpowiednik z serwisu {@link ScheduleInteractionService}.
   * @param event Zdarzenie przeciągania panelu z zajęciami ze strefy
   */
  async OnMyCoursesStart(event: CdkDragStart<CourseEdition>): Promise<void> {
    this.scheduleInteractionService.onMyCoursesStart(
      event, this.data, this.tabWeeks, this.currentTabIndex, this.userInfo.IsStaff && this.userInfo.IsAdmin, this.settings, this.myCoursesComponent, this.scheduleComponent, this.snackBar
    );
  }

  /**
   * Metoda wywołująca jej odpowiednik z serwisu {@link ScheduleInteractionService}.
   * @param event Zdarzenie upuszczenia panelu z zajęciami w strefie
   */
  async OnScheduleDrop(event:CdkDragDrop<CourseEdition[], CourseEdition[], CourseEdition>): Promise<void> {
    this.scheduleInteractionService.onScheduleDrop(
      event, this.data, this.tabWeeks, this.currentTabIndex, this.settings, this.roomTypes, true, this.userInfo.IsStaff && this.userInfo.IsAdmin,
      this.currentFilter.filter, this.scheduleComponent, this.myCoursesComponent, this.dialogService, this.snackBar
    );
  }

  /**
   * Metoda wywołująca jej odpowiednik z serwisu {@link ScheduleInteractionService}.
   * @param drag Zdarzenie najechania panelu z zajęciami na strefę
   */
  OnScheduleEnter(drag: CdkDragEnter<CourseEdition[]>): void {
    this.scheduleInteractionService.onScheduleEnter(
      drag, this.data
    );
  }

  /**
   * Metoda wywołująca jej odpowiednik z serwisu {@link ScheduleInteractionService}.
   * @param event Zdarzenie przeciągania panelu z zajęciami ze strefy
   */
  async OnScheduleStart(event: CdkDragStart<CourseEdition>): Promise<void> {
    this.scheduleInteractionService.onScheduleStart(
      event, this.data, this.tabWeeks, this.currentTabIndex, this.userInfo.IsStaff && this.userInfo.IsAdmin, false, this.settings, this.scheduleComponent, this.snackBar
    )
  }

  /**
   * Metoda wywołująca jej odpowiednik z serwisu {@link ScheduleInteractionService}.
   * @param event Zdarzenie zakończenia przeciągania panelu z zajęciami
   */
  async OnRelease(event: CdkDragRelease<CourseEdition>): Promise<void> {
    this.scheduleInteractionService.onRelease(
      event, this.data, this.settings, this.scheduleComponent
    );
  }

  /**
   * Metoda wywołująca jej odpowiednik z serwisu {@link ScheduleInteractionService}.
   * @param event Informacje o wybranych zajęciach, ich stanie (czy są możliwe do modyfikowania w bieżącym widoku) i pozycji na planie
   */
  OnSelect(event: {courseEdition: CourseEdition, isDisabled: boolean, day: number, periodIndex: number}): void {
    this.scheduleInteractionService.onSelect(
      event, this.data, this.isModifying
    );
  }

  /**
   * Metoda wywołująca jej odpowiednik z serwisu {@link ScheduleInteractionService}.
   * @param event Informacje o wybranej przez użytkownika ramy czasowej na planie
   */
  async OnRoomSelect(event: {day: number, periodIndex: number}): Promise<void> {
    this.scheduleInteractionService.onRoomSelect(
      event, this.data, this.tabWeeks, this.currentTabIndex, this.settings, 
      this.roomTypes, true, this.userInfo.IsStaff && this.userInfo.IsAdmin, this.currentFilter.filter, this.scheduleComponent, this.dialogService, this.snackBar
    );
  }

  /**
   * Metoda wywołująca jej odpowiednik z serwisu {@link ScheduleInteractionService}.
   */
  async AddRoom(): Promise<void> {
    this.scheduleInteractionService.addRoom(
      this.data, this.roomTypes, this.dialogService, this.snackBar
    )
  }

  /**
   * Metoda wywołująca jej odpowiednik z serwisu {@link ScheduleInteractionService}.
   */
  async ChangeRoom(): Promise<void> {
    this.scheduleInteractionService.changeRoom(
      this.data, this.userInfo.IsStaff && this.userInfo.IsAdmin, this.settings, this.roomTypes, true, this.userInfo.IsStaff && this.userInfo.IsAdmin, this.currentFilter.filter, this.dialogService, this.snackBar
    );
  }
  
  /**
   * Metoda wywołująca jej odpowiednik z serwisu {@link ScheduleInteractionService}.
   */
  async ShowScheduledChanges(): Promise<void> {
    this.scheduleInteractionService.showScheduledChanges(
      this.data, this.settings, null, this.userInfo.IsStaff && this.userInfo.IsAdmin, this.isModifying, this.roomTypes, this.dialogService, this.snackBar
    );
  }

  /**
   * Metoda wywołująca jej odpowiednik z serwisu {@link ScheduleInteractionService}.
   */
  async Move(): Promise<void> {
    this.scheduleInteractionService.move(
      this.data, this.tabWeeks, this.currentTabIndex, this.userInfo.IsStaff && this.userInfo.IsAdmin, this.settings, this.scheduleComponent, this.snackBar
    );
  }

  CancelSelection(): void {
    this.scheduleInteractionService.cancelSelection(
      this.data
    );
  }

  OnMouseEnter(event: {day: number, periodIndex: number}): void {
    this.scheduleInteractionService.onMouseEnter(
      event, this.data
    )
  }

  OnMouseLeave(): void {
    this.scheduleInteractionService.onMouseLeave(
      this.data
    );
  }

  ngOnDestroy() {
    this.signalrSubscriptions.forEach(
      subscription => subscription.unsubscribe()
    );
    this.isConnectedSubscription.unsubscribe();
  }
}
