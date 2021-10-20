import { Component, OnInit, QueryList, ViewChildren } from '@angular/core';
import { CdkDrag, CdkDragDrop, CdkDropList, DragRef, DropListRef, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';

import { ScheduleDesignerApiService } from 'src/app/services/ScheduleDesignerApiService/schedule-designer-api.service';
import { Course } from 'src/app/others/Course';
import { CourseType } from 'src/app/others/CourseType';
import { Group } from 'src/app/others/Group';
import { Coordinator } from 'src/app/others/Coordinator';
import { SignalrService } from 'src/app/services/SignalrService/signalr.service';
import { MatDialog } from '@angular/material/dialog';
import { DialogExampleComponent } from 'src/app/components/dialog-example/dialog-example.component';
import { CourseComponent } from 'src/app/components/course/course.component';

@Component({
  selector: 'app-schedule',
  templateUrl: './schedule.component.html',
  styleUrls: ['./schedule.component.css']
})
export class ScheduleComponent implements OnInit {

  @ViewChildren('scheduleSlots') scheduleSlots : QueryList<DropListRef>;
  @ViewChildren(CourseComponent) courses : QueryList<CourseComponent>;

  numberOfWeeks:number = 15;
  currentTabIndex:number = 0;
  tabLabels:string[] = ['Semester', 'Even Weeks', 'Odd Weeks'];

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
  schedule:Course[][] = [[new Course(
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
      CourseType.Lecture, 
      15, 
      new Group("3ID12A"), 
      [
        new Coordinator("Mariusz", "Bedla", "dr inż."), 
        new Coordinator("Grzegorz", "Łukawski", "dr inż."),
        new Coordinator("Antoni", "Antoniak", "dr inż.")
      ]
    )],[],[]];

  constructor(
    private scheduleDesignerApiService:ScheduleDesignerApiService,
    private signalrService:SignalrService,
    private dialog:MatDialog
  ) 
  { }

  private setLabels() {
    for (let i:number = 0; i < this.numberOfWeeks; ++i) {
      this.tabLabels.push('Week ' + (i + 1));
    }
  }

  ngOnInit(): void {
    this.signalrService.initConnection();
    /*this.signalrService.testMessage.subscribe((testMessage: string) => {
      this.testMessage = testMessage;
    });*/

    this.setLabels();
  }

  OnTabChange(index:number) {
    //load data
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
        event.previousContainer.data,
        1 - event.currentIndex,
        event.previousIndex
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

  ScheduleSlotEnterPredicate(course:CdkDrag<Course>, slot:CdkDropList<Course[]>) {
    return slot.data.length < 2;
  }

  Reset(index:number, event:Course) {
    transferArrayItem<Course>(
      this.schedule[index],
      this.yourCourses,
      0,
      this.yourCourses.length
    );
  }
}
