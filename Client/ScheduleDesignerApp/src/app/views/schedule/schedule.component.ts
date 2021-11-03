import { Component, ElementRef, OnInit, QueryList, ViewChild, ViewChildren } from '@angular/core';
import { CdkDrag, CdkDragDrop, CdkDragEnter, CdkDragRelease, CdkDragStart, CdkDropList, DropListRef, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { ScheduleDesignerApiService } from 'src/app/services/ScheduleDesignerApiService/schedule-designer-api.service';
import { CourseEdition } from 'src/app/others/CourseEdition';
import { CourseType } from 'src/app/others/CourseType';
import { Group } from 'src/app/others/Group';
import { SignalrService } from 'src/app/services/SignalrService/signalr.service';
import { MatDialog } from '@angular/material/dialog';
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
  isReleased:boolean = false;

  loading:boolean = true;
  connectionStatus:boolean = false;
  snackBarDuration:number = 10 * 1000;
  
  settings:Settings;
  frequencies:number[];
  
  currentTabIndex:number = 0;
  
  tabLabels:string[] = ['Semester', 'Even Weeks', 'Odd Weeks'];
  scheduleTimeLabels:string[] = [];
  scheduleDayLabels:string[] = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday'];

  courseTypes:Map<number, CourseType>;

  myCourses:CourseEdition[];
  schedule:CourseEdition[][][] = [];

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
        this.snackBar.open("Connection with server has been lost. Please refresh the page to possibly reconnect.", "OK", {
          duration: this.snackBarDuration
        });
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
      this.setFrequencies();
      this.initializeScheduleTable();

      this.getMyCourseEditions(0);
    }, (error) => {
      if (error?.status == 401) {
        this.usosApiService.Deauthorize();

        this.snackBar.open('Session expired. Please log in again.', 'OK', {
          duration: this.snackBarDuration
        });
        this.router.navigate(['login']);
      } else if (!isConnectedSubscription.closed) {
        this.snackBar.open("Connection with server failed. Please refresh the page to try again.", "OK", {
          duration: this.snackBarDuration
        });
      }
    });
  }

  private getMyCourseEditions(index:number) {
    let roundUp = (index != 1);

    this.scheduleDesignerApiService.GetMyCourseEditions(this.frequencies[index], this.courseTypes, this.settings, roundUp).subscribe((myCourses) => {
      this.myCourses = myCourses;
      
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

  private setFrequencies() {
    this.frequencies = [this.settings.TermDurationWeeks, this.settings.TermDurationWeeks / 2, this.settings.TermDurationWeeks / 2];
    for (let i:number = 0; i < this.settings.TermDurationWeeks; ++i) {
      this.frequencies.push(1);
    }
  }

  private initializeScheduleTable() {
    let periods = this.settings.periods;
    let numberOfSlots = this.settings.periods.length - 1;
    for (let i:number = 0; i < numberOfSlots; ++i) {
      this.scheduleTimeLabels.push(
        periods[i] + ' - ' + periods[i + 1]
      );
    }

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
    console.log(this.scheduleSlots);

    this.getMyCourseEditions(index);
  }

  async DropInMyCourses(event:CdkDragDrop<CourseEdition[], CourseEdition[], CourseEdition>) {
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
    }
    
    const unlockResult = await this.scheduleDesignerApiService.UnlockCourseEdition(event.item.data.CourseId, event.item.data.CourseEditionId).toPromise();
    event.item.data.Locked = false;
  }

  async DropInSchedule(event:CdkDragDrop<CourseEdition[], CourseEdition[], CourseEdition>, dayIndex:number, slotIndex:number) {
    const index = dayIndex * this.scheduleTimeLabels.length + slotIndex;

    /*if (!this.allowedSlotIndexes[index]) {
      const unlockResult = await this.scheduleDesignerApiService.UnlockCourseEdition(event.item.data.CourseId, event.item.data.CourseEditionId).toPromise();
      event.item.data.Locked = false;
      return;
    }*/

    if (event.previousContainer === event.container) {
      const unlockResult = await this.scheduleDesignerApiService.UnlockCourseEdition(event.item.data.CourseId, event.item.data.CourseEditionId).toPromise();
      event.item.data.Locked = false;
      return;
    }

    if (event.container.data.length == 1) {
      //must be confirmed and not scheduled
      const dialog = this.dialog.open(RoomSelectionComponent);
      await dialog.afterClosed().toPromise();

      event.previousContainer.data[event.previousIndex].Room = new Room(1, "EXAMPLE");

      transferArrayItem(
        event.previousContainer.data,
        event.container.data,
        event.previousIndex,
        event.currentIndex
      );

      event.container.data[1 - event.currentIndex].Room = null;

      transferArrayItem(
        event.container.data,
        this.myCourses,
        1 - event.currentIndex,
        this.myCourses.length
      );

      const unlockResult = await this.scheduleDesignerApiService.UnlockCourseEdition(event.item.data.CourseId, event.item.data.CourseEditionId).toPromise();
      event.item.data.Locked = false;
      return;
    }

    const dialog = this.dialog.open(RoomSelectionComponent);

    await dialog.afterClosed().toPromise();
    //get room and set it
    event.previousContainer.data[event.previousIndex].Room = new Room(1, "EXAMPLE");

    //if room has been chosen and accepted by API
    transferArrayItem(
      event.previousContainer.data,
      event.container.data,
      event.previousIndex,
      event.currentIndex
    );
    //otherwise if room has been chosen but is busy -> scheduledmove

    const unlockResult = await this.scheduleDesignerApiService.UnlockCourseEdition(event.item.data.CourseId, event.item.data.CourseEditionId).toPromise();
    event.item.data.Locked = false;
  }

  IsScheduleSlotDisabled(dayIndex:number, slotIndex:number) {
    return this.schedule[dayIndex][slotIndex].length > 1;
  }

  ScheduleSlotEnterPredicate(drag:CdkDrag<CourseEdition>, drop:CdkDropList<CourseEdition[]>) {
    return drop.data.length < 2;
  }

  async OnStartDragging(event:CdkDragStart<CourseEdition>) {
    this.isReleased = false;
    try {
      const lockResult = await this.scheduleDesignerApiService.LockCourseEdition(event.source.data.CourseId, event.source.data.CourseEditionId).toPromise();
    } catch (error) {
      if (!this.isReleased) {
        event.source.dropContainer._dropListRef.drop(
          event.source._dragRef,
          0,
          0,
          event.source.dropContainer._dropListRef,
          false,
          {x: 0, y:0},
          {x: 0, y:0}
        );
      }
    }
    this.currentDragEvent = event;
    let freeSlots = await this.scheduleDesignerApiService.GetFreePeriods().toPromise();
    event.source.dropContainer.connectedTo = ['my-courses', '0,0', '1,1', '3,3'];
    if (!this.isReleased) {
      event.source.dropContainer._dropListRef.enter(
        event.source._dragRef, 0, 0
      );
    }
    let numberOfSlots = this.scheduleTimeLabels.length;
    let freeSlotIndex = 0;
    let scheduleSlots = this.scheduleSlots.toArray();
    for (let i = 0; i < this.scheduleSlots.length; ++i) {
      if (i == (freeSlots[freeSlotIndex] - 1) * numberOfSlots + (freeSlots[freeSlotIndex + 1] - 1)) {
        //scheduleSlots[i].enterPredicate = (drag, drop) => { return drop.data.length < 2; };
        let element = scheduleSlots[i].element as ElementRef<HTMLElement>;
        element.nativeElement.setAttribute('selected', '');
        freeSlotIndex += 2; 
      } else {
        //scheduleSlots[i].enterPredicate = () => false;
        let element = scheduleSlots[i].element as ElementRef<HTMLElement>;
        element.nativeElement.removeAttribute('selected');
      }
    }
  }

  async OnReleaseDragging(event:CdkDragRelease<CourseEdition>) {
    this.isReleased = true;
    this.currentDragEvent = null;
    //event.source.dropContainer.connectedTo = [];
    let scheduleSlots = this.scheduleSlots.toArray();
    for (let i = 0; i < this.scheduleSlots.length; ++i) {
      let element = scheduleSlots[i].element as ElementRef<HTMLElement>;
      element.nativeElement.removeAttribute('selected');
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
