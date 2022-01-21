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
import { Account } from 'src/app/others/Accounts';
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
import { ResourceItem } from 'src/app/others/ResourcesTree';
import { ResourceTreeService } from 'src/app/services/ResourceTreeService/resource-tree.service';

@Component({
  selector: 'app-full-schedule',
  templateUrl: './full-schedule.component.html',
  styleUrls: ['./full-schedule.component.css']
})
export class FullScheduleComponent implements OnInit {

  @ViewChild(MyCoursesComponent) myCoursesComponent!: MyCoursesComponent;
  @ViewChild(ScheduleComponent) scheduleComponent!: ScheduleComponent;

  account: Account
  data: ModifyingScheduleData = new ModifyingScheduleData();
  isModifying: boolean = false;

  settings: Settings;
  courseTypes: Map<number, CourseType>;
  roomTypes: Map<number, RoomType>;

  tabWeeks: number[][];
  tabLabels: string[];
  currentTabIndex: number = 0;
  currentFilter: {weeks: number[], filter: Filter, tabSwitched: boolean, editable: boolean};
  currentResourceName: string;
  
  loading: boolean = true;
  connectionStatus: boolean = false;

  signalrSubscriptions: Subscription[];
  isConnectedSubscription: Subscription;

  constructor(
    private store: Store<{account: Account}>,
    private signalrService: SignalrService,
    private scheduleDesignerApiService: ScheduleDesignerApiService,
    private usosApiService: UsosApiService,
    private scheduleInteractionService: ScheduleInteractionService,
    private resourceTreeService: ResourceTreeService,
    private snackBar: MatSnackBar,
    private dialogService: MatDialog,
    private router: Router
  ) {
    this.store.select('account').subscribe((account) => {
      if (account.UserId == 0) {
        return;
      }
      this.account = account;
    });
    this.resourceTreeService.clearData();
    this.resourceTreeService.setAllResources();
  }

  canMakePropositions(): boolean {
    if (!this.currentResourceName) {
      return false;
    }
    return this.account?.Coordinator || this.account?.RepresentativeGroups.some(groupId => this.currentFilter.filter.GroupsIds.includes(groupId));
  }

  private updateBusyPeriods(): void {
    this.scheduleInteractionService.updateBusyPeriods(
      this.data, this.tabWeeks, this.currentTabIndex, this.settings, this.scheduleComponent
    );
  }

  private selectCourseIfPossible(): void {
    this.scheduleInteractionService.selectCourseIfPossible(
      this.data, this.tabWeeks, this.currentTabIndex, this.scheduleComponent
    );
  }

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

  private updateLockInMyCourses(
    courseId: number,
    courseEditionId: number
  ) {
    this.scheduleInteractionService.updateLockInMyCourses(
      courseId, courseEditionId, this.data, this.loading, this.snackBar
    );
  }

  private updateLockInSchedule(position:SchedulePosition) {
    this.scheduleInteractionService.updateLockInSchedule(
      position, this.data, this.isModifying, this.settings, this.scheduleComponent, this.loading, this.snackBar
    );
  }

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

