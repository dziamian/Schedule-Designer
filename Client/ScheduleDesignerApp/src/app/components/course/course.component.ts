import { CdkDrag, CdkDragRelease, CdkDragStart } from '@angular/cdk/drag-drop';
import { Component, Input, OnInit, Output, EventEmitter, ViewChild } from '@angular/core';
import { CourseEdition } from 'src/app/others/CourseEdition';

@Component({
  selector: 'app-course',
  templateUrl: './course.component.html',
  styleUrls: ['./course.component.css']
})
export class CourseComponent implements OnInit {

  @ViewChild(CdkDrag) cdkCourse : CdkDrag<CourseEdition>;

  @Input() course:CourseEdition;
  
  @Output() ctrlClick:EventEmitter<CourseEdition> = new EventEmitter<CourseEdition>();
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
}
