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
    /*if (!this.cdkCourse.disabled && event.ctrlKey) {
      this.ctrlClick.emit(this.course);
    }*/
  }

  CheckIfItsMe(id:number):boolean {
    return this.account.UserId == id;
  }

  CheckIfNotMatching():boolean {
    return this.weeksOnTab?.sort((a,b) => a - b).join(',') !== this.course.Weeks?.sort((a,b) => a - b).join(',');
  }

  OnStarted(event:CdkDragStart) {
    this.start.emit(event);
  }

  OnReleased(event:CdkDragRelease) {
    this.release.emit(event);
  }

  private checkFrequency(weeks:number[],termDurationWeeks:number,even:boolean):boolean {
    const length = weeks.length;
    const halfTermDurationWeeks = termDurationWeeks / 2;
    const requiredLength = (even) ? Math.floor(halfTermDurationWeeks) : Math.ceil(halfTermDurationWeeks);

    if (length != requiredLength) {
      return false;
    }

    for (let i = 0; i < length; ++i) {
      if (even && weeks[i] % 2 != 0) {
        return false;
      }
      if (!even && weeks[i] % 2 == 0) {
        return false;
      }
    }

    return true;
  }

  private frequencyToString(weeks:number[]|null):string {
    if (weeks == null) {
      return '';
    }
    let result = (weeks.length > 1) ? 'Tygodnie ' : 'Tydzień ';  
    weeks.sort((a, b) => a - b).forEach((week) => {
      result += week + ', ';
    });
    result = result.substring(0, result.length - 2);
    return result;
  }

  ShowFrequency():string {
    if (this.course.Weeks == null) {
      return '';
    }

    const courseWeeksLength = this.course.Weeks.length;
    if (courseWeeksLength == 0) {
      return '';
    }

    const termDurationWeeks = this.settings.TermDurationWeeks;

    if (courseWeeksLength == termDurationWeeks) {
      return 'Tygodniowo';
    }

    if (this.checkFrequency(this.course.Weeks, termDurationWeeks, true)) {
      return 'Tygodnie parzyste';
    }

    if (this.checkFrequency(this.course.Weeks, termDurationWeeks, false)) {
      return 'Tygodnie nieparzyste';
    }

    return this.frequencyToString(this.course.Weeks);
  }
}
