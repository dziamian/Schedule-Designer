import { Component, ElementRef, OnInit, QueryList, ViewChildren } from '@angular/core';
import { CdkDrag, CdkDragDrop, CdkDragEnter, CdkDragRelease, CdkDragStart, CdkDropList, DropListRef, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { ScheduleDesignerApiService } from 'src/app/services/ScheduleDesignerApiService/schedule-designer-api.service';
import { CourseEdition } from 'src/app/others/CourseEdition';
import { CourseType } from 'src/app/others/CourseType';
import { Group } from 'src/app/others/Group';
import { Coordinator } from 'src/app/others/Coordinator';
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

  @ViewChildren('scheduleSlots') scheduleSlots : QueryList<DropListRef<CourseEdition>>;
  @ViewChildren(CourseComponent) courses : QueryList<CourseComponent>;

  loading:boolean = true;
  tabLoading:boolean = true;
  connectionStatus:boolean = false;
  snackBarDuration:number = 10 * 1000;
  
  settings:Settings;
  currentTabIndex:number = 0;
  
  tabLabels:string[] = ['Semester', 'Even Weeks', 'Odd Weeks'];
  scheduleTimeLabels:string[] = [];
  scheduleDayLabels:string[] = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday'];

  courseTypes:Map<number, CourseType>;

  yourCourses:CourseEdition[] = [
    new CourseEdition(
      "Systemy Operacyjne 2", 
      new CourseType(1, "Wykład", "#D9FFFF"), 
      15, 
      [
        new Group("3ID12A")
      ], 
      [
        new Coordinator("Mariusz", "Bedla", "dr inż."), 
        new Coordinator("Grzegorz", "Łukawski", "dr inż."),
        new Coordinator("Antoni", "Antoniak", "dr inż.")
      ]
    ),
    new CourseEdition(
      "Systemy Operacyjne 2", 
      new CourseType(2, "Laboratorium", "#FFECFF"), 
      15, 
      [
        new Group("3ID12A")
      ],
      [
        new Coordinator("Mariusz", "Bedla", "dr inż."), 
        new Coordinator("Grzegorz", "Łukawski", "dr inż.")
      ]
    ),
    new CourseEdition(
      "Systemy Operacyjne 2", 
      new CourseType(3, "Projekt", "#FFFFEA"), 
      15, 
      [
        new Group("3ID12A")
      ],
      [
        new Coordinator("Mariusz", "Bedla", "dr inż."), 
        new Coordinator("Grzegorz", "Łukawski", "dr inż.")
      ]
    ),
    new CourseEdition(
      "Systemy Operacyjne 2", 
      new CourseType(4, "Ćwiczenia", "#E6FFE6"), 
      15, 
      [
        new Group("3ID12A")
      ],
      [
        new Coordinator("Mariusz", "Bedla", "dr inż."),
      ]
    ),
  ];
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
      this.initializeScheduleTable();

      this.loading = false;
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

  private setLabels() {
    for (let i:number = 0; i < this.settings.TermDurationWeeks; ++i) {
      this.tabLabels.push('Week ' + (i + 1));
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

  OnTabChange(index:number) {
    //remove yourCourses, schedule
    //load yourCourses, schedule
    console.log(this.scheduleSlots);
  }

  async DropInMyCourses(event:CdkDragDrop<CourseEdition[]>) {
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
      event.container.data[event.currentIndex].room = null;
    }
  }

  async DropInSchedule(event:CdkDragDrop<CourseEdition[]>) {
    if (event.previousContainer === event.container) {
      return;
    }

    if (event.container.data.length == 1) {
      //must be confirmed and not scheduled
      const dialog = this.dialog.open(RoomSelectionComponent);
      await dialog.afterClosed().toPromise();

      event.previousContainer.data[event.previousIndex].room = new Room("EXAMPLE");

      transferArrayItem(
        event.previousContainer.data,
        event.container.data,
        event.previousIndex,
        event.currentIndex
      );

      event.container.data[1 - event.currentIndex].room = null;

      transferArrayItem(
        event.container.data,
        this.yourCourses,
        1 - event.currentIndex,
        this.yourCourses.length
      );

      return;
    }

    const dialog = this.dialog.open(RoomSelectionComponent);

    await dialog.afterClosed().toPromise();
    //get room and set it
    event.previousContainer.data[event.previousIndex].room = new Room("EXAMPLE");

    //if room has been chosen and accepted by API
    transferArrayItem(
      event.previousContainer.data,
      event.container.data,
      event.previousIndex,
      event.currentIndex
    );
    //otherwise if room has been chosen but is busy -> scheduledmove
  }

  IsScheduleSlotDisabled(dayIndex:number, slotIndex:number) {
    return this.schedule[dayIndex][slotIndex].length > 1;
  }

  ScheduleSlotEnterPredicate(course:CdkDrag<CourseEdition>, slot:CdkDropList<CourseEdition[]>) {
    return slot.data.length < 2;
  }

  async OnStartDragging(event:CdkDragStart) {
    var freeSlots = await this.scheduleDesignerApiService.GetFreePeriods().toPromise();
    var numberOfSlots = this.scheduleTimeLabels.length;
    var freeSlotIndex = 0;
    var scheduleSlots = this.scheduleSlots.toArray();
    for (var i = 0; i < this.scheduleSlots.length; ++i) {
      if (i == (freeSlots[freeSlotIndex] - 1) * numberOfSlots + (freeSlots[freeSlotIndex + 1] - 1)) {
        scheduleSlots[i].enterPredicate = (drag, drop) => drop.data.length < 2;
        let element = scheduleSlots[i].element as ElementRef<HTMLElement>;
        element.nativeElement.setAttribute('selected', '');
        freeSlotIndex += 2;
      } else {
        scheduleSlots[i].enterPredicate = () => false;
      }
    }
  }

  OnReleaseDragging(event:CdkDragRelease) {
    this.scheduleSlots.forEach((slot) => {
      var element = slot.element as ElementRef<HTMLElement>;
      element.nativeElement.removeAttribute('selected');
    });
  }

  Reset(dayIndex:number, slotIndex:number, event:CourseEdition) {
    transferArrayItem<CourseEdition>(
      this.schedule[dayIndex][slotIndex],
      this.yourCourses,
      0,
      this.yourCourses.length
    );
  }
}
