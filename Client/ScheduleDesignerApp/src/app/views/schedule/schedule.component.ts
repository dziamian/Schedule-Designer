import { Component, OnInit, QueryList, ViewChildren } from '@angular/core';
import { CdkDrag, CdkDragDrop, CdkDragEnter, CdkDropList, DropListRef, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';

import { ScheduleDesignerApiService } from 'src/app/services/ScheduleDesignerApiService/schedule-designer-api.service';
import { Course } from 'src/app/others/Course';
import { CourseType } from 'src/app/others/CourseType';
import { Group } from 'src/app/others/Group';
import { Coordinator } from 'src/app/others/Coordinator';
import { SignalrService } from 'src/app/services/SignalrService/signalr.service';
import { MatDialog } from '@angular/material/dialog';
import { DialogExampleComponent } from 'src/app/components/dialog-example/dialog-example.component';
import { CourseComponent } from 'src/app/components/course/course.component';
import { Settings } from 'src/app/others/Settings';
import { forkJoin } from 'rxjs';
import { MatSnackBar } from '@angular/material/snack-bar';

@Component({
  selector: 'app-schedule',
  templateUrl: './schedule.component.html',
  styleUrls: ['./schedule.component.css']
})
export class ScheduleComponent implements OnInit {

  @ViewChildren('scheduleSlots') scheduleSlots : QueryList<DropListRef>;
  @ViewChildren(CourseComponent) courses : QueryList<CourseComponent>;

  loading:boolean = true;
  
  settings:Settings;
  currentTabIndex:number = 0;
  
  tabLabels:string[] = ['Semester', 'Even Weeks', 'Odd Weeks'];
  scheduleTimeLabels:string[] = [];
  scheduleDayLabels:string[] = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday'];

  yourCourses:Course[] = [
    new Course(
      "Systemy Operacyjne 2", 
      CourseType.Lecture, 
      15, 
      new Group("3ID12A"), 
      [
        new Coordinator("Mariusz", "Bedla", "dr inż."), 
        new Coordinator("Grzegorz", "Łukawski", "dr inż."),
        new Coordinator("Antoni", "Antoniak", "dr inż.")
      ]
    ),
    new Course(
      "Systemy Operacyjne 2", 
      CourseType.Laboratory, 
      15, 
      new Group("3ID12A"), 
      [
        new Coordinator("Mariusz", "Bedla", "dr inż."), 
        new Coordinator("Grzegorz", "Łukawski", "dr inż.")
      ]
    ),
    new Course(
      "Systemy Operacyjne 2", 
      CourseType.Project, 
      15, 
      new Group("3ID12A"), 
      [
        new Coordinator("Mariusz", "Bedla", "dr inż."), 
        new Coordinator("Grzegorz", "Łukawski", "dr inż.")
      ]
    ),
    new Course(
      "Systemy Operacyjne 2", 
      CourseType.Exercise, 
      15, 
      new Group("3ID12A"), 
      [
        new Coordinator("Mariusz", "Bedla", "dr inż."),
      ]
    ),
  ];
  schedule:Course[][][] = [];

  constructor(
    private scheduleDesignerApiService:ScheduleDesignerApiService,
    private signalrService:SignalrService,
    private snackBar:MatSnackBar,
    private dialog:MatDialog
  ) 
  { }

  ngOnInit(): void {
    forkJoin([
      this.signalrService.initConnection(),
      this.scheduleDesignerApiService.GetSettings(),
      this.scheduleDesignerApiService.GetPeriods()
    ]).subscribe(([,settings,periods]) => {
      this.settings = settings;
      this.settings.periods = periods;
      this.setLabels();
      this.initializeScheduleTable();

      this.loading = false;
    }, () => {
      this.snackBar.open("Connection with server failed. Please refresh the page to try again.", "OK");
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

  async DropInMyCourses(event:CdkDragDrop<Course[]>) {
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
    }
  }

  async DropInSchedule(event:CdkDragDrop<Course[]>) {
    if (event.previousContainer === event.container) {
      return;
    }

    if (event.container.data.length == 1) {
      //must be confirmed and not scheduled
      if (event.previousContainer.id != 'your-courses') {
        //different dialog
        const dialog = this.dialog.open(DialogExampleComponent);
        await dialog.afterClosed().toPromise();
      } else {
        const dialog = this.dialog.open(DialogExampleComponent);
        await dialog.afterClosed().toPromise();
      }

      transferArrayItem(
        event.previousContainer.data,
        event.container.data,
        event.previousIndex,
        event.currentIndex
      );

      transferArrayItem(
        event.container.data,
        this.yourCourses,
        1 - event.currentIndex,
        this.yourCourses.length
      );

      return;
    }

    const dialog = this.dialog.open(DialogExampleComponent);

    await dialog.afterClosed().toPromise();
    //get room and set it
    //console.log(event.previousContainer.data[event.previousIndex].room = new Room("New room"));

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

  YourCoursesEnter(event:CdkDragEnter<Course[]>) {
    //event.item.getPlaceholderElement().style.display = '';
  }

  ScheduleSlotEnter(event:CdkDragEnter<Course[]>) {
    /*if (event.container == event.item.dropContainer) {
      event.item.getPlaceholderElement().style.display = '';
      return;
    }
    if (event.container.data.length > 0) {
      event.item.getPlaceholderElement().style.display = 'none';
    } else {
      event.item.getPlaceholderElement().style.display = '';
    }*/
  }

  ScheduleSlotEnterPredicate(course:CdkDrag<Course>, slot:CdkDropList<Course[]>) {
    //maybe let's do it after start dragging -> disable unacceptable slots and change style for acceptable
    //check if slot is on acceptable list for course (took from API - free slots for groups and coordinators)
    return slot.data.length < 2;
  }

  Reset(dayIndex:number, slotIndex:number, event:Course) {
    transferArrayItem<Course>(
      this.schedule[dayIndex][slotIndex],
      this.yourCourses,
      0,
      this.yourCourses.length
    );
  }
}
