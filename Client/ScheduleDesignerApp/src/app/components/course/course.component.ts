import { CdkDrag, CdkDragRelease, CdkDragStart } from '@angular/cdk/drag-drop';
import { Component, Input, OnInit, Output, EventEmitter, ViewChild } from '@angular/core';
import { Store } from '@ngrx/store';
import { Account } from 'src/app/others/Accounts';
import { CourseEdition } from 'src/app/others/CourseEdition';
import { Settings } from 'src/app/others/Settings';

@Component({
  selector: 'app-course',
  templateUrl: './course.component.html',
  styleUrls: ['./course.component.css']
})
export class CourseComponent implements OnInit {

  @ViewChild(CdkDrag) cdkCourse : CdkDrag<CourseEdition>;

  @Input() course:CourseEdition;
  @Input() settings:Settings;
  @Input() weeksOnTab:number[];
  
  @Output() select:EventEmitter<CourseEdition> = new EventEmitter<CourseEdition>();
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

  Click(event:MouseEvent) {
    if (!this.cdkCourse.disabled) {
      this.select.emit(this.course);
    }
  }

  CheckIfItsMe(id:number):boolean {
    return this.account.UserId == id;
  }

  CheckIfNotMatching():boolean {
    if (this.course.Weeks == null) return false;
    return this.weeksOnTab?.sort((a,b) => a - b).join(',') 
      !== this.course.Weeks?.sort((a,b) => a - b).join(',');
  }

  CheckIfMatching():boolean {
    if (this.course.Weeks == null) return false;
    return this.weeksOnTab?.sort((a,b) => a - b).join(',') 
      === this.course.Weeks?.sort((a,b) => a - b).join(',');
  }

  OnStarted(event:CdkDragStart) {
    this.start.emit(event);
  }

  OnReleased(event:CdkDragRelease) {
    this.release.emit(event);
  }

  Floor(number:number):number {
    return Math.floor(number);
  }

  Ceil(number:number):number {
    return Math.ceil(number);
  }
}
