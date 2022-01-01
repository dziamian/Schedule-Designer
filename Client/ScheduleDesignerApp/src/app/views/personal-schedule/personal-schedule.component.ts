import { CdkDragDrop, CdkDragEnter, CdkDragRelease, CdkDragStart, transferArrayItem } from '@angular/cdk/drag-drop';
import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { forkJoin } from 'rxjs';
import { skip } from 'rxjs/operators';
import { AddRoomSelectionComponent } from 'src/app/components/add-room-selection/add-room-selection.component';
import { MyCoursesComponent } from 'src/app/components/my-courses/my-courses.component';
import { RoomSelectionComponent } from 'src/app/components/room-selection/room-selection.component';
import { ScheduleComponent } from 'src/app/components/schedule/schedule.component';
import { ScheduledChangesViewComponent } from 'src/app/components/scheduled-changes-view/scheduled-changes-view.component';
import { Account } from 'src/app/others/Accounts';
import { AddRoomSelectionDialogData, AddRoomSelectionDialogResult } from 'src/app/others/dialogs/AddRoomSelectionDialog';
import { MessageObject, SchedulePosition } from 'src/app/others/CommunicationObjects';
import { CourseEdition } from 'src/app/others/CourseEdition';
import { ModifyingScheduleData } from 'src/app/others/ModifyingScheduleData';
import { RoomSelectionDialogData, RoomSelectionDialogResult, RoomSelectionDialogStatus } from 'src/app/others/dialogs/RoomSelectionDialog';
import { ScheduledChangesDialogData, ScheduledChangesDialogResult } from 'src/app/others/dialogs/ScheduledChangesDialog';
import { SelectedCourseEdition } from 'src/app/others/SelectedCourseEdition';
import { Settings } from 'src/app/others/Settings';
import { CourseType, RoomType } from 'src/app/others/Types';
import { ScheduleDesignerApiService } from 'src/app/services/ScheduleDesignerApiService/schedule-designer-api.service';
import { SignalrService } from 'src/app/services/SignalrService/signalr.service';
import { UsosApiService } from 'src/app/services/UsosApiService/usos-api.service';
import { SelectViewComponent } from 'src/app/components/select-view/select-view.component';
import { SelectViewDialogData, SelectViewDialogResult } from 'src/app/others/dialogs/SelectViewDialogData';
import { Filter } from 'src/app/others/Filter';

@Component({
  selector: 'app-personal-schedule',
  templateUrl: './personal-schedule.component.html',
  styleUrls: ['./personal-schedule.component.css']
})
export class PersonalScheduleComponent implements OnInit {

  @ViewChild(MyCoursesComponent) myCoursesComponent!: MyCoursesComponent;
  @ViewChild(ScheduleComponent) scheduleComponent!: ScheduleComponent;

  account: Account
  data: ModifyingScheduleData = new ModifyingScheduleData();

  settings: Settings;
  courseTypes: Map<number, CourseType>;
  roomTypes: Map<number, RoomType>;
  filter: Filter;

  tabWeeks: number[][];
  tabLabels: string[];
  currentTabIndex: number = 0;
  currentFilter: {weeks: number[], filter: Filter, tabSwitched: boolean, editable: boolean};
  
  loading: boolean = true;
  connectionStatus: boolean = false;

