import { Component, Input, OnInit, Output, EventEmitter } from '@angular/core';
import { Course } from 'src/app/others/Course';
import { CourseType } from 'src/app/others/CourseType';

@Component({
  selector: 'app-course',
  templateUrl: './course.component.html',
  styleUrls: ['./course.component.css']
})
export class CourseComponent implements OnInit {

  @Input() course?:Course;
  @Output() ctrlClick:EventEmitter<Course> = new EventEmitter<Course>();

  constructor() { }

  ngOnInit(): void {
  }

  CtrlClick(event:MouseEvent) {
    if (event.ctrlKey) {
      this.ctrlClick.emit(this.course);
    }
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
