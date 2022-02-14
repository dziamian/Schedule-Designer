import { CdkDragDrop, CdkDragEnter, CdkDragRelease, CdkDragStart, DropListRef } from '@angular/cdk/drag-drop';
import { Component, OnInit, Output, EventEmitter, Input, ViewChild, SimpleChanges } from '@angular/core';
import { forkJoin, Subscription } from 'rxjs';
import { skip } from 'rxjs/operators';
import { UserInfo } from 'src/app/others/Accounts';
import { CourseEdition } from 'src/app/others/CourseEdition';
import { Filter } from 'src/app/others/Filter';
import { Group } from 'src/app/others/Group';
import { ModifyingScheduleData } from 'src/app/others/ModifyingScheduleData';
import { Settings } from 'src/app/others/Settings';
import { CourseType } from 'src/app/others/Types';
import { ScheduleDesignerApiService } from 'src/app/services/ScheduleDesignerApiService/schedule-designer-api.service';
import { SignalrService } from 'src/app/services/SignalrService/signalr.service';

/**
 * Komponent odpowiadający za ładowanie i wyświetlanie nieułożonych jeszcze zajęć w planie.
 */
@Component({
  selector: 'app-my-courses',
  templateUrl: './my-courses.component.html',
  styleUrls: ['./my-courses.component.css']
})
export class MyCoursesComponent implements OnInit {

  /** Strefa do upuszczania paneli z zajęciami w celu usunięcia ich z planu. */
  @ViewChild('myCoursesDrop') myCoursesSlot : DropListRef<CourseEdition[]>

  /** Określa czy tryb modyfikacji jest włączony. */
  @Input() isModifying: boolean;
  /** Informacje o zalogowanym użytkowniku. */
  @Input() userInfo: UserInfo;
  /** Ustawienia aplikacji. */
  @Input() settings: Settings;
  /** Kolekcja typów przedmiotów. */
  @Input() courseTypes: Map<number, CourseType>;
  /** Dane trybu modyfikacji. */
  @Input() modifyingScheduleData: ModifyingScheduleData;
  /** Aktualny filtr widoku (przechowuje informacje także o tym, czy widok powinien zostać przeładowany czy nie). */
  @Input() currentFilter: {weeks: number[], filter: Filter, tabSwitched: boolean};

  /** Emiter zdarzenia najechania panelu z zajęciami na strefę upuszczania. */
  @Output() onDropEnter: EventEmitter<CdkDragEnter> = new EventEmitter<CdkDragEnter>();
  /** Emiter zdarzenia interakcji upuszczenia panelu z zajęciami w strefie. */
  @Output() onDropped: EventEmitter<CdkDragDrop<CourseEdition[]>> = new EventEmitter<CdkDragDrop<CourseEdition[]>>();
  /** Emiter zdarzenia rozpoczęcia interakcji przeciągania panelu z zajęciami. */
  @Output() onStart: EventEmitter<CdkDragStart> = new EventEmitter<CdkDragStart>();
  /** Emiter zdarzenia zakończenia interakcji przeciągania panelu z zajęciami. */
  @Output() onRelease: EventEmitter<CdkDragRelease> = new EventEmitter<CdkDragRelease>();
  /** Emiter zdarzenia załadowania nieułożonych zajęć w planie. */
  @Output() onLoaded: EventEmitter<void> = new EventEmitter();

  loadingSubscription: Subscription;
  signalrSubscriptions: Subscription[];
  /** Informuje czy dane zostały załadowane. */
  loading: boolean | null = null;

  /** Liczba wyświetlanych paneli na stronę. */
  readonly TOP: number = 6;
  /** Liczba paneli do pominięcia. */
  skip: number = 0;
  /** Tablica nieułożonych edycji zajęć. */
  myCourses: CourseEdition[];

  constructor(
    private signalrService: SignalrService,
    private scheduleDesignerApiService:ScheduleDesignerApiService
  ) { }

  /**
   * Metoda aktualizująca widok, wyświetlając kolejną stronę nieułożonych edycji zajęć.
   */
  NextPage() {
    this.skip += this.TOP;
  }

  /**
   * Metoda aktualizująca widok, wyświetlając poprzednią stronę nieułożonych edycji zajęć.
   */
  PreviousPage() {
    this.skip -= this.TOP;
  }

  private updateLockInMyCourses(courseId:number, courseEditionId:number, locked: {value:boolean, byAdmin: boolean}) {
    if (!this.myCourses) {
      return;
    }

    this.myCourses.forEach((myCourse) => {
      if (myCourse.CourseId == courseId && myCourse.CourseEditionId == courseEditionId) {
        myCourse.IsLocked = locked.value;
        myCourse.IsLockedByAdmin = locked.byAdmin;
      }
    });
  }

