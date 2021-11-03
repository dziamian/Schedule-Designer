import { CdkDrag, CdkDragRelease, CdkDragStart } from '@angular/cdk/drag-drop';
import { Component, Input, OnInit, Output, EventEmitter, ViewChild } from '@angular/core';
import { Store } from '@ngrx/store';
import { Account } from 'src/app/others/Accounts';
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

  account:Account;

  constructor(
    private store:Store<{account:Account}>
  ) {
    this.store.select('account').subscribe((account) => {
      if (account.UserId == 0) {
        return;
      }
      this.account = account;
    });
  }

  ngOnInit(): void {
  }

  CtrlClick(event:MouseEvent) {
    if (!this.cdkCourse.disabled && event.ctrlKey) {
      this.ctrlClick.emit(this.course);
    }
  }

  CheckIfItsMe(id:number):boolean {
    return this.account.UserId == id;
  }

  OnStarted(event:CdkDragStart) {
    this.start.emit(event);
  }

  OnReleased(event:CdkDragRelease) {
    this.release.emit(event);
  }
}
