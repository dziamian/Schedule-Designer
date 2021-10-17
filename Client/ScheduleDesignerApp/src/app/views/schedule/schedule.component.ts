import { Component, OnInit } from '@angular/core';
import { CdkDragDrop, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';

import { ScheduleDesignerApiService } from 'src/app/services/ScheduleDesignerApiService/schedule-designer-api.service';
import { Course } from 'src/app/others/Course';
import { CourseType } from 'src/app/others/CourseType';
import { Group } from 'src/app/others/Group';
import { Coordinator } from 'src/app/others/Coordinator';
import { SignalrService } from 'src/app/services/SignalrService/signalr.service';
import { MatDialog } from '@angular/material/dialog';
import { DialogExampleComponent } from 'src/app/components/dialog-example/dialog-example.component';

@Component({
  selector: 'app-schedule',
  templateUrl: './schedule.component.html',
  styleUrls: ['./schedule.component.css']
})
export class ScheduleComponent implements OnInit {

  termLength:number = 15;
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
        new Coordinator("Grzegorz", "Łukawski", "dr inż.")
      ]
    ),
  ];
  schedule:Course[] = [];

  testMessage:string;

  constructor(
    private scheduleDesignerApiService:ScheduleDesignerApiService,
    private signalrService:SignalrService,
    private dialog:MatDialog
  ) 
  { }

  ngOnInit(): void {
    this.signalrService.initConnection();
    this.signalrService.testMessage.subscribe((testMessage: string) => {
      this.testMessage = testMessage;
    });

    this.setLabels();
  }

  private setLabels() {
    for (let i:number = 0; i < this.termLength; ++i) {
      this.tabLabels.push('Week ' + (i + 1));
    }
  }

  public click() {
    console.log("click");
  }

  async Drop(event:CdkDragDrop<Course[]>) {
    const dialog = this.dialog.open(DialogExampleComponent);

    await dialog.afterClosed().toPromise();
    
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
}