    this.signalrSubscriptions.push(this.signalrService.lastUnlockedSchedulePositions.pipe(skip(1)).subscribe((unlockedSchedulePositions) => {
      this.updateLockInSchedule(unlockedSchedulePositions);
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

  ShowSchedule(resource: ResourceItem) {
    this.currentResourceName = resource.name;
    this.currentFilter = {
      weeks: this.tabWeeks[this.currentTabIndex],
      filter: resource.filter ?? new Filter([],[],[]),
      tabSwitched: false,
      editable: (this.currentTabIndex == 3 || this.currentTabIndex == 4)
    };
  }

  async OnTabChange(index: number, isFirst: boolean): Promise<void> {
    var tabSwitched = !isFirst;
    this.currentFilter = {
      weeks: [],
      filter: this.currentFilter.filter,
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
      filter: this.currentFilter.filter,
      tabSwitched: tabSwitched, 
      editable: (index == 3 || index == 4)
    };
  }

  OnTabLoaded(): void {
    this.updateBusyPeriods();
    this.selectCourseIfPossible();
  }

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
      filter: this.currentFilter.filter,
      tabSwitched: false, 
      editable: true
    };
  }

  ModifySchedule() {
    this.isModifying = this.scheduleInteractionService.ModifySchedule(
      this.data, this.isModifying
    );
  }

  PrintSchedule() {
    if (this.scheduleComponent != null) {
      this.scheduleComponent.PrintSchedule();
    }
  }

  async OnMyCoursesDrop(event: CdkDragDrop<CourseEdition[], CourseEdition[], CourseEdition>): Promise<void> {
    this.scheduleInteractionService.onMyCoursesDrop(
      event, this.data, this.tabWeeks, this.currentTabIndex, this.myCoursesComponent, this.scheduleComponent, this.snackBar
    );
  }

  OnMyCoursesEnter(drag: CdkDragEnter<CourseEdition[]>): void {
    this.scheduleInteractionService.onMyCoursesEnter(
      drag, this.data
    );
  }

  async OnMyCoursesStart(event: CdkDragStart<CourseEdition>): Promise<void> {
    this.scheduleInteractionService.onMyCoursesStart(
      event, this.data, this.tabWeeks, this.currentTabIndex, this.account.Admin, this.settings, this.myCoursesComponent, this.scheduleComponent, this.snackBar
    );
  }

  async OnScheduleDrop(event:CdkDragDrop<CourseEdition[], CourseEdition[], CourseEdition>): Promise<void> {
    this.scheduleInteractionService.onScheduleDrop(
      event, this.data, this.tabWeeks, this.currentTabIndex, this.settings, this.roomTypes, this.account.Admin, true,
      this.currentFilter.filter, this.scheduleComponent, this.myCoursesComponent, this.dialogService, this.snackBar
    );
  }

  OnScheduleEnter(drag: CdkDragEnter<CourseEdition[]>): void {
    this.scheduleInteractionService.onScheduleEnter(
      drag, this.data
    );
  }

  async OnScheduleStart(event: CdkDragStart<CourseEdition>): Promise<void> {
    this.scheduleInteractionService.onScheduleStart(
      event, this.data, this.tabWeeks, this.currentTabIndex, this.account.Admin, this.account.Coordinator, this.settings, this.scheduleComponent, this.snackBar
    )
  }

  async OnRelease(event: CdkDragRelease<CourseEdition>): Promise<void> {
    this.scheduleInteractionService.onRelease(
      event, this.data, this.settings, this.scheduleComponent
    );
  }

  OnSelect(event: {courseEdition: CourseEdition, isDisabled: boolean, day: number, periodIndex: number}): void {
    this.scheduleInteractionService.onSelect(
      event, this.data, this.isModifying
    );
  }

  async OnRoomSelect(event: {day: number, periodIndex: number}): Promise<void> {
    this.scheduleInteractionService.onRoomSelect(
      event, this.data, this.tabWeeks, this.currentTabIndex, this.settings, 
      this.roomTypes, this.account.Admin, true, this.currentFilter.filter, this.scheduleComponent, this.dialogService, this.snackBar
    );
  }

  async AddRoom(): Promise<void> {
    this.scheduleInteractionService.addRoom(
      this.data, this.roomTypes, this.dialogService, this.snackBar
    )
  }

  async ChangeRoom(): Promise<void> {
    this.scheduleInteractionService.changeRoom(
      this.data, this.account.Admin, this.settings, this.roomTypes, this.account.Admin, true, this.currentFilter.filter, this.dialogService, this.snackBar
    );
  }
  
  async ShowScheduledChanges(): Promise<void> {
    this.scheduleInteractionService.showScheduledChanges(
      this.data, this.settings, !this.account.Admin ? this.account.UserId : null, this.account.Admin, this.isModifying, this.roomTypes, this.dialogService, this.snackBar
    );
  }

  async Move(): Promise<void> {
    this.scheduleInteractionService.move(
      this.data, this.tabWeeks, this.currentTabIndex, this.account.Admin, this.settings, this.scheduleComponent, this.snackBar
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
