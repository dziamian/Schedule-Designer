import { CdkDrag, CdkDragRelease, CdkDragStart } from '@angular/cdk/drag-drop';
import { Component, Input, OnInit, Output, EventEmitter, ViewChild } from '@angular/core';
import { Course } from 'src/app/others/Course';
import { CourseType } from 'src/app/others/CourseType';

@Component({
  selector: 'app-course',
  templateUrl: './course.component.html',
  styleUrls: ['./course.component.css']
})
export class CourseComponent implements OnInit {

  @ViewChild(CdkDrag) cdkCourse : CdkDrag<Course>;

  @Input() course?:Course;
  
  @Output() ctrlClick:EventEmitter<Course> = new EventEmitter<Course>();
  @Output() start:EventEmitter<CdkDragStart> = new EventEmitter<CdkDragStart>();
  @Output() release:EventEmitter<CdkDragRelease> = new EventEmitter<CdkDragRelease>();

  constructor() { }

  ngOnInit(): void {
  }

  CtrlClick(event:MouseEvent) {
    if (!this.cdkCourse.disabled && event.ctrlKey) {
      this.ctrlClick.emit(this.course);
    }
  }

  OnStarted(event:CdkDragStart) {
    this.start.emit(event);
  }

  OnReleased(event:CdkDragRelease) {
    this.release.emit(event);
  }

  public getCourseTypeName(type?:CourseType):string {
    switch (type) {
      case CourseType.Lecture: return "Wykład";
      case CourseType.Laboratory: return "Laboratorium";
      case CourseType.Project: return "Projekt";
      case CourseType.Exercise: return "Ćwiczenia";
      default: return "";
    }
  }
}
