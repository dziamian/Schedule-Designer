import { CdkDrag, CdkDragRelease, CdkDragStart } from '@angular/cdk/drag-drop';
import { Component, Input, OnInit, Output, EventEmitter, ViewChild, SimpleChanges } from '@angular/core';
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

  @Input() isModifying:boolean;
  @Input() ignoreUsersLocks:boolean;
  @Input() course:CourseEdition;
  @Input() settings:Settings;
  @Input() weeksOnTab:number[];
  @Input() isSelectedMoving:boolean|undefined;
  
  @Output() onSelect:EventEmitter<{courseEdition:CourseEdition,isDisabled:boolean}> = new EventEmitter<{courseEdition:CourseEdition,isDisabled:boolean}>();
  @Output() onStart:EventEmitter<CdkDragStart> = new EventEmitter<CdkDragStart>();
  @Output() onRelease:EventEmitter<CdkDragRelease> = new EventEmitter<CdkDragRelease>();

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

  getScheduledMovesBadge(): number  {
    if (this.isModifying) {
      return this.course.ScheduledMoves.length;
    }
    return this.course.ScheduledMoves.filter(move => move.IsConfirmed).length;
  }

  getBackground(): string {
    return this.course?.IsLocked
      ? ((!this.course?.IsLockedByAdmin)
        ? `repeating-linear-gradient(90deg, ${this.course?.Type?.Color}, ${this.course?.Type?.Color} 10px, #FFFFFF 10px, #FFFFFF 20px) left / 50% 100% no-repeat,
        ${this.course?.Type?.Color} right / 50% 100% no-repeat`
        : `repeating-linear-gradient(90deg, ${this.course?.Type?.Color}, ${this.course?.Type?.Color} 10px, #FFFFFF 10px, #FFFFFF 20px)`)
      : `${this.course?.Type?.Color}`;
  }

  ngOnInit(): void {
  }

  Click(event:MouseEvent) {
    this.onSelect.emit({courseEdition: this.course, isDisabled: this.cdkCourse.disabled});
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

  CheckIfAnyProposition():boolean {
    if (!this.isModifying) {
      return false;
    }
    return this.course.ScheduledMoves.some((scheduledMove) => !scheduledMove.IsConfirmed);
  }

  OnStarted(event:CdkDragStart) {
    this.onStart.emit(event);
  }

  OnReleased(event:CdkDragRelease) {
    this.onRelease.emit(event);
  }

  Floor(number:number):number {
    return Math.floor(number);
  }

  Ceil(number:number):number {
    return Math.ceil(number);
  }
}