  /**
   * Metoda rozpoczynająca odbieranie bieżących informacji dotyczących planu z centrum SignalR.
   */
  private setSignalrSubscriptions(): void {
    this.signalrSubscriptions = [];

    this.signalrSubscriptions.push(this.signalrService.lastLockedCourseEdition.pipe(skip(1)).subscribe(({courseId, courseEditionId, byAdmin}) => {
      this.updateLockInMyCourses(courseId, courseEditionId, {value: true, byAdmin: byAdmin});
    }));

    this.signalrSubscriptions.push(this.signalrService.lastUnlockedCourseEdition.pipe(skip(1)).subscribe(({courseId, courseEditionId}) => {
      this.updateLockInMyCourses(courseId, courseEditionId, {value: false, byAdmin: false});
    }));

    this.signalrSubscriptions.push(this.signalrService.lastAddedSchedulePositions.pipe(skip(1)).subscribe((addedSchedulePositions) => {
      if (this.loading || this.loading == null) {
        return;
      }

      const schedulePosition = addedSchedulePositions.SchedulePosition;
      const addedAmount = schedulePosition.Weeks.length;

      //filter for updated board
      const filter = new Filter(addedSchedulePositions.CoordinatorsIds, addedSchedulePositions.GroupsIds, [
        addedSchedulePositions.SchedulePosition.RoomId
      ]);
      if (this.currentFilter.filter.challengeAll(filter)) {
        const courses = this.myCourses.filter((course) => 
          course.CourseId == schedulePosition.CourseId 
          && course.CourseEditionId == schedulePosition.CourseEditionId 
        );

        if (courses.length != 0) {
          const firstCourse = courses[0];
          
          let currentScheduleAmount = firstCourse.ScheduleAmount + addedAmount;

          this.myCourses = this.myCourses.filter((course) => {
            if (course.CourseId != schedulePosition.CourseId 
              || course.CourseEditionId != schedulePosition.CourseEditionId) {
              return true;
            }
            
            if (currentScheduleAmount + course.CurrentAmount <= Math.ceil(course.FullAmount)) {
              course.ScheduleAmount += addedAmount;
              currentScheduleAmount += course.CurrentAmount;
              return true;
            }
            
            return false;
          });
        }
      }
    }));

    this.signalrSubscriptions.push(this.signalrService.lastRemovedSchedulePositions.pipe(skip(1)).subscribe((removedSchedulePositions) => {
      if (this.loading || this.loading == null) {
        return;
      }

      const schedulePosition = removedSchedulePositions.SchedulePosition;
      const removedAmount = schedulePosition.Weeks.length;

      //filter for updated board
      const filter = new Filter(removedSchedulePositions.CoordinatorsIds, removedSchedulePositions.GroupsIds, [
        removedSchedulePositions.SchedulePosition.RoomId
      ]);
      if (this.currentFilter.filter.challengeAll(filter)) {
        const courses = this.myCourses.filter((course) => {
          if (course.CourseId == schedulePosition.CourseId 
            && course.CourseEditionId == schedulePosition.CourseEditionId) {
              course.ScheduleAmount -= removedAmount;
              return true;
          }
          return false;
        });

        if (courses.length > 0) {
          const firstCourse = courses[0];
          
          const currentAmount = firstCourse.CurrentAmount;
          const currentScheduleAmount = firstCourse.ScheduleAmount + currentAmount * courses.length;
          const fullAmount = Math.ceil(firstCourse.FullAmount);

          const coursesAmountAdded = Math.floor((fullAmount - currentScheduleAmount) / currentAmount);
          let newCourses:CourseEdition[] = []; 
          for (let i = 0; i < coursesAmountAdded; ++i) {
            const courseEdition = new CourseEdition(
              firstCourse.CourseId, firstCourse.CourseEditionId,
              firstCourse.Name, firstCourse.Type,
              firstCourse.CurrentAmount, firstCourse.Groups,
              firstCourse.Coordinators
            );
            courseEdition.IsLocked = firstCourse.IsLocked;
            courseEdition.IsLockedByAdmin = firstCourse.IsLockedByAdmin;
            courseEdition.ScheduleAmount = firstCourse.ScheduleAmount;
            courseEdition.FullAmount = firstCourse.FullAmount;
            newCourses.push(courseEdition);
          }
          this.myCourses.splice(0,0,...newCourses);
        } else {
          const mainGroupsIds = removedSchedulePositions.GroupsIds.slice(
            0, removedSchedulePositions.MainGroupsAmount
          );

          forkJoin([
            this.scheduleDesignerApiService.GetFilteredCourseEdition(
              schedulePosition.CourseId, schedulePosition.CourseEditionId,
              this.currentFilter.weeks.length, this.currentFilter.filter, 
              this.courseTypes, this.settings
            ),
            this.scheduleDesignerApiService.GetGroupsFullNames(mainGroupsIds)
          ]).subscribe(([myNewCourses, groupFullNames]) => {
            if (myNewCourses.length > 0) {
              myNewCourses.forEach((myNewCourse) => {
                for (let i = 0; i < groupFullNames.length; ++i) {
                  myNewCourse.Groups[i].FullName = groupFullNames[i];
                }
              });
              this.myCourses.splice(0,0,...myNewCourses);
            }
          }, () => {});
        }
      }
    }));
  }

