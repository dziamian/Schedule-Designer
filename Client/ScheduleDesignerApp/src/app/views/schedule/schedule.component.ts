import { Component, ElementRef, OnInit, QueryList, ViewChild, ViewChildren } from '@angular/core';
import { CdkDrag, CdkDragDrop, CdkDragEnter, CdkDragRelease, CdkDragStart, CdkDropList, DropListRef, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { ScheduleDesignerApiService } from 'src/app/services/ScheduleDesignerApiService/schedule-designer-api.service';
import { CourseEdition } from 'src/app/others/CourseEdition';
import { CourseType } from 'src/app/others/CourseType';
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

  myCourses:CourseEdition[];
  schedule:CourseEdition[][][];

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
      this.myCourses.forEach((value) => {
        if (value.CourseId == courseId && value.CourseEditionId == courseEditionId) {
          value.Locked = true;
        }
      });
    });

    this.signalrService.lastUnlockedCourseEdition.pipe(skip(1)).subscribe(({courseId, courseEditionId}) => {
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
      this.scheduleDesignerApiService.GetCourseTypes()
    ]).subscribe(([,settings,periods,courseTypes]) => {
      this.connectionStatus = true;
      
      this.settings = settings;
      this.settings.periods = periods;
      this.courseTypes = courseTypes;
      this.setLabels();
      this.setFrequenciesAndWeeks();
      this.initializeScheduleTable();

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
      this.scheduleDesignerApiService.GetScheduleAsCoordinator(this.weeks[index], this.courseTypes, this.schedule)
    ]).subscribe(([myCourses, mySchedule]) => {
      this.myCourses = myCourses;
      this.schedule = mySchedule;

      let allGroups = new Array<Group>();
      
      this.myCourses.forEach((courseEdition) => {
        courseEdition.Groups.forEach((group) => {
          allGroups.push(group);
        });
      });

      this.scheduleDesignerApiService.GetGroupsFullNames(allGroups.map((e) => e.GroupId)).subscribe((groupsFullNames) => {
        for (let i = 0; i < groupsFullNames.length; ++i) {
          allGroups[i].FullName = groupsFullNames[i];
        }
      });

      
      this.loading = false;
      this.tabLoading = false;
      //TEST
        /*setTimeout(() => {
          console.log("TEST");
          console.log(this.myCoursesSlot);
          console.log(this.scheduleSlots);
          if (this.currentDragEvent != null) {
            const numberOfSlots = this.scheduleTimeLabels.length;
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

  private initializeScheduleTable() {
    const periods = this.settings.periods;
    const numberOfSlots = this.settings.periods.length - 1;
    
    for (let i:number = 0; i < numberOfSlots; ++i) {
      this.scheduleTimeLabels.push(
        periods[i] + ' - ' + periods[i + 1]
      );
    }

    this.schedule = [];
    for (let j:number = 0; j < 5; ++j) {
      this.schedule.push([]);
      for (let i:number = 0; i < numberOfSlots; ++i) {
        this.schedule[j].push([]);
      }
    }
  }

  private clearSchedule() {
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

  OnTabChange(index:number) {
    this.currentTabIndex = index;
    this.tabLoading = true;

    this.clearSchedule();
    this.getMyCourseEditionsAndScheduleAsCoordinator(index);
  }

  async DropInMyCourses(event:CdkDragDrop<CourseEdition[], CourseEdition[], CourseEdition>) {
    if (this.isCanceled) {
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
      event.container.data[event.currentIndex].Weeks = null;
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
  }

  async DropInSchedule(event:CdkDragDrop<CourseEdition[], CourseEdition[], CourseEdition>, dayIndex:number, slotIndex:number) {
    this.isReleased = true;
    
    const index = dayIndex * this.scheduleTimeLabels.length + slotIndex;

    if (this.isCanceled) {
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
      return;
    }

    if (event.container.data.length == 1) {
      //must be confirmed and not scheduled
      this.currentOpenedDialog = this.dialog.open(RoomSelectionComponent);
      const result = await this.currentOpenedDialog.afterClosed().toPromise();
      this.currentOpenedDialog = null;
      if (result == false) {
        return;
      } 

      event.previousContainer.data[event.previousIndex].Room = new Room(1, "EXAMPLE");
      event.previousContainer.data[event.previousIndex].Weeks = [1];

      transferArrayItem(
        event.previousContainer.data,
        event.container.data,
        event.previousIndex,
        event.currentIndex
      );

      event.container.data[1 - event.currentIndex].Room = null;
      event.container.data[1 - event.currentIndex].Weeks = null;

      transferArrayItem(
        event.container.data,
        this.myCourses,
        1 - event.currentIndex,
        this.myCourses.length
      );

      try {
        const result = await this.signalrService.UnlockCourseEdition(event.item.data.CourseId, event.item.data.CourseEditionId).toPromise();
        if (result.statusCode >= 400) {
          throw result;
        }
        event.item.data.Locked = false;
      } catch(error) {
      
      }
      this.currentDragEvent = null;
      return;
    }

    this.currentOpenedDialog = this.dialog.open(RoomSelectionComponent);
    const result = await this.currentOpenedDialog.afterClosed().toPromise();
    this.currentOpenedDialog = null;
    if (result == false) {
      return;
    } 
    
    //get room and set it
    event.previousContainer.data[event.previousIndex].Room = new Room(1, "EXAMPLE");
    event.previousContainer.data[event.previousIndex].Weeks = [1];

    //if room has been chosen and accepted by API
    transferArrayItem(
      event.previousContainer.data,
      event.container.data,
      event.previousIndex,
      event.currentIndex
    );
    //otherwise if room has been chosen but is busy -> scheduledmove

    try {
      const result = await this.signalrService.UnlockCourseEdition(event.item.data.CourseId, event.item.data.CourseEditionId).toPromise();
      if (result.statusCode >= 400) {
        throw result;
      }
      event.item.data.Locked = false;
    } catch (error) {

    }
    this.currentDragEvent = null;
  }

  IsScheduleSlotDisabled(dayIndex:number, slotIndex:number) {
    return this.schedule[dayIndex][slotIndex].length > 1;
  }

  ScheduleSlotEnterPredicate(drag:CdkDrag<CourseEdition>, drop:CdkDropList<CourseEdition[]>) {
    return drop.data.length < 1;
  }

  OnScheduleSlotEnter(drag:CdkDragEnter<CourseEdition[]>) {
    let element = drag.container.element as ElementRef<HTMLElement>;
    if (element.nativeElement.getAttribute('selected')) {
      this.isMoveValid = true;
    } else {
      this.isMoveValid = false;
    }
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
    
    let numberOfSlots = this.scheduleTimeLabels.length;
    let busySlotIndex = 0;
    let scheduleSlots = this.scheduleSlots.toArray();
    for (let i = 0; i < this.scheduleSlots.length; ++i) {
      let element = scheduleSlots[i].element as ElementRef<HTMLElement>;
      connectedTo.push(element.nativeElement.id);
      if (i != (busySlots[busySlotIndex]?.PeriodIndex - 1) * numberOfSlots + (busySlots[busySlotIndex]?.Day - 1)) {
        element.nativeElement.setAttribute('selected', '');
        ++busySlotIndex;
      } else {
        element.nativeElement.removeAttribute('selected');
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
        let element = scheduleSlots[i].element as ElementRef<HTMLElement>;
        element.nativeElement.removeAttribute('selected');
      }
    }
  }

  async OnReleaseDragging(event:CdkDragRelease<CourseEdition>) {
    this.isReleased = true;
    //this.currentDragEvent = null;
    //event.source.dropContainer.connectedTo = [];
    let scheduleSlots = this.scheduleSlots.toArray();
    for (let i = 0; i < this.scheduleSlots.length; ++i) {
      let element = scheduleSlots[i].element as ElementRef<HTMLElement>;
      element.nativeElement.removeAttribute('selected');
    }
    this.isMoveValid = null;
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