  constructor(
    private store: Store<{account: Account}>,
    private signalrService: SignalrService,
    private scheduleDesignerApiService: ScheduleDesignerApiService,
    private usosApiService: UsosApiService,
    private snackBar: MatSnackBar,
    private dialogService: MatDialog,
    private router: Router
  ) {
    this.store.select('account').subscribe((account) => {
      if (account.UserId == 0) {
        return;
      }
      this.account = account;
      this.filter = new Filter([this.account.UserId], [], []);
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

  private updateBusyPeriods(): void {
    if (this.data.currentSelectedCourseEdition != null && this.data.currentSelectedCourseEdition.IsMoving) {
      this.scheduleDesignerApiService.GetBusyPeriods(
        this.data.currentSelectedCourseEdition.CourseEdition.CourseId, 
        this.data.currentSelectedCourseEdition.CourseEdition.CourseEditionId,
        this.tabWeeks[this.currentTabIndex]
      ).subscribe(busySlots => {
        const numberOfSlots = this.settings.Periods.length - 1;
        let busySlotIndex = 0;
        
        for (let i = 0; i < this.scheduleComponent.scheduleSlots.length; ++i) {
          if (i != (busySlots[busySlotIndex]?.Day - 1) * numberOfSlots + (busySlots[busySlotIndex]?.PeriodIndex - 1)) {
            this.data.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = true;
          } else {
            this.data.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = false;
            ++busySlotIndex;
          }
        }
      });
    }
  }

  private selectCourseIfPossible(): void {
    if (this.data.currentSelectedCourseEdition != null) {
      if (this.tabWeeks[this.currentTabIndex].sort((a,b) => a - b).join(',') 
        === this.data.currentSelectedCourseEdition.CourseEdition.Weeks?.sort((a,b) => a - b).join(',')) {
          this.scheduleComponent.schedule[this.data.currentSelectedCourseEdition.Day][this.data.currentSelectedCourseEdition.PeriodIndex]
            .forEach((courseEdition) => {
              if (this.data.currentSelectedCourseEdition?.CourseEdition.CourseId == courseEdition.CourseId
                && this.data.currentSelectedCourseEdition?.CourseEdition.CourseEditionId == courseEdition.CourseEditionId
                && this.data.currentSelectedCourseEdition?.CourseEdition.Room?.RoomId == courseEdition.Room?.RoomId) {
                  this.data.currentSelectedCourseEdition.CourseEdition = courseEdition;
                  this.data.currentSelectedCourseEdition.CourseEdition.IsCurrentlyActive = true;
                  this.data.currentSelectedCourseEdition.CanChangeRoom = true;
                  this.data.currentSelectedCourseEdition.CanMakeMove = true;
              }
            });
      } else {
        this.data.currentSelectedCourseEdition.CanChangeRoom = false;
        if (this.tabWeeks[this.currentTabIndex].length == this.data.currentSelectedCourseEdition.CourseEdition.Weeks?.length) {
          this.data.currentSelectedCourseEdition.CanMakeMove = true;
        }
      }
    }
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

  private updateLockInSchedule(position:SchedulePosition, value:boolean) {
    if (this.loading) {
      return;
    }

    const courseId = position.CourseId;
    const courseEditionId = position.CourseEditionId;
    const roomId = position.RoomId;
    const day = position.Day - 1;
    const periodIndex = position.PeriodIndex - 1;
    const weeks = position.Weeks;

    if (this.data.currentSelectedCourseEdition != null 
      && this.data.currentSelectedCourseEdition.CourseEdition.CourseId == courseId
      && this.data.currentSelectedCourseEdition.CourseEdition.CourseEditionId == courseEditionId
      && this.data.currentSelectedCourseEdition.CourseEdition.Room?.RoomId == roomId
      && this.data.currentSelectedCourseEdition.Day == day && this.data.currentSelectedCourseEdition.PeriodIndex == periodIndex
      && this.data.currentSelectedCourseEdition.CourseEdition.Weeks?.some(r => weeks.includes(r))) {
        this.data.currentSelectedCourseEdition.IsMoving = false;
        this.data.currentSelectedCourseEdition.CourseEdition.IsCurrentlyActive = false;
        this.data.currentSelectedCourseEdition = null;
    }

    //TODO: admin took control: currentDrag, all currentDialogs except addRoom,scheduledChanges
  }

  private setSignalrSubscriptions(): void {
    this.signalrService.lastLockedSchedulePositions.pipe(skip(1)).subscribe((lockedSchedulePositions) => {
      this.updateLockInSchedule(lockedSchedulePositions, true);
    });

    this.signalrService.lastUnlockedSchedulePositions.pipe(skip(1)).subscribe((unlockedSchedulePositions) => {
      this.updateLockInSchedule(unlockedSchedulePositions, false);
    });

    this.signalrService.lastAddedSchedulePositions.pipe(skip(1)).subscribe((addedSchedulePositions) => {
      if (this.loading) {
        return;
      }
      
      const schedulePosition = addedSchedulePositions.SchedulePosition;
      const commonWeeks = schedulePosition.Weeks.filter(week => this.tabWeeks[this.currentTabIndex].includes(week));

      //active dragged and selected fields update
      const event = this.data.currentDragEvent?.source;
      const item = (event != undefined) ? this.data.currentDragEvent?.source.data : this.data.currentSelectedCourseEdition?.CourseEdition;

      if (item == undefined || commonWeeks.length == 0) {
        return;
      }

      if (item.Coordinators.map(c => c.UserId).some(c => addedSchedulePositions.CoordinatorsIds.includes(c))
        || item.Groups.map(g => g.GroupId).some(g => addedSchedulePositions.GroupsIds.includes(g))) {
          if (this.data.currentDragEvent != null || this.data.currentSelectedCourseEdition?.IsMoving) {
            this.data.scheduleSlotsValidity[schedulePosition.Day - 1][schedulePosition.PeriodIndex - 1] = false;
          }
          
          if (event?.dropContainer.id === 'my-courses') {
            const connectedTo = event.dropContainer.connectedTo as string[];
            const id = `${schedulePosition.Day - 1},${schedulePosition.PeriodIndex - 1}`;
            if (this.data.currentDragEvent != null) {
              this.data.currentDragEvent.source.dropContainer.connectedTo = connectedTo.filter(e => e !== id);
              event.dropContainer._dropListRef.enter(
                event._dragRef,
                0,0
              );
            }
          }
          else if (event != undefined && this.data.currentDropContainerIndexes[0] !== -1 && this.data.currentDropContainerIndexes[1] !== -1) {
            if (this.data.currentDropContainerIndexes[0] == schedulePosition.Day - 1 
              && this.data.currentDropContainerIndexes[1] == schedulePosition.PeriodIndex - 1) {
                this.data.isCurrentMoveValid = false;
            }
          }
      }
    });

    this.signalrService.lastModifiedSchedulePositions.pipe(skip(1)).subscribe((modifiedSchedulePositions) => {
      if (this.loading) {
        return;
      }

      const srcSchedulePosition = modifiedSchedulePositions.SourceSchedulePosition;
      const dstSchedulePosition = modifiedSchedulePositions.DestinationSchedulePosition;
      const commonWeeks = dstSchedulePosition.Weeks.filter(week => this.tabWeeks[this.currentTabIndex].includes(week));
      const movesIds = modifiedSchedulePositions.MovesIds;

      //scheduled change occurred
      const currentDrag = this.data.currentDragEvent?.source;
      if (currentDrag != null) {
        const currentIndexes = (currentDrag.dropContainer.id !== 'my-courses') ? this.getIndexes(currentDrag.dropContainer.id) : [-1,-1];
        if (currentDrag.data.CourseId == srcSchedulePosition.CourseId 
          && currentDrag.data.CourseEditionId == srcSchedulePosition.CourseEditionId && currentDrag.data.Room?.RoomId == srcSchedulePosition.RoomId
          && currentIndexes[1] == srcSchedulePosition.PeriodIndex - 1 && currentIndexes[0] == srcSchedulePosition.Day - 1
          && currentDrag.data.Weeks?.some(c => srcSchedulePosition.Weeks.includes(c))) {
            if (this.data.currentDragEvent != null) {
              this.data.currentDragEvent.source.dropContainer._dropListRef.enter(
                this.data.currentDragEvent.source._dragRef,
                0,0
              );
              document.dispatchEvent(new Event('mouseup'));
            }
        }
      }

      //scheduled change occurred
      const selectedCourseEdition = this.data.currentSelectedCourseEdition;
      if (selectedCourseEdition != null) {
        if (selectedCourseEdition.CourseEdition.CourseId == srcSchedulePosition.CourseId
          && selectedCourseEdition.CourseEdition.CourseEditionId == srcSchedulePosition.CourseEditionId 
          && selectedCourseEdition.CourseEdition.Room?.RoomId == srcSchedulePosition.RoomId
          && selectedCourseEdition.PeriodIndex == srcSchedulePosition.PeriodIndex - 1 && selectedCourseEdition.Day == srcSchedulePosition.Day - 1
          && selectedCourseEdition.CourseEdition.Weeks?.some(c => srcSchedulePosition.Weeks.includes(c))) {
            if (this.data.currentSelectedCourseEdition != null) {
              this.data.currentSelectedCourseEdition.CourseEdition.IsCurrentlyActive = false;
            }
            this.data.currentSelectedCourseEdition = null;
          }
      }

      //scheduled change occurred
      const currentDialogData = this.data.currentRoomSelectionDialog?.componentInstance.data;
      if (currentDialogData != null) {
        const currentIndexes = currentDialogData.SrcIndexes;
        if (currentDialogData.CourseEdition.CourseId == srcSchedulePosition.CourseId
          && currentDialogData.CourseEdition.CourseEditionId == srcSchedulePosition.CourseEditionId && currentDialogData.CourseEdition.Room?.RoomId == srcSchedulePosition.RoomId
          && currentIndexes[1] == srcSchedulePosition.PeriodIndex - 1 && currentIndexes[0] == srcSchedulePosition.Day - 1
          && currentDialogData.CourseEdition.Weeks?.some(c => srcSchedulePosition.Weeks.includes(c))) {
            if (this.data.currentRoomSelectionDialog != null) {
              this.data.currentRoomSelectionDialog.close(RoomSelectionComponent.CANCELED);
            }
        }
      }

      //active dragged and selected fields update
      const event = this.data.currentDragEvent?.source;
      const item = (event != undefined) ? this.data.currentDragEvent?.source.data : this.data.currentSelectedCourseEdition?.CourseEdition;
      
      if (item == undefined || commonWeeks.length == 0) {
        return;
      }

      if (item.Coordinators.map(c => c.UserId).some(c => modifiedSchedulePositions.CoordinatorsIds.includes(c))
        || item.Groups.map(g => g.GroupId).some(g => modifiedSchedulePositions.GroupsIds.includes(g))) {
        if (this.data.currentDragEvent != null || this.data.currentSelectedCourseEdition?.IsMoving) {
          this.data.scheduleSlotsValidity[dstSchedulePosition.Day - 1][dstSchedulePosition.PeriodIndex - 1] = false;
        }
        
        
        if (event?.dropContainer.id === 'my-courses') {
          const connectedTo = event.dropContainer.connectedTo as string[];
          const id = `${dstSchedulePosition.Day - 1},${dstSchedulePosition.PeriodIndex - 1}`;
          if (this.data.currentDragEvent != null) {
            this.data.currentDragEvent.source.dropContainer.connectedTo = connectedTo.filter(e => e !== id);
            event.dropContainer._dropListRef.enter(
              event._dragRef,
              0,0
            );
          }
        }
        else if (event != undefined && this.data.currentDropContainerIndexes[0] !== -1 && this.data.currentDropContainerIndexes[1] !== -1) {
          if (this.data.currentDropContainerIndexes[0] == dstSchedulePosition.Day - 1 
            && this.data.currentDropContainerIndexes[1] == dstSchedulePosition.PeriodIndex - 1) {
              this.data.isCurrentMoveValid = false;
          }
        }

        this.scheduleDesignerApiService.IsPeriodBusy(
          srcSchedulePosition.CourseId, srcSchedulePosition.CourseEditionId,
          srcSchedulePosition.PeriodIndex, srcSchedulePosition.Day,
          srcSchedulePosition.Weeks.filter(week => this.tabWeeks[this.currentTabIndex].includes(week))
        ).subscribe((isBusy) => {
          if (this.data.currentDragEvent != null || this.data.currentSelectedCourseEdition?.IsMoving) {
            this.data.scheduleSlotsValidity[srcSchedulePosition.Day - 1][srcSchedulePosition.PeriodIndex - 1] = !isBusy;
          }

          if (isBusy) {
            return;
          }

          if (event?.dropContainer.id === 'my-courses') {
            const connectedTo = event.dropContainer.connectedTo as string[];
            const id = `${srcSchedulePosition.Day - 1},${srcSchedulePosition.PeriodIndex - 1}`;
            if (this.data.currentDragEvent != null) {
              connectedTo.push(id);
              this.data.currentDragEvent.source.dropContainer.connectedTo = connectedTo;
              event.dropContainer._dropListRef.enter(
                event._dragRef,
                0,0
              );
            }
          } else if (event != undefined && this.data.currentDropContainerIndexes[0] !== -1 && this.data.currentDropContainerIndexes[1] !== -1) {
            if (this.data.currentDropContainerIndexes[0] == srcSchedulePosition.Day - 1 
              && this.data.currentDropContainerIndexes[1] == srcSchedulePosition.PeriodIndex - 1) {
                this.data.isCurrentMoveValid = true;
            }
          }
          
        });
      }
    });

    this.signalrService.lastRemovedSchedulePositions.pipe(skip(1)).subscribe((removedSchedulePositions) => {
      if (this.loading) {
        return;
      }

      const schedulePosition = removedSchedulePositions.SchedulePosition;
      const commonWeeks = schedulePosition.Weeks.filter(week => this.tabWeeks[this.currentTabIndex].includes(week));

      //active dragged and selected fields update
      const item = this.data.currentDragEvent?.source.data;
      const event = this.data.currentDragEvent?.source;
      if (item == undefined || commonWeeks.length == 0) {
        return;
      }

      if (item.Coordinators.map(c => c.UserId).some(c => removedSchedulePositions.CoordinatorsIds.includes(c))
        || item.Groups.map(g => g.GroupId).some(g => removedSchedulePositions.GroupsIds.includes(g))) {

        this.scheduleDesignerApiService.IsPeriodBusy(
          schedulePosition.CourseId, schedulePosition.CourseEditionId,
          schedulePosition.PeriodIndex, schedulePosition.Day,
          schedulePosition.Weeks.filter(week => this.tabWeeks[this.currentTabIndex].includes(week))
        ).subscribe((isBusy) => {
          if (this.data.currentDragEvent != null || this.data.currentSelectedCourseEdition?.IsMoving) {
            this.data.scheduleSlotsValidity[schedulePosition.Day - 1][schedulePosition.PeriodIndex - 1] = !isBusy;
          }

          if (isBusy) {
            return;
          }

          if (event?.dropContainer.id === 'my-courses') {
            const connectedTo = event.dropContainer.connectedTo as string[];
            const id = `${schedulePosition.Day - 1},${schedulePosition.PeriodIndex - 1}`;
            if (this.data.currentDragEvent != null) {
              connectedTo.push(id);
              this.data.currentDragEvent.source.dropContainer.connectedTo = connectedTo;
              event.dropContainer._dropListRef.enter(
                event._dragRef,
                0,0
              );
            }
          } else if (event != undefined && this.data.currentDropContainerIndexes[0] !== -1 && this.data.currentDropContainerIndexes[1] !== -1) {
            if (this.data.currentDropContainerIndexes[0] == schedulePosition.Day - 1 
              && this.data.currentDropContainerIndexes[1] == schedulePosition.PeriodIndex - 1) {
                this.data.isCurrentMoveValid = true;
            }
          }
          
        });
      }
    });
  }

  ngOnInit(): void {
    let isConnectedSubscription = this.signalrService.isConnected.pipe(skip(1)).subscribe((status) => {
      this.connectionStatus = status;
      if (!status && !this.signalrService.connectionIntentionallyStopped) {
        this.snackBar.open("Connection with server has been lost. Please refresh the page to possibly reconnect.", "OK");
      }
    });

    this.setSignalrSubscriptions();

    forkJoin([
      this.signalrService.InitConnection(),
      this.scheduleDesignerApiService.GetSettings(),
      this.scheduleDesignerApiService.GetPeriods(),
      this.scheduleDesignerApiService.GetCourseTypes(),
      this.scheduleDesignerApiService.GetRoomTypes()
    ]).subscribe(([,settings,periods,courseTypes,roomTypes]) => {
      this.connectionStatus = true;
      
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
      } else if (!isConnectedSubscription.closed) {
        this.snackBar.open("Connection with server failed. Please refresh the page to try again.", "OK");
      }
    });
  }

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
      filter: this.filter,
      tabSwitched: false, 
      editable: true
    };
  }

  private getIndexes(id: string): number[] {
    const indexes = id.split(',');
    return [
      Number.parseInt(indexes[0]),
      Number.parseInt(indexes[1])
    ];
  }

  async OnMyCoursesDrop(event: CdkDragDrop<CourseEdition[], CourseEdition[], CourseEdition>): Promise<void> {
    if (this.data.isCurrentDragCanceled) {
      this.data.currentDragEvent = null;
      if (this.data.currentSelectedCourseEdition != null) {
        this.data.currentSelectedCourseEdition.CourseEdition.IsCurrentlyActive = false;
      }
      this.data.currentSelectedCourseEdition = null;
      this.data.isCurrentMoveValid = null;
      event.item.data.IsCurrentlyActive = false;
      return;
    }

    const courseEdition = event.item.data;
    const previousContainer = event.previousContainer;
    const currentContainer = event.container;
    const weeks = this.tabWeeks[this.currentTabIndex];
    const isScheduleSource = event.previousContainer.id !== 'my-courses';

    if (isScheduleSource) {
      const previousIndexes = this.getIndexes(previousContainer.id);

      try {
        const result = await new Promise<MessageObject>((resolve, reject) => {
          const responseSubscription = this.signalrService.lastResponse.pipe(skip(1))
          .subscribe((messageObject) => {
            responseSubscription.unsubscribe();
            resolve(messageObject);
          },() => {
            reject();
          });
          this.signalrService.RemoveSchedulePositions(
            courseEdition.Room!.RoomId, previousIndexes[1] + 1,
            previousIndexes[0] + 1, weeks
          );
          setTimeout(() => responseSubscription.unsubscribe(), 15000);
        });
        
        if (result.StatusCode >= 400) {
          throw result;
        }

        transferArrayItem(
          previousContainer.data,
          currentContainer.data,
          event.previousIndex,
          event.currentIndex
        );
        courseEdition.Room = null;
        courseEdition.CurrentAmount = courseEdition.Weeks?.length ?? 0;
        courseEdition.Weeks = null;
        courseEdition.ScheduledMoves = [];

        this.myCoursesComponent.myCourses.forEach((element) => {
          if (element.CourseId == courseEdition.CourseId 
            && element.CourseEditionId == courseEdition.CourseEditionId) {
              element.ScheduleAmount -= weeks.length;
          }
        });

        for (let i = 0; i < this.scheduleComponent.schedule.length; ++i) {
          for (let j = 0; j < this.scheduleComponent.schedule[i].length; ++j) {
            for (let k = 0; k < this.scheduleComponent.schedule[i][j].length; ++j) {
              const currentCourseEdition = this.scheduleComponent.schedule[i][j][k];
              if (currentCourseEdition.CourseId == courseEdition.CourseId
                && currentCourseEdition.CourseEditionId == courseEdition.CourseEditionId) {
                  currentCourseEdition.ScheduleAmount -= weeks.length;
              }
            }
          }
        }
      }
      catch (error:any) {
        if (error.Message != undefined) {
          this.snackBar.open(error.Message, "OK");
        }
      }
    }
    
    if (this.data.currentSelectedCourseEdition != null) {
      this.data.currentSelectedCourseEdition.CourseEdition.IsCurrentlyActive = false;
    }
    this.data.currentSelectedCourseEdition = null;

    if (!isScheduleSource) {
      try {
        const result = await this.signalrService.UnlockCourseEdition(
          courseEdition.CourseId, courseEdition.CourseEditionId
        ).toPromise();
        
        if (result.StatusCode >= 400) {
          throw result;
        }
        
        courseEdition.Locked = false;
      } catch (error) {
        
      }
    } else {
      try {
        const result = await this.scheduleDesignerApiService.IsCourseEditionLocked(
          courseEdition.CourseId, courseEdition.CourseEditionId
        ).toPromise();

        courseEdition.Locked = result;
      }
      catch (error) {

      }
    }
    this.data.currentDragEvent = null;
    this.data.isCurrentMoveValid = null;
    event.item.data.IsCurrentlyActive = false;
  }

  OnMyCoursesEnter(drag: CdkDragEnter<CourseEdition[]>): void {
    this.data.isCurrentMoveValid = null;
    this.data.currentDropContainerIndexes = [-1, -1];
  }

  async OnMyCoursesStart(event: CdkDragStart<CourseEdition>): Promise<void> {
    this.data.isCurrentDragReleased = false;
    this.data.areSlotsValiditySet = false;
    this.data.currentDragEvent = event;
    event.source.data.IsCurrentlyActive = true;
    
    const courseEdition = event.source.data;
    const dropContainer = event.source.dropContainer;

    if (this.data.currentSelectedCourseEdition != null) {
      this.data.currentSelectedCourseEdition.CourseEdition.IsCurrentlyActive = false;
    }
    this.data.currentSelectedCourseEdition = new SelectedCourseEdition(courseEdition, -1, -1);
    this.data.currentSelectedCourseEdition.CanShowScheduledChanges = courseEdition.ScheduledMoves.length > 0;

    try {
      if (!this.data.isCurrentDragReleased) {
        const result = await this.signalrService.LockCourseEdition(
          courseEdition.CourseId, courseEdition.CourseEditionId
        ).toPromise();
        
        if (result.StatusCode >= 400) {
          throw result;
        }
        
        courseEdition.Locked = true;
      } else {
        return;
      }
    } catch (error:any) {
      if (!this.data.isCurrentDragReleased) {
        dropContainer._dropListRef.enter(
          event.source._dragRef,
          0,0
        );
        document.dispatchEvent(new Event('mouseup'));
        this.data.isCurrentDragReleased = true;
      } else {
        if (this.data.currentRoomSelectionDialog != null) {
          this.data.currentRoomSelectionDialog.close(RoomSelectionDialogResult.CANCELED);
        }
      }
      courseEdition.Locked = true;
      if (error.Message != undefined) {
        this.snackBar.open(error.Message, "OK");
      }

      this.data.currentDragEvent = null;
      if (this.data.currentSelectedCourseEdition != null) {
        this.data.currentSelectedCourseEdition.CourseEdition.IsCurrentlyActive = false;
      }
      this.data.currentSelectedCourseEdition = null;
      this.data.isCurrentMoveValid = null;
      event.source.data.IsCurrentlyActive = false;
      return;
    }

    let busySlots = await this.scheduleDesignerApiService.GetBusyPeriods(
      courseEdition.CourseId, 
      courseEdition.CourseEditionId,
      this.tabWeeks[this.currentTabIndex]
    ).toPromise();
    let connectedTo = ['my-courses'];
    
    const numberOfSlots = this.settings.Periods.length - 1;
    let busySlotIndex = 0;
    let scheduleSlots = this.scheduleComponent.scheduleSlots.toArray();
    
    for (let i = 0; i < this.scheduleComponent.scheduleSlots.length; ++i) {
      let element = scheduleSlots[i].element as ElementRef<HTMLElement>;
      if (i != (busySlots[busySlotIndex]?.Day - 1) * numberOfSlots + (busySlots[busySlotIndex]?.PeriodIndex - 1)) {
        this.data.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = true;
        connectedTo.push(element.nativeElement.id);
      } else {
        this.data.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = false;
        ++busySlotIndex;
      }
    }
    this.data.areSlotsValiditySet = true;
    dropContainer.connectedTo = connectedTo;
    
    if (!this.data.isCurrentDragReleased) {
      dropContainer._dropListRef.enter(
        event.source._dragRef, 0, 0
      );
    }
    
    if (this.data.isCurrentDragReleased) {
      try {
        const result = await this.signalrService.UnlockCourseEdition(
          courseEdition.CourseId, courseEdition.CourseEditionId
        ).toPromise();
        
        if (result.StatusCode >= 400) {
          throw result;
        }
        
        courseEdition.Locked = false;
      } catch (error) {
        
      }
      this.data.currentDragEvent = null;
      if (this.data.currentSelectedCourseEdition != null) {
        this.data.currentSelectedCourseEdition.CourseEdition.IsCurrentlyActive = false;
      }
      this.data.currentSelectedCourseEdition = null;
      for (let i = 0; i < this.scheduleComponent.scheduleSlots.length; ++i) {
        this.data.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = false;
      }
    }
  }

  async OnScheduleDrop(event:CdkDragDrop<CourseEdition[], CourseEdition[], CourseEdition>): Promise<void> {
    this.data.isCurrentDragReleased = true;

    if (this.data.isCurrentDragCanceled) {
      this.data.currentDragEvent = null;
      if (this.data.currentSelectedCourseEdition != null) {
        this.data.currentSelectedCourseEdition.CourseEdition.IsCurrentlyActive = false;
      }
      this.data.currentSelectedCourseEdition = null;
      this.data.isCurrentMoveValid = null;
      event.item.data.IsCurrentlyActive = false;
      return;
    }

    const courseEdition = event.item.data;
    const previousContainer = event.previousContainer;
    const currentContainer = event.container;
    const isScheduleSource = previousContainer.id !== 'my-courses';
    const previousIndexes = (isScheduleSource) ? this.getIndexes(previousContainer.id) : [-1,-1];
    const currentIndexes = this.getIndexes(currentContainer.id);
    const weeks = this.tabWeeks[this.currentTabIndex];
    
    if (previousContainer === currentContainer || !this.data.areSlotsValiditySet) {
      try {
        const result = await this.signalrService.UnlockSchedulePositions(
          courseEdition.Room!.RoomId, previousIndexes[1] + 1,
          previousIndexes[0] + 1, weeks
        ).toPromise();
        
        if (result.StatusCode >= 400) {
          throw result;
        }
        
        courseEdition.Locked = false;
      } catch (error) {

      }
      this.data.currentDragEvent = null;
      if (this.data.currentSelectedCourseEdition != null) {
        this.data.currentSelectedCourseEdition.CourseEdition.IsCurrentlyActive = false;
      }
      this.data.currentSelectedCourseEdition = null;
      this.data.isCurrentMoveValid = null;
      event.item.data.IsCurrentlyActive = false;
      return;
    }
    
    const dialogData = new RoomSelectionDialogData(
      courseEdition,
      previousIndexes,
      currentIndexes,
      weeks,
      this.settings.DayLabels,
      this.settings.TimeLabels,
      this.roomTypes,
      this.data.isCurrentMoveValid!,
      isScheduleSource,
      this.account.UserId
    );

    this.data.currentRoomSelectionDialog = this.dialogService.open(RoomSelectionComponent, {
      disableClose: true,
      data: dialogData
    });
    const dialogResult:RoomSelectionDialogResult = await this.data.currentRoomSelectionDialog.afterClosed().toPromise();
    this.data.currentRoomSelectionDialog = null;
    this.data.isCurrentMoveValid = null;
    this.data.currentSelectedCourseEdition = null;

    switch (dialogResult.Status) {
      case RoomSelectionDialogStatus.ACCEPTED: {
        courseEdition.Room = dialogResult.Room;
        courseEdition.Weeks = dialogResult.Weeks;
        courseEdition.ScheduledMoves = [];

        if (!isScheduleSource) {
          
          this.myCoursesComponent.myCourses.forEach((element) => {
            if (element.CourseId == courseEdition.CourseId 
              && element.CourseEditionId == courseEdition.CourseEditionId) {
                element.ScheduleAmount += dialogResult.Weeks.length;
            }
          });

          for (let i = 0; i < this.scheduleComponent.schedule.length; ++i) {
            for (let j = 0; j < this.scheduleComponent.schedule[i].length; ++j) {
              for (let k = 0; k < this.scheduleComponent.schedule[i][j].length; ++j) {
                const currentCourseEdition = this.scheduleComponent.schedule[i][j][k];
                if (currentCourseEdition.CourseId == courseEdition.CourseId
                  && currentCourseEdition.CourseEditionId == courseEdition.CourseEditionId) {
                    currentCourseEdition.ScheduleAmount += dialogResult.Weeks.length;
                }
              }
            }
          }
        }

        const previousIndex = (isScheduleSource) ? event.previousIndex 
          : this.myCoursesComponent.myCourses.findIndex(value => value.IsCurrentlyActive);

        transferArrayItem(
          event.previousContainer.data,
          event.container.data,
          previousIndex,
          event.currentIndex
        );
      } break;
      case RoomSelectionDialogStatus.SCHEDULED: {

      } break;
      case RoomSelectionDialogStatus.CANCELED: {
        if (dialogResult.Message != undefined) {
          this.snackBar.open(dialogResult.Message, "OK");
        }
      } break;
      case RoomSelectionDialogStatus.FAILED: {
        if (dialogResult.Message != undefined) {
          this.snackBar.open(dialogResult.Message, "OK");
        }
      } break;
    }

    if (!isScheduleSource) {
      try {
        const result = await this.signalrService.UnlockCourseEdition(
          courseEdition.CourseId, courseEdition.CourseEditionId
        ).toPromise();
        
        if (result.StatusCode >= 400) {
          throw result;
        }
        
        courseEdition.Locked = false;
      } catch (error) {
  
      }
    } else if (dialogResult.Status == RoomSelectionDialogStatus.ACCEPTED) {
      courseEdition.Locked = false;
    } else {
      try {
        const result = await this.signalrService.UnlockSchedulePositions(
          courseEdition.Room!.RoomId, previousIndexes[1] + 1,
          previousIndexes[0] + 1, weeks
        ).toPromise();
        
        if (result.StatusCode >= 400) {
          throw result;
        }
        
        courseEdition.Locked = false;
      } catch (error) {
  
      }
    }

    event.item.data.IsCurrentlyActive = false;
  }

  OnScheduleEnter(drag: CdkDragEnter<CourseEdition[]>): void {
    const indexes = this.getIndexes(drag.container.id);
    this.data.isCurrentMoveValid = this.data.scheduleSlotsValidity[indexes[0]][indexes[1]];
    this.data.currentDropContainerIndexes = this.getIndexes(drag.container.id);
  }

  async OnScheduleStart(event: CdkDragStart<CourseEdition>): Promise<void> {
    this.data.isCurrentDragReleased = false;
    this.data.areSlotsValiditySet = false;
    this.data.currentDragEvent = event;
    event.source.data.IsCurrentlyActive = true;
    
    const courseEdition = event.source.data;
    const dropContainer = event.source.dropContainer;
    const indexes = this.getIndexes(dropContainer.id);

    if (this.data.currentSelectedCourseEdition != null) {
      this.data.currentSelectedCourseEdition.CourseEdition.IsCurrentlyActive = false;
    }
    this.data.currentSelectedCourseEdition = new SelectedCourseEdition(courseEdition, indexes[1], indexes[0]);
    this.data.currentSelectedCourseEdition.CanShowScheduledChanges = courseEdition.ScheduledMoves.length > 0;

    try {
      if (!this.data.isCurrentDragReleased) {
        const result = await this.signalrService.LockSchedulePositions(
          courseEdition.Room!.RoomId, indexes[1] + 1,
          indexes[0] + 1, this.tabWeeks[this.currentTabIndex]
        ).toPromise();
        
        if (result.StatusCode >= 400) {
          throw result;
        }

        courseEdition.Locked = true;
      } else {
        return;
      }
    } catch (error:any) {
      this.data.isCurrentDragCanceled = true;
      if (!this.data.isCurrentDragReleased) {
        dropContainer._dropListRef.enter(
          event.source._dragRef,
          0,0
        );
        document.dispatchEvent(new Event('mouseup'));
      } else {
        if (this.data.currentRoomSelectionDialog != null) {
          this.data.currentRoomSelectionDialog.close(RoomSelectionDialogResult.CANCELED);
        }
      }
      courseEdition.Locked = true;
      if (error.Message != undefined) {
        this.snackBar.open(error.Message, "OK");
      }
      return;
    }

    let busySlots = await this.scheduleDesignerApiService.GetBusyPeriods(
      courseEdition.CourseId, 
      courseEdition.CourseEditionId,
      this.tabWeeks[this.currentTabIndex]
    ).toPromise();
    let connectedTo = ['my-courses'];
    
    const numberOfSlots = this.settings.Periods.length - 1;
    let busySlotIndex = 0;
    let scheduleSlots = this.scheduleComponent.scheduleSlots.toArray();
    
    for (let i = 0; i < this.scheduleComponent.scheduleSlots.length; ++i) {
      let element = scheduleSlots[i].element as ElementRef<HTMLElement>;
      connectedTo.push(element.nativeElement.id);
      if (i != (busySlots[busySlotIndex]?.Day - 1) * numberOfSlots + (busySlots[busySlotIndex]?.PeriodIndex - 1)) {
        this.data.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = true;
      } else {
        this.data.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = false;
        ++busySlotIndex;
      }
    }
    this.data.scheduleSlotsValidity[indexes[0]][indexes[1]] = true;
    this.data.areSlotsValiditySet = true;
    dropContainer.connectedTo = connectedTo;
    
    if (!this.data.isCurrentDragReleased) {
      dropContainer._dropListRef.enter(
        event.source._dragRef, 0, 0
      );
    }
    
    if (this.data.isCurrentDragReleased) {
      try {
        const result = await this.signalrService.UnlockSchedulePositions(
          courseEdition.Room!.RoomId, indexes[1] + 1,
          indexes[0] + 1, this.tabWeeks[this.currentTabIndex]
        ).toPromise();
        
        if (result.StatusCode >= 400) {
          throw result;
        }

        courseEdition.Locked = false;
      } catch (error) {
        
      }
      this.data.currentDragEvent = null;
      if (this.data.currentSelectedCourseEdition != null) {
        this.data.currentSelectedCourseEdition.CourseEdition.IsCurrentlyActive = false;
      }
      this.data.currentSelectedCourseEdition = null;
      for (let i = 0; i < this.scheduleComponent.scheduleSlots.length; ++i) {
        this.data.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = false;
      }
    }
  }

  async OnRelease(event: CdkDragRelease<CourseEdition>): Promise<void> {
    this.data.isCurrentDragReleased = true;

    event.source.dropContainer.connectedTo = [];

    this.data.currentDragEvent = null;
    const numberOfSlots = this.settings.Periods.length - 1;
    for (let i = 0; i < this.scheduleComponent.scheduleSlots.length; ++i) {
      this.data.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = false;
    }
  }

  OnSelect(event: {courseEdition: CourseEdition, isDisabled: boolean, day: number, periodIndex: number}): void {
    if (this.data.currentSelectedCourseEdition != null) {
      if (this.data.currentSelectedCourseEdition.IsMoving) {
        return;
      }
      this.data.currentSelectedCourseEdition.CourseEdition.IsCurrentlyActive = false;
      if (event.courseEdition == this.data.currentSelectedCourseEdition.CourseEdition) {
        this.data.currentSelectedCourseEdition = null;
        return;
      }
    }
    this.data.currentSelectedCourseEdition = new SelectedCourseEdition(event.courseEdition, event.periodIndex, event.day);
    this.data.currentSelectedCourseEdition.CanChangeRoom = !event.isDisabled;
    this.data.currentSelectedCourseEdition.CanMakeMove = !event.isDisabled;
    this.data.currentSelectedCourseEdition.CanShowScheduledChanges = event.courseEdition.ScheduledMoves.length > 0;
    event.courseEdition.IsCurrentlyActive = true;
  }

  async OnRoomSelect(event: {day: number, periodIndex: number}): Promise<void> {
    if (!this.data.currentSelectedCourseEdition?.IsMoving || !this.data.areSlotsValiditySet) {
      return;
    }
    
    if (this.scheduleComponent.schedule[event.day][event.periodIndex].length > 0) {
      return;
    }

    this.data.isCurrentMoveValid = this.data.scheduleSlotsValidity[event.day][event.periodIndex];
    
    const selectedCourseEdition = this.data.currentSelectedCourseEdition;
    const dialogData = new RoomSelectionDialogData(
      selectedCourseEdition.CourseEdition,
      [selectedCourseEdition.Day,selectedCourseEdition.PeriodIndex],
      [event.day,event.periodIndex],
      this.tabWeeks[this.currentTabIndex],
      this.settings.DayLabels,
      this.settings.TimeLabels,
      this.roomTypes,
      this.data.isCurrentMoveValid!,
      true,
      this.account.UserId
    );

    this.data.currentRoomSelectionDialog = this.dialogService.open(RoomSelectionComponent, {
      disableClose: true,
      data: dialogData
    });
    const dialogResult:RoomSelectionDialogResult = await this.data.currentRoomSelectionDialog.afterClosed().toPromise();
    this.data.currentRoomSelectionDialog = null;

    switch (dialogResult.Status) {
      case RoomSelectionDialogStatus.ACCEPTED: {
        //remove old
        if (this.tabWeeks[this.currentTabIndex].sort((a,b) => a - b).join(',') 
          === this.data.currentSelectedCourseEdition.CourseEdition.Weeks?.sort((a,b) => a - b).join(',')) {
            
            const selectedDay = this.data.currentSelectedCourseEdition.Day;
            const selectedPeriodIndex = this.data.currentSelectedCourseEdition.PeriodIndex;
            const selectedCourseEdition = this.data.currentSelectedCourseEdition.CourseEdition;
            
            const existingSrcCourseEditions = this.scheduleComponent.schedule[selectedDay][selectedPeriodIndex].filter((courseEdition) => 
            selectedCourseEdition.CourseId == courseEdition.CourseId 
                && selectedCourseEdition.CourseEditionId == courseEdition.CourseEditionId
                && selectedCourseEdition.Room!.RoomId == courseEdition.Room!.RoomId
            );
            
            if (existingSrcCourseEditions.length > 0) {
              existingSrcCourseEditions[0].Weeks = existingSrcCourseEditions[0].Weeks
                ?.filter(week => !selectedCourseEdition.Weeks?.includes(week)) ?? [];
              
              if (existingSrcCourseEditions[0].Weeks.length == 0) {
                this.scheduleComponent.schedule[selectedDay][selectedPeriodIndex] 
                  = this.scheduleComponent.schedule[selectedDay][selectedPeriodIndex].filter(courseEdition => courseEdition.Weeks != null 
                    && courseEdition.Weeks.length > 0);
              }
            }
        }
        
        this.data.currentSelectedCourseEdition.CourseEdition.Room = dialogResult.Room;
        this.data.currentSelectedCourseEdition.CourseEdition.Weeks = dialogResult.Weeks;
        this.data.currentSelectedCourseEdition.CourseEdition.ScheduledMoves = [];

        //add new
        this.scheduleComponent.schedule[event.day][event.periodIndex].push(this.data.currentSelectedCourseEdition.CourseEdition);
      } break;
      case RoomSelectionDialogStatus.SCHEDULED: {

      } break;
      case RoomSelectionDialogStatus.CANCELED: {
        if (dialogResult.Message != undefined) {
          this.snackBar.open(dialogResult.Message, "OK");
        }
      } break;
      case RoomSelectionDialogStatus.FAILED: {
        if (dialogResult.Message != undefined) {
          this.snackBar.open(dialogResult.Message, "OK");
        }
      } break;
    }
    if (dialogResult.Status != RoomSelectionDialogStatus.ACCEPTED) {
      try {
        const result = await this.signalrService.UnlockSchedulePositions(
          selectedCourseEdition.CourseEdition.Room?.RoomId!, selectedCourseEdition.PeriodIndex + 1,
          selectedCourseEdition.Day + 1, selectedCourseEdition.CourseEdition.Weeks!
        ).toPromise();
        
        if (result.StatusCode >= 400) {
          throw result;
        }
        
        this.data.currentSelectedCourseEdition.CourseEdition.Locked = false;
      } catch (error) {
  
      }
    } else {
      this.data.currentSelectedCourseEdition.CourseEdition.Locked = false;
    }
    
    this.data.isCurrentMoveValid = null;
    this.data.currentSelectedCourseEdition.IsMoving = false;
    const numberOfSlots = this.settings.Periods.length - 1;
    for (let i = 0; i < this.scheduleComponent.scheduleSlots.length; ++i) {
      this.data.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = false;
    }
    this.data.areSlotsValiditySet = false;
  }

  async AddRoom(): Promise<void> {
    if (this.data.currentSelectedCourseEdition == null) {
      return;
    }
    const selectedCourseEdition = this.data.currentSelectedCourseEdition;

    const dialogData = new AddRoomSelectionDialogData(
      selectedCourseEdition.CourseEdition,
      this.roomTypes,
      this.account.UserId
    );

    this.data.currentAddRoomSelectionDialog = this.dialogService.open(AddRoomSelectionComponent, {
      disableClose: true,
      data: dialogData
    });
    const dialogResult:AddRoomSelectionDialogResult = await this.data.currentAddRoomSelectionDialog.afterClosed().toPromise();
    this.data.currentAddRoomSelectionDialog = null;

    if (dialogResult.Message != undefined) {
      this.snackBar.open(dialogResult.Message, "OK");
    }
  }

  async ChangeRoom(): Promise<void> {
    if (this.data.currentSelectedCourseEdition == null) {
      return;
    }
    const selectedCourseEdition = this.data.currentSelectedCourseEdition;

    try {
      const result = await this.signalrService.LockSchedulePositions(
        selectedCourseEdition.CourseEdition.Room!.RoomId, selectedCourseEdition.PeriodIndex + 1,
        selectedCourseEdition.Day + 1, selectedCourseEdition.CourseEdition.Weeks!
      ).toPromise();
      
      if (result.StatusCode >= 400) {
        throw result;
      }
      
      this.data.currentSelectedCourseEdition.CourseEdition.Locked = true;
    } catch (error:any) {
      if (error.Message != undefined) {
        this.snackBar.open(error.Message, "OK");
      }
      return;
    }

    const dialogData = new RoomSelectionDialogData(
      selectedCourseEdition.CourseEdition,
      [selectedCourseEdition.Day,selectedCourseEdition.PeriodIndex],
      [selectedCourseEdition.Day,selectedCourseEdition.PeriodIndex],
      selectedCourseEdition.CourseEdition.Weeks!,
      this.settings.DayLabels,
      this.settings.TimeLabels,
      this.roomTypes,
      true,
      true,
      this.account.UserId
    );

    this.data.currentRoomSelectionDialog = this.dialogService.open(RoomSelectionComponent, {
      disableClose: true,
      data: dialogData
    });
    const dialogResult:RoomSelectionDialogResult = await this.data.currentRoomSelectionDialog.afterClosed().toPromise();
    this.data.currentRoomSelectionDialog = null;

    switch (dialogResult.Status) {
      case RoomSelectionDialogStatus.ACCEPTED: {
        this.data.currentSelectedCourseEdition.CourseEdition.Room = dialogResult.Room;
        this.data.currentSelectedCourseEdition.CourseEdition.Weeks = dialogResult.Weeks;
      } break;
      case RoomSelectionDialogStatus.SCHEDULED: {

      } break;
      case RoomSelectionDialogStatus.CANCELED: {
        if (dialogResult.Message != undefined) {
          this.snackBar.open(dialogResult.Message, "OK");
        }
      } break;
      case RoomSelectionDialogStatus.FAILED: {
        if (dialogResult.Message != undefined) {
          this.snackBar.open(dialogResult.Message, "OK");
        }
      } break;
    }
    if (dialogResult.Status != RoomSelectionDialogStatus.ACCEPTED) {
      try {
        const result = await this.signalrService.UnlockSchedulePositions(
          selectedCourseEdition.CourseEdition.Room?.RoomId!, selectedCourseEdition.PeriodIndex + 1,
          selectedCourseEdition.Day + 1, selectedCourseEdition.CourseEdition.Weeks!
        ).toPromise();
        
        if (result.StatusCode >= 400) {
          throw result;
        }
        
        this.data.currentSelectedCourseEdition.CourseEdition.Locked = false;
      } catch (error) {
  
      }
    } else {
      this.data.currentSelectedCourseEdition.CourseEdition.Locked = false;
    }
  }
  
  async ShowScheduledChanges(): Promise<void> {
    if (this.data.currentSelectedCourseEdition == null) {
      return;
    }
    const selectedCourseEdition = this.data.currentSelectedCourseEdition;

    const dialogData = new ScheduledChangesDialogData(
      selectedCourseEdition.CourseEdition,
      [selectedCourseEdition.Day,selectedCourseEdition.PeriodIndex],
      this.settings.DayLabels,
      this.settings.TimeLabels,
      this.roomTypes,
      this.settings
    );

    this.data.currentScheduledChangesDialog = this.dialogService.open(ScheduledChangesViewComponent, {
      disableClose: true,
      data: dialogData
    });
    const dialogResult: ScheduledChangesDialogResult = await this.data.currentScheduledChangesDialog.afterClosed().toPromise();
    this.data.currentScheduledChangesDialog = null;
    if (this.data.currentSelectedCourseEdition != null) {
      this.data.currentSelectedCourseEdition.CanShowScheduledChanges 
        = this.data.currentSelectedCourseEdition.CourseEdition.ScheduledMoves.length > 0;
    }

    if (dialogResult.Message != undefined) {
      this.snackBar.open(dialogResult.Message, "OK");
    }
  }

  async Move(): Promise<void> {
    if (this.data.currentSelectedCourseEdition == null) {
      return;
    }

    if (this.data.currentSelectedCourseEdition.IsMoving) {
      try {
        const selectedCourseEdition = this.data.currentSelectedCourseEdition;
        const result = await this.signalrService.UnlockSchedulePositions(
          selectedCourseEdition.CourseEdition.Room?.RoomId!, selectedCourseEdition.PeriodIndex + 1,
          selectedCourseEdition.Day + 1, selectedCourseEdition.CourseEdition.Weeks!
        ).toPromise();
        
        if (result.StatusCode >= 400) {
          throw result;
        }
        
        this.data.currentSelectedCourseEdition.CourseEdition.Locked = false;
      } catch (error) {
  
      }
      
      this.data.currentSelectedCourseEdition.IsMoving = false;
      
      const numberOfSlots = this.settings.Periods.length - 1;
      for (let i = 0; i < this.scheduleComponent.scheduleSlots.length; ++i) {
        this.data.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = false;
      }
      this.data.areSlotsValiditySet = false;
      return;
    }

    const selectedCourseEdition = this.data.currentSelectedCourseEdition;
    try {
      const result = await this.signalrService.LockSchedulePositions(
        selectedCourseEdition.CourseEdition.Room!.RoomId, selectedCourseEdition.PeriodIndex + 1,
        selectedCourseEdition.Day + 1, selectedCourseEdition.CourseEdition.Weeks!
      ).toPromise();
      
      if (result.StatusCode >= 400) {
        throw result;
      }
      
      this.data.currentSelectedCourseEdition.CourseEdition.Locked = true;
    } catch (error:any) {
      if (error.Message != undefined) {
        this.snackBar.open(error.Message, "OK");
      }
      return;
    }
    
    let busySlots = await this.scheduleDesignerApiService.GetBusyPeriods(
      selectedCourseEdition.CourseEdition.CourseId, 
      selectedCourseEdition.CourseEdition.CourseEditionId,
      this.tabWeeks[this.currentTabIndex]
    ).toPromise();
    
    const numberOfSlots = this.settings.Periods.length - 1;
    let busySlotIndex = 0;
    
    for (let i = 0; i < this.scheduleComponent.scheduleSlots.length; ++i) {
      if (i != (busySlots[busySlotIndex]?.Day - 1) * numberOfSlots + (busySlots[busySlotIndex]?.PeriodIndex - 1)) {
        this.data.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = true;
      } else {
        this.data.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = false;
        ++busySlotIndex;
      }
    }

    this.data.areSlotsValiditySet = true;

    this.data.currentSelectedCourseEdition.IsMoving = true;
  }

  CancelSelection(): void {
    if (this.data.currentSelectedCourseEdition != null) {
      this.data.currentSelectedCourseEdition.CourseEdition.IsCurrentlyActive = false;
    }
    this.data.currentSelectedCourseEdition = null;
  }

  OnMouseEnter(event: {day: number, periodIndex: number}): void {
    if (this.data.currentSelectedCourseEdition?.IsMoving) {
      this.data.currentDropContainerIndexes = [event.day, event.periodIndex];
      if (this.scheduleComponent.schedule[event.day][event.periodIndex].length > 0) {
        this.data.isCurrentMoveValid = null;
      } else {
        this.data.isCurrentMoveValid = this.data.scheduleSlotsValidity[event.day][event.periodIndex];
      }
    }
  }

  OnMouseLeave(): void {
    if (this.data.currentSelectedCourseEdition?.IsMoving) {
      this.data.isCurrentMoveValid = null;
      this.data.currentDropContainerIndexes = [-1,-1];
    }
  }
}