  /**
   * Metoda przygotowująca komponent.
   * Rozpoczyna odbieranie bieżących informacji z centrum SignalR.
   * Ładuje edycje zajęć po raz pierwszy.
   */
  ngOnInit(): void {
    this.setSignalrSubscriptions();
    if (this.currentFilter.weeks.length > 0 && this.currentFilter.filter) {
      this.loadMyCourses();
    }
  }

  /**
   * Metoda wywoływana w momencie wystąpienia zmian na wejściu komponentu.
   * W przypadku zmian dotyczących filtra edycji zajęć powoduje to przeładowanie ich danych.
   * @param changes Zachodzące zmiany na wejściu komponentu
   */
  ngOnChanges(changes: SimpleChanges): void {
    if (changes.currentFilter && changes.currentFilter.currentValue && !changes.currentFilter.currentValue.tabSwitched 
      && changes.currentFilter.currentValue.weeks.length > 0 && changes.currentFilter.currentValue.filter) {
        if (changes.currentFilter.isFirstChange()) {
          return;
        }
        
        const currentWeeks: number[] = changes.currentFilter.currentValue.weeks;
        const previousWeeks: number[] = changes.currentFilter.previousValue.weeks ?? [];
        const currentFilter: Filter = changes.currentFilter.currentValue.filter;
        const previousFilter: Filter = changes.currentFilter.previousValue.filter;

        if (currentWeeks.sort((a,b) => a - b).join(',') 
          !== previousWeeks.sort((a,b) => a - b).join(',')
          || !currentFilter.compare(previousFilter)) {
            this.loadMyCourses();
        }
    }
  }

  /**
   * Metoda ładująca nieułożone edycje zajęć z serwera.
   */
  private loadMyCourses(): void {
    this.loadingSubscription?.unsubscribe();
    this.loading = true;

    this.loadingSubscription = this.scheduleDesignerApiService.GetFilteredCourseEditions(this.currentFilter.weeks.length, this.currentFilter.filter, this.courseTypes, this.settings).subscribe((myCourses) => {
      this.myCourses = myCourses;

      if (myCourses.length == 0) {
        this.loading = false;
        this.onLoaded.emit();
        return;
      }

      myCourses.sort((a,b) => a.CourseEditionId - b.CourseEditionId);

      forkJoin([
        this.scheduleDesignerApiService.GetCoordinatorsForCourses(myCourses),
        this.scheduleDesignerApiService.GetGroupsForCourses(myCourses),
      ]).subscribe(() => {
        var allGroups = new Array<Group>();
        for (let i = 0; i < this.myCourses.length; ++i) {
          for (let j = 0; j < this.myCourses[i].Groups.length; ++j) {
            allGroups.push(this.myCourses[i].Groups[j]);
          }
        }

        this.scheduleDesignerApiService.GetGroupsFullNames(allGroups.map((e) => e.GroupId)).subscribe((groupsFullNames) => {
          for (let i = 0; i < groupsFullNames.length; ++i) {
            allGroups[i].FullName = groupsFullNames[i];
          }
          
          this.loading = false;
          this.onLoaded.emit();
        });
      });
    });
  }

  OnDropEnter(event: CdkDragEnter) {
    this.onDropEnter.emit(event);
  }

  OnDropped(event: CdkDragDrop<CourseEdition[]>) {
    this.onDropped.emit(event);
  }

  OnStarted(event: CdkDragStart) {
    this.onStart.emit(event);
  }

  OnReleased(event: CdkDragRelease) {
    this.onRelease.emit(event);
  }

  ngOnDestroy() {
    this.signalrSubscriptions.forEach(
      subscription => subscription.unsubscribe()
    );
  }
}
