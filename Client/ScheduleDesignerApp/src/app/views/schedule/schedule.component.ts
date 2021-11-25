import { Component, ElementRef, OnInit, QueryList, ViewChild, ViewChildren } from '@angular/core';
import { CdkDrag, CdkDragDrop, CdkDragEnter, CdkDragRelease, CdkDragStart, CdkDropList, DropListRef, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { ScheduleDesignerApiService } from 'src/app/services/ScheduleDesignerApiService/schedule-designer-api.service';
import { CourseEdition } from 'src/app/others/CourseEdition';
import { CourseType, RoomType } from 'src/app/others/Types';
import { Group } from 'src/app/others/Group';
import { SignalrService } from 'src/app/services/SignalrService/signalr.service';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { CourseComponent } from 'src/app/components/course/course.component';
import { Settings } from 'src/app/others/Settings';
import { forkJoin } from 'rxjs';
import { MatSnackBar } from '@angular/material/snack-bar';
import { UsosApiService } from 'src/app/services/UsosApiService/usos-api.service';
import { Router } from '@angular/router';
import { skip } from 'rxjs/operators';
import { RoomSelectionComponent } from 'src/app/components/room-selection/room-selection.component';
import { Room } from 'src/app/others/Room';
import { RoomSelectionDialogData, RoomSelectionDialogResult, RoomSelectionDialogStatus } from 'src/app/others/RoomSelectionDialog';

@Component({
  selector: 'app-schedule',
  templateUrl: './schedule.component.html',
  styleUrls: ['./schedule.component.css']
})
export class ScheduleComponent implements OnInit {

  @ViewChild('myCoursesSlot') myCoursesSlot : DropListRef<CourseEdition>
  @ViewChildren('scheduleSlots') scheduleSlots : QueryList<DropListRef<CourseEdition>>;
  @ViewChildren(CourseComponent) courses : QueryList<CourseComponent>;
  currentDragEvent : CdkDragStart<CourseEdition> | null;
  currentOpenedDialog : MatDialogRef<RoomSelectionComponent, any> | null;
  isReleased:boolean = false;
  isCanceled:boolean = false;

  loading:boolean = true;
  tabLoading:boolean = true;
  connectionStatus:boolean = false;
  
  settings:Settings;
  frequencies:number[];
  weeks:number[][];
  
  currentTabIndex:number = 0;
  
  tabLabels:string[] = ['Semester', 'Even Weeks', 'Odd Weeks'];
  scheduleTimeLabels:string[] = [];
  scheduleDayLabels:string[] = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday'];

  courseTypes:Map<number, CourseType>;
  roomTypes:Map<number, RoomType>;

  myCourses:CourseEdition[];
  schedule:CourseEdition[][][];
  
  scheduleSlotsValidity:boolean[][];
  isMoveValid:boolean|null = null;

  constructor(
    private scheduleDesignerApiService:ScheduleDesignerApiService,
    private usosApiService:UsosApiService,
    private signalrService:SignalrService,
    private router:Router,
    private snackBar:MatSnackBar,
    private dialog:MatDialog
  ) 
  { }

  ngOnInit(): void {
    let isConnectedSubscription = this.signalrService.isConnected.pipe(skip(1)).subscribe((status) => {
      this.connectionStatus = status;
      if (!status && !this.signalrService.connectionIntentionallyStopped) {
        this.snackBar.open("Connection with server has been lost. Please refresh the page to possibly reconnect.", "OK");
      }
    });

    this.signalrService.lastLockedCourseEdition.pipe(skip(1)).subscribe(({courseId, courseEditionId}) => {
      if (!this.myCourses) {
        return;
      }

      this.myCourses.forEach((value) => {
        if (value.CourseId == courseId && value.CourseEditionId == courseEditionId) {
          value.Locked = true;
        }
      });
    });

    this.signalrService.lastUnlockedCourseEdition.pipe(skip(1)).subscribe(({courseId, courseEditionId}) => {
      if (!this.myCourses) {
        return;
      }

      this.myCourses.forEach((value) => {
        if (value.CourseId == courseId && value.CourseEditionId == courseEditionId) {
          value.Locked = false;
        }
      });
    });

    forkJoin([
      this.signalrService.InitConnection(),
      this.scheduleDesignerApiService.GetSettings(),
      this.scheduleDesignerApiService.GetPeriods(),
      this.scheduleDesignerApiService.GetCourseTypes(),
      this.scheduleDesignerApiService.GetRoomTypes()
    ]).subscribe(([,settings,periods,courseTypes,roomTypes]) => {
      this.connectionStatus = true;
      
      this.settings = settings;
      this.settings.periods = periods;
      this.courseTypes = courseTypes;
      this.roomTypes = roomTypes;

      this.setLabels();
      this.setFrequenciesAndWeeks();
      this.initializeValues();

      this.getMyCourseEditionsAndScheduleAsCoordinator(0);
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

  private getMyCourseEditionsAndScheduleAsCoordinator(index:number) {
    let roundUp = (index != 1);

    forkJoin([
      this.scheduleDesignerApiService.GetMyCourseEditions(this.frequencies[index], this.courseTypes, this.settings, roundUp),
      this.scheduleDesignerApiService.GetScheduleAsCoordinator(this.weeks[index], this.courseTypes, this.settings)
    ]).subscribe(([myCourses, mySchedule]) => {
      this.myCourses = myCourses;
      this.schedule = mySchedule;

      let allGroups = new Array<Group>();
      let allRooms = new Array<Room>();
      
      for (let i = 0; i < this.myCourses.length; ++i) {
        for (let j = 0; j < this.myCourses[i].Groups.length; ++j) {
          allGroups.push(this.myCourses[i].Groups[j]);
        }
      }
      for (let i = 0; i < this.schedule.length; ++i) {
        for (let j = 0; j < this.schedule[i].length; ++j) {
          for (let k = 0; k < this.schedule[i][j].length; ++k) {
            for (let l = 0; l < this.schedule[i][j][k].Groups.length; ++l) {
              allGroups.push(this.schedule[i][j][k].Groups[l]);
            }
            allRooms.push(this.schedule[i][j][k].Room!);
          }
        }
      }

      forkJoin([
        this.scheduleDesignerApiService.GetGroupsFullNames(allGroups.map((e) => e.GroupId)),
        this.scheduleDesignerApiService.GetRoomsNames(allRooms.map((e) => e.RoomId))
      ]).subscribe(([groupsFullNames, roomsNames]) => {
        for (let i = 0; i < groupsFullNames.length; ++i) {
          allGroups[i].FullName = groupsFullNames[i];
        }

        for (let i = 0; i < roomsNames.length; ++i) {
          allRooms[i].Name = roomsNames[i];
        }
        
        this.loading = false;
        this.tabLoading = false;
      });

      //TEST
        /*setTimeout(() => {
          console.log("TEST");
          console.log(this.myCoursesSlot);
          console.log(this.scheduleSlots);
          if (this.currentDragEvent != null) {
            const numberOfSlots = this.settings.periods.length - 1;
            this.currentDragEvent.source.dropContainer.connectedTo = ['1,1', '1,4', '1,3'];
            console.log(this.currentDragEvent.source.dropContainer.connectedTo);
            this.currentDragEvent.source.dropContainer._dropListRef.enter(
              this.currentDragEvent.source._dragRef, 0, 0
            );
          }
        }, 5000);*/
    });
  }

  private setLabels() {
    for (let i:number = 0; i < this.settings.TermDurationWeeks; ++i) {
      this.tabLabels.push('Week ' + (i + 1));
    }
  }

  private setFrequenciesAndWeeks() {
    this.frequencies = [this.settings.TermDurationWeeks, this.settings.TermDurationWeeks / 2, this.settings.TermDurationWeeks / 2];
    this.weeks = [[],[],[]];

    for (let i:number = 0; i < this.settings.TermDurationWeeks; ++i) {
      this.frequencies.push(1);
      const weekNumber = i + 1;
      this.weeks[0].push(weekNumber);
      this.weeks.push([weekNumber]);
      if (weekNumber % 2 == 0) {
        this.weeks[1].push(weekNumber);
      } else {
        this.weeks[2].push(weekNumber);
      }
    }
  }

  private initializeValues() {
    const periods = this.settings.periods;
    const numberOfSlots = this.settings.periods.length - 1;
    
    for (let i:number = 0; i < numberOfSlots; ++i) {
      this.scheduleTimeLabels.push(
        periods[i] + ' - ' + periods[i + 1]
      );
    }

    this.scheduleSlotsValidity = [];
    for (let j:number = 0; j < 5; ++j) {
      this.scheduleSlotsValidity.push([]);
      for (let i:number = 0; i < numberOfSlots; ++i) {
        this.scheduleSlotsValidity[j].push(false);
      }
    }
  }

  private resetSchedule() {
    const numberOfSlots = this.settings.periods.length - 1;

    this.schedule = [];
    for (let j:number = 0; j < 5; ++j) {
      this.schedule.push([]);
      for (let i:number = 0; i < numberOfSlots; ++i) {
        this.schedule[j].push([]);
      }
    }
  }

  private getIndexes(id:string):number[] {
    const indexes = id.split(',');
    return [
      Number.parseInt(indexes[0]),
      Number.parseInt(indexes[1])
    ];
  }

  GetMaxElementIndexOnDay(dayIndex:number):number {
    let dayScheduleLength:number[] = [];
    for (let i = 0; i < this.settings.periods.length - 1; ++i) {
      dayScheduleLength.push(this.schedule[dayIndex][i].length);
    }
    return Math.max(...dayScheduleLength) - 1;
  }

  OnTabChange(index:number) {
    this.currentTabIndex = index;
    this.tabLoading = true;

    this.resetSchedule();
    this.getMyCourseEditionsAndScheduleAsCoordinator(index);
  }

  async DropInMyCourses(event:CdkDragDrop<CourseEdition[], CourseEdition[], CourseEdition>) {
    if (this.isCanceled) {
      this.currentDragEvent = null;
      this.isMoveValid = null;
      return;
    }

    if (event.previousContainer === event.container) {
      moveItemInArray(
        event.container.data,
        event.previousIndex,
        event.currentIndex
      );
    } else {
      transferArrayItem(
        event.previousContainer.data,
        event.container.data,
        event.previousIndex,
        event.currentIndex
      );
      event.container.data[event.currentIndex].Room = null;
      event.container.data[event.currentIndex].Amount = event.container.data[event.currentIndex].Weeks?.length ?? 0;
      event.container.data[event.currentIndex].Weeks = null;
    }
    
    try {
      const result = await this.signalrService.UnlockCourseEdition(event.item.data.CourseId, event.item.data.CourseEditionId).toPromise();
      
      if (result.statusCode >= 400) {
        throw result;
      }
      event.item.data.Locked = false;
    } catch (error) {
      console.log(error);
    }
    this.currentDragEvent = null;
    this.isMoveValid = null;
  }

  async DropInSchedule(event:CdkDragDrop<CourseEdition[], CourseEdition[], CourseEdition>, dayIndex:number, slotIndex:number) {
    this.isReleased = true;

    const index = dayIndex * this.scheduleTimeLabels.length + slotIndex;

    if (this.isCanceled) {
      this.currentDragEvent = null;
      this.isMoveValid = null;
      return;
    }

    if (event.previousContainer === event.container) {
      try {
        const result = await this.signalrService.UnlockCourseEdition(event.item.data.CourseId, event.item.data.CourseEditionId).toPromise();
        if (result.statusCode >= 400) {
          throw result;
        }
        event.item.data.Locked = false;
      } catch (error) {

      }
      this.currentDragEvent = null;
      this.isMoveValid = null;
      return;
    }

    this.currentOpenedDialog = this.dialog.open(RoomSelectionComponent, {
      disableClose: true,
      data: new RoomSelectionDialogData(
        event.item.data,
        this.getIndexes(event.container.id),
        this.weeks[this.currentTabIndex],
        this.scheduleDayLabels,
        this.scheduleTimeLabels,
        this.roomTypes,
        this.isMoveValid!,
        event.previousContainer.id !== 'my-courses'
      )
    });
    const result:RoomSelectionDialogResult = await this.currentOpenedDialog.afterClosed().toPromise();
    this.currentOpenedDialog = null;

    switch (result.Status) {
      case RoomSelectionDialogStatus.ACCEPTED: {
        event.previousContainer.data[event.previousIndex].Room = result.Room;
        event.previousContainer.data[event.previousIndex].Weeks = this.weeks[this.currentTabIndex];

        transferArrayItem(
          event.previousContainer.data,
          event.container.data,
          event.previousIndex,
          event.currentIndex
        );
      } break;
      case RoomSelectionDialogStatus.SCHEDULED: {

      } break;
      case RoomSelectionDialogStatus.CANCELED: {

      } break;
      case RoomSelectionDialogStatus.FAILED: {

      } break;
    }

    try {
      const result = await this.signalrService.UnlockCourseEdition(event.item.data.CourseId, event.item.data.CourseEditionId).toPromise();
      if (result.statusCode >= 400) {
        throw result;
      }
      event.item.data.Locked = false;
    } catch (error) {

    }
    this.currentDragEvent = null;
    this.isMoveValid = null;
  }

  IsScheduleSlotDisabled(dayIndex:number, slotIndex:number) {
    return this.schedule[dayIndex][slotIndex].length > 1;
  }

  ScheduleSlotEnterPredicate(drag:CdkDrag<CourseEdition>, drop:CdkDropList<CourseEdition[]>) {
    return drop.data.length < 1;
  }

  OnScheduleSlotEnter(drag:CdkDragEnter<CourseEdition[]>) {
    const indexes = this.getIndexes(drag.container.id);
    this.isMoveValid = this.scheduleSlotsValidity[indexes[0]][indexes[1]];
  }

  OnMyCoursesEnter(drag:CdkDragEnter<CourseEdition[]>) {
    this.isMoveValid = null;
  }

  async OnStartDragging(event:CdkDragStart<CourseEdition>) {
    this.isReleased = false;
    this.isCanceled = false;
    this.currentDragEvent = event;
    
    try {
      if (!this.isReleased) {
        const result = await this.signalrService.LockCourseEdition(event.source.data.CourseId, event.source.data.CourseEditionId).toPromise();
        if (result.statusCode >= 400) {
          throw result;
        }
        event.source.data.Locked = true;
      } else {
        return;
      }
    } catch (error) {
      this.isCanceled = true;
      if (!this.isReleased) {
        event.source.dropContainer._dropListRef.enter(
          event.source._dragRef,
          0,0
        );
        document.dispatchEvent(new Event('mouseup'));
      } else {
        if (this.currentOpenedDialog != null) {
          this.currentOpenedDialog.close(false);
        }
      }
      event.source.data.Locked = true;
      this.snackBar.open("Someone has locked this course before you.", "OK");
      return;
    }

    let busySlots = await this.scheduleDesignerApiService.GetBusyPeriods(
      event.source.data.CourseId, 
      event.source.data.CourseEditionId,
      this.weeks[this.currentTabIndex]
    ).toPromise();
    let connectedTo = ['my-courses'];
    
    const numberOfSlots = this.settings.periods.length - 1;
    let busySlotIndex = 0;
    let scheduleSlots = this.scheduleSlots.toArray();
    
    if (event.source.dropContainer.id != 'my-courses') {
      for (let i = 0; i < this.scheduleSlots.length; ++i) {
        let element = scheduleSlots[i].element as ElementRef<HTMLElement>;
        connectedTo.push(element.nativeElement.id);
        if (i != (busySlots[busySlotIndex]?.Day - 1) * numberOfSlots + (busySlots[busySlotIndex]?.PeriodIndex - 1)) {
          this.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = true;
        } else {
          this.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = false;
          ++busySlotIndex;
        }
      }
      const indexes = this.getIndexes(event.source.dropContainer.id);
      this.scheduleSlotsValidity[indexes[0]][indexes[1]] = true;
    } else {
      for (let i = 0; i < this.scheduleSlots.length; ++i) {
        let element = scheduleSlots[i].element as ElementRef<HTMLElement>;
        if (i != (busySlots[busySlotIndex]?.Day - 1) * numberOfSlots + (busySlots[busySlotIndex]?.PeriodIndex - 1)) {
          this.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = true;
          connectedTo.push(element.nativeElement.id);
        } else {
          this.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = false;
          ++busySlotIndex;
        }
      }
    }

    event.source.dropContainer.connectedTo = connectedTo;
    
    if (!this.isReleased) {
      event.source.dropContainer._dropListRef.enter(
        event.source._dragRef, 0, 0
      );
    }
    
    if (this.isReleased) {
      try {
        const result = await this.signalrService.UnlockCourseEdition(event.source.data.CourseId, event.source.data.CourseEditionId).toPromise();
        if (result.statusCode >= 400) {
          throw result;
        }
        event.source.data.Locked = false;
      } catch (error) {

      }
      this.currentDragEvent = null;
      for (let i = 0; i < this.scheduleSlots.length; ++i) {
        this.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = false;
      }
    }
  }

  async OnReleaseDragging(event:CdkDragRelease<CourseEdition>) {
    this.isReleased = true;
    
    event.source.dropContainer.connectedTo = [];

    const numberOfSlots = this.settings.periods.length - 1;
    for (let i = 0; i < this.scheduleSlots.length; ++i) {
      this.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = false;
    }
  }

  Reset(dayIndex:number, slotIndex:number, event:CourseEdition) {
    transferArrayItem<CourseEdition>(
      this.schedule[dayIndex][slotIndex],
      this.myCourses,
      0,
      this.myCourses.length
    );
  }
}
