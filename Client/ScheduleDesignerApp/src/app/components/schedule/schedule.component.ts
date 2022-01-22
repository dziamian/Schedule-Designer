import { CdkDrag, CdkDragDrop, CdkDragEnter, CdkDragRelease, CdkDragStart, CdkDropList, DropListRef } from '@angular/cdk/drag-drop';
import { Component, EventEmitter, Input, OnInit, Output, QueryList, SimpleChanges, ViewChildren } from '@angular/core';
import { forkJoin, Subscription } from 'rxjs';
import { skip } from 'rxjs/operators';
import { Account } from 'src/app/others/Accounts';
import { SchedulePosition } from 'src/app/others/CommunicationObjects';
import { CourseEdition } from 'src/app/others/CourseEdition';
import { Filter } from 'src/app/others/Filter';
import { Group } from 'src/app/others/Group';
import { ModifyingScheduleData } from 'src/app/others/ModifyingScheduleData';
import { Room } from 'src/app/others/Room';
import { Settings } from 'src/app/others/Settings';
import { CourseType } from 'src/app/others/Types';
import { ScheduleDesignerApiService } from 'src/app/services/ScheduleDesignerApiService/schedule-designer-api.service';
import { SignalrService } from 'src/app/services/SignalrService/signalr.service';

@Component({
  selector: 'app-schedule',
  templateUrl: './schedule.component.html',
  styleUrls: ['./schedule.component.css']
})
export class ScheduleComponent implements OnInit {

  @ViewChildren('scheduleDrops') scheduleSlots: QueryList<DropListRef<CourseEdition[]>>;

  @Input() labelBefore: string;
  @Input() labelAfter: string;

  @Input() isModifying: boolean;
  @Input() account: Account;
  @Input() representativeGroupsIds:number[] = [];
  @Input() settings: Settings;
  @Input() courseTypes: Map<number, CourseType>;
  @Input() modifyingScheduleData: ModifyingScheduleData;
  @Input() currentFilter: {weeks: number[], filter: Filter, tabSwitched: boolean, editable: boolean};

  @Output() onStart: EventEmitter<CdkDragStart> = new EventEmitter<CdkDragStart>();
  @Output() onRelease: EventEmitter<CdkDragRelease> = new EventEmitter<CdkDragRelease>();
  @Output() onCourseSelect: EventEmitter<{
    courseEdition: CourseEdition, isDisabled: boolean, day: number, periodIndex: number
  }> = new EventEmitter();
  @Output() onDropSelect: EventEmitter<{
    day:number, periodIndex:number
  }> = new EventEmitter();
  @Output() onDrop: EventEmitter<CdkDragDrop<CourseEdition[]>> = new EventEmitter<CdkDragDrop<CourseEdition[]>>();
  @Output() onDragEnter: EventEmitter<CdkDragEnter> = new EventEmitter<CdkDragEnter>();
  @Output() onMouseEnter: EventEmitter<{
    day:number, periodIndex:number
  }> = new EventEmitter();
  @Output() onMouseLeave: EventEmitter<null> = new EventEmitter();
  @Output() onLoaded: EventEmitter<null> = new EventEmitter();
  @Output() onViewEdit: EventEmitter<null> = new EventEmitter();

  loadingSubscription: Subscription;
  signalrSubscriptions: Subscription[];
  loading: boolean | null = null;

  schedule: CourseEdition[][][];

  constructor(
    private signalrService: SignalrService,
    private scheduleDesignerApiService:ScheduleDesignerApiService
  ) { }

  private updateLockInSchedule(position:SchedulePosition) {
    if (!this.schedule) {
      return;
    }

    const courseId = position.CourseId;
    const courseEditionId = position.CourseEditionId;
    const roomId = position.RoomId;
    const day = position.Day - 1;
    const periodIndex = position.PeriodIndex - 1;
    const weeks = position.Weeks;
    const lockedValue = position.IsLocked;
    const lockedByAdmin = position.IsLockedByAdmin;

    if (!this.currentFilter.weeks.some(r => weeks.includes(r))) {
      return;
    }

    let courseEditions = this.schedule[day][periodIndex];
    courseEditions.forEach((courseEdition) => {
      if (courseEdition.CourseId == courseId && courseEdition.CourseEditionId == courseEditionId 
        && courseEdition.Room?.RoomId == roomId) {
          if (!lockedValue) {
            this.scheduleDesignerApiService.AreSchedulePositionsLocked(
              position.RoomId, position.PeriodIndex,
              position.Day, courseEdition.Weeks!
            ).subscribe((areLocked) => {
              courseEdition.IsLocked = areLocked.value;
              courseEdition.IsLockedByAdmin = areLocked.byAdmin;
            });
          } else {
            courseEdition.IsLocked = lockedValue;
            courseEdition.IsLockedByAdmin = lockedByAdmin;
          }
      }
    });
  }

  private setSignalrSubscriptions(): void {
    this.signalrSubscriptions = [];

    this.signalrSubscriptions.push(this.signalrService.lastLockedSchedulePositions.pipe(skip(1)).subscribe((lockedSchedulePositions) => {
      this.updateLockInSchedule(lockedSchedulePositions);
    }));

    this.signalrSubscriptions.push(this.signalrService.lastUnlockedSchedulePositions.pipe(skip(1)).subscribe((unlockedSchedulePositions) => {
      this.updateLockInSchedule(unlockedSchedulePositions);
    }));

    this.signalrSubscriptions.push(this.signalrService.lastAddedSchedulePositions.pipe(skip(1)).subscribe((addedSchedulePositions) => {
      if (this.loading || this.loading == null) {
        return;
      }

      const schedulePosition = addedSchedulePositions.SchedulePosition;
      const commonWeeks = schedulePosition.Weeks.filter(week => this.currentFilter.weeks.includes(week));

      //filter for updated board
      const filter = new Filter(addedSchedulePositions.CoordinatorsIds, addedSchedulePositions.GroupsIds, [
        addedSchedulePositions.SchedulePosition.RoomId
      ]);
      if (this.currentFilter.filter.challengeAll(filter)) {
        if (commonWeeks.length > 0) {
          const existingCourseEditions = this.schedule[schedulePosition.Day - 1][schedulePosition.PeriodIndex - 1].filter((courseEdition) => 
            courseEdition.CourseId == schedulePosition.CourseId 
              && courseEdition.CourseEditionId == schedulePosition.CourseEditionId
              && courseEdition.Room!.RoomId == schedulePosition.RoomId
          );

          if (existingCourseEditions.length > 0) {
            existingCourseEditions[0].Weeks?.push(
              ...commonWeeks
            );
          } else {
            const mainGroupsIds = addedSchedulePositions.GroupsIds.slice(
              0, addedSchedulePositions.MainGroupsAmount
            );
    
            forkJoin([
              this.scheduleDesignerApiService.GetCourseEditionInfo(
                schedulePosition.CourseId, schedulePosition.CourseEditionId, this.settings),
              this.scheduleDesignerApiService.GetGroupsFullNames(mainGroupsIds),
              this.scheduleDesignerApiService.GetCoordinatorsFromUsers(addedSchedulePositions.CoordinatorsIds),
              this.scheduleDesignerApiService.GetRoomsNames([schedulePosition.RoomId])
            ]).subscribe(([courseEditionInfo, groupsNames, coordinators, roomNames]) => {
              let groups:Group[] = [];
              for (let i = 0; i < mainGroupsIds.length; ++i) {
                const group = new Group(mainGroupsIds[i]);
                group.FullName = groupsNames[i];
                groups.push(group);
              }
              const room = new Room(schedulePosition.RoomId);
              room.Name = roomNames[0];
              
              const addedCourseEdition = new CourseEdition(
                schedulePosition.CourseId, schedulePosition.CourseEditionId,
                courseEditionInfo.Name, this.courseTypes.get(courseEditionInfo.CourseTypeId)!,
                0, groups, coordinators
              );
            
              addedCourseEdition.Room = room;
              addedCourseEdition.Weeks = commonWeeks;
              addedCourseEdition.ScheduleAmount = courseEditionInfo.ScheduleAmount;
              addedCourseEdition.FullAmount = courseEditionInfo.FullAmount;
    
              this.schedule[schedulePosition.Day - 1][schedulePosition.PeriodIndex - 1].push(addedCourseEdition);
            });
          }
        }
      }
    }));

    this.signalrSubscriptions.push(this.signalrService.lastModifiedSchedulePositions.pipe(skip(1)).subscribe((modifiedSchedulePositions) => {
      if (this.loading || this.loading == null) {
        return;
      }

      const srcSchedulePosition = modifiedSchedulePositions.SourceSchedulePosition;
      const dstSchedulePosition = modifiedSchedulePositions.DestinationSchedulePosition;
      const commonWeeks = dstSchedulePosition.Weeks.filter(week => this.currentFilter.weeks.includes(week));
      const movesIds = modifiedSchedulePositions.MovesIds;

      //filter for updated board
      const filter = new Filter(modifiedSchedulePositions.CoordinatorsIds, modifiedSchedulePositions.GroupsIds, [
        srcSchedulePosition.RoomId, dstSchedulePosition.RoomId
      ]);
      if (this.currentFilter.filter.challengeAll(filter)) {
        let srcScheduleSlot = this.schedule[srcSchedulePosition.Day - 1][srcSchedulePosition.PeriodIndex - 1];
        let dstScheduleSlot = this.schedule[dstSchedulePosition.Day - 1][dstSchedulePosition.PeriodIndex - 1];

        const srcRoomChallenge = this.currentFilter.filter.challengeRoom(srcSchedulePosition.RoomId);
        const dstRoomChallenge = this.currentFilter.filter.challengeRoom(dstSchedulePosition.RoomId);

        if (srcRoomChallenge && dstRoomChallenge
          && srcSchedulePosition.PeriodIndex == dstSchedulePosition.PeriodIndex && srcSchedulePosition.Day == dstSchedulePosition.Day
          && commonWeeks.length > 0) {
          //update old if only room changed or weeks
          const existingCourseEditions = this.schedule[srcSchedulePosition.Day - 1][srcSchedulePosition.PeriodIndex - 1].filter((courseEdition) => 
            courseEdition.CourseId == srcSchedulePosition.CourseId 
              && courseEdition.CourseEditionId == srcSchedulePosition.CourseEditionId
              && courseEdition.Room!.RoomId == srcSchedulePosition.RoomId
          );
          if (existingCourseEditions.length > 0) {
            if (srcSchedulePosition.Weeks.sort((a,b) => a - b).join(',') !== dstSchedulePosition.Weeks.sort((a,b) => a - b).join(',')) {
              existingCourseEditions[0].Weeks = commonWeeks;
            }
            if (srcSchedulePosition.RoomId != dstSchedulePosition.RoomId) {
              this.scheduleDesignerApiService.GetRoomsNames([dstSchedulePosition.RoomId]).subscribe(roomName => {
                const room = new Room(dstSchedulePosition.RoomId);
                room.Name = roomName[0];
                existingCourseEditions[0].Room = room;
                existingCourseEditions[0].IsLocked = false;
                existingCourseEditions[0].IsLockedByAdmin = false;
              });
            }
            this.scheduleDesignerApiService.AreSchedulePositionsLocked(
              dstSchedulePosition.RoomId, dstSchedulePosition.PeriodIndex,
              dstSchedulePosition.Day, existingCourseEditions[0].Weeks!
            ).subscribe((areLocked) => {
              existingCourseEditions[0].IsLocked = areLocked.value;
              existingCourseEditions[0].IsLockedByAdmin = areLocked.byAdmin;
            });
            
            existingCourseEditions[0].ScheduledMoves = existingCourseEditions[0].ScheduledMoves
              .filter((scheduledMove) => !movesIds.includes(scheduledMove.MoveId));
          }
        } else {
          //remove or update old
          const existingSrcCourseEditions = this.schedule[srcSchedulePosition.Day - 1][srcSchedulePosition.PeriodIndex - 1].filter((courseEdition) => 
            courseEdition.CourseId == srcSchedulePosition.CourseId 
              && courseEdition.CourseEditionId == srcSchedulePosition.CourseEditionId
              && courseEdition.Room!.RoomId == srcSchedulePosition.RoomId
          );
          
          if (srcRoomChallenge && existingSrcCourseEditions.length > 0) {
            existingSrcCourseEditions[0].Weeks = existingSrcCourseEditions[0].Weeks
              ?.filter(week => !srcSchedulePosition.Weeks.includes(week)) ?? [];
            
            if (existingSrcCourseEditions[0].Weeks.length == 0) {
              this.schedule[srcSchedulePosition.Day - 1][srcSchedulePosition.PeriodIndex - 1] 
                = srcScheduleSlot.filter(courseEdition => courseEdition.Weeks != null 
                  && courseEdition.Weeks.length > 0);
            } else {
              existingSrcCourseEditions[0].ScheduledMoves = existingSrcCourseEditions[0].ScheduledMoves
                .filter((scheduledMove) => !movesIds.includes(scheduledMove.MoveId));

              this.scheduleDesignerApiService.AreSchedulePositionsLocked(
                srcSchedulePosition.RoomId, srcSchedulePosition.PeriodIndex,
                srcSchedulePosition.Day, existingSrcCourseEditions[0].Weeks
              ).subscribe((areLocked) => {
                existingSrcCourseEditions[0].IsLocked = areLocked.value;
                existingSrcCourseEditions[0].IsLockedByAdmin = areLocked.byAdmin;
              });
            }
          }

          //add or update new
          if (dstRoomChallenge && commonWeeks.length > 0) {
            const existingDstCourseEditions = dstScheduleSlot.filter((courseEdition) => 
              courseEdition.CourseId == dstSchedulePosition.CourseId 
                && courseEdition.CourseEditionId == dstSchedulePosition.CourseEditionId
                && courseEdition.Room!.RoomId == dstSchedulePosition.RoomId
            );

            if (existingDstCourseEditions.length > 0) {
              if (existingDstCourseEditions[0].Weeks?.some(week => commonWeeks.includes(week))) {
                const addedCourseEdition = new CourseEdition(
                  existingDstCourseEditions[0].CourseId, existingDstCourseEditions[0].CourseEditionId,
                  existingDstCourseEditions[0].Name, existingDstCourseEditions[0].Type,
                  0, existingDstCourseEditions[0].Groups, existingDstCourseEditions[0].Coordinators
                );
                addedCourseEdition.Room = existingDstCourseEditions[0].Room;
                addedCourseEdition.Weeks = commonWeeks;
                addedCourseEdition.ScheduleAmount = existingDstCourseEditions[0].ScheduleAmount;
                addedCourseEdition.FullAmount = existingDstCourseEditions[0].FullAmount;

                this.schedule[dstSchedulePosition.Day - 1][dstSchedulePosition.PeriodIndex - 1].push(addedCourseEdition);
              } else {
                existingDstCourseEditions[0].Weeks?.push(...commonWeeks);
                
                this.scheduleDesignerApiService.AreSchedulePositionsLocked(
                  dstSchedulePosition.RoomId, dstSchedulePosition.PeriodIndex,
                  dstSchedulePosition.Day, existingDstCourseEditions[0].Weeks!
                ).subscribe((areLocked) => {
                  existingSrcCourseEditions[0].IsLocked = areLocked.value;
                  existingSrcCourseEditions[0].IsLockedByAdmin = areLocked.byAdmin;
                });
              }
            } else {
              const mainGroupsIds = modifiedSchedulePositions.GroupsIds.slice(
                0, modifiedSchedulePositions.MainGroupsAmount
              );

              forkJoin([
                this.scheduleDesignerApiService.GetCourseEditionInfo(
                  dstSchedulePosition.CourseId, dstSchedulePosition.CourseEditionId, this.settings),
                this.scheduleDesignerApiService.GetGroupsFullNames(mainGroupsIds),
                this.scheduleDesignerApiService.GetCoordinatorsFromUsers(modifiedSchedulePositions.CoordinatorsIds),
                this.scheduleDesignerApiService.GetRoomsNames([dstSchedulePosition.RoomId])
              ]).subscribe(([courseEditionInfo, groupsNames, coordinators, roomNames]) => {
                let groups:Group[] = [];
                for (let i = 0; i < mainGroupsIds.length; ++i) {
                  const group = new Group(mainGroupsIds[i]);
                  group.FullName = groupsNames[i];
                  groups.push(group);
                }
                const room = new Room(dstSchedulePosition.RoomId);
                room.Name = roomNames[0];
                
                const addedCourseEdition = new CourseEdition(
                  dstSchedulePosition.CourseId, dstSchedulePosition.CourseEditionId,
                  courseEditionInfo.Name, this.courseTypes.get(courseEditionInfo.CourseTypeId)!,
                  0, groups, coordinators
                );
      
                addedCourseEdition.Room = room;
                addedCourseEdition.Weeks = commonWeeks;
                addedCourseEdition.ScheduleAmount = courseEditionInfo.ScheduleAmount;
                addedCourseEdition.FullAmount = courseEditionInfo.FullAmount;
      
                dstScheduleSlot.push(addedCourseEdition);
              });
            }
          }
        }
      }
    }));

    this.signalrSubscriptions.push(this.signalrService.lastRemovedSchedulePositions.pipe(skip(1)).subscribe((removedSchedulePositions) => {
      if (this.loading || this.loading == null) {
        return;
      }

      const schedulePosition = removedSchedulePositions.SchedulePosition;
      const movesIds = removedSchedulePositions.MovesIds;

      //filter for updated board
      const filter = new Filter(removedSchedulePositions.CoordinatorsIds, removedSchedulePositions.GroupsIds, [
        removedSchedulePositions.SchedulePosition.RoomId
      ]);
      if (this.currentFilter.filter.challengeAll(filter)) {
        let scheduleSlot = this.schedule[schedulePosition.Day - 1][schedulePosition.PeriodIndex - 1];
        const existingCourseEditions = this.schedule[schedulePosition.Day - 1][schedulePosition.PeriodIndex - 1].filter((courseEdition) => 
          courseEdition.CourseId == schedulePosition.CourseId 
            && courseEdition.CourseEditionId == schedulePosition.CourseEditionId
            && courseEdition.Room!.RoomId == schedulePosition.RoomId
        );
        
        if (existingCourseEditions.length > 0) {
          existingCourseEditions[0].Weeks = existingCourseEditions[0].Weeks
            ?.filter(week => !schedulePosition.Weeks.includes(week)) ?? [];
          
          if (existingCourseEditions[0].Weeks.length == 0) {
            this.schedule[schedulePosition.Day - 1][schedulePosition.PeriodIndex - 1] 
              = scheduleSlot.filter(courseEdition => courseEdition.Weeks != null 
                && courseEdition.Weeks.length > 0);
          } else {
            existingCourseEditions[0].ScheduledMoves = existingCourseEditions[0].ScheduledMoves
              .filter((scheduledMove) => !movesIds.includes(scheduledMove.MoveId));

            this.scheduleDesignerApiService.AreSchedulePositionsLocked(
              schedulePosition.RoomId, schedulePosition.PeriodIndex,
              schedulePosition.Day, existingCourseEditions[0].Weeks
            ).subscribe((areLocked) => {
              existingCourseEditions[0].IsLocked = areLocked.value;
              existingCourseEditions[0].IsLockedByAdmin = areLocked.byAdmin;
            });
          }
        }
      }
    }));

    this.signalrSubscriptions.push(this.signalrService.lastAddedScheduledMove.pipe(skip(1)).subscribe((addedScheduledMove) => {
      if (this.loading || this.loading == null) {
        return;
      }
      
      const srcSchedulePosition = addedScheduledMove.sourceSchedulePosition;
      const commonWeeks = srcSchedulePosition.Weeks.filter(week => this.currentFilter.weeks.includes(week));

      if (commonWeeks.length == 0) {
        return;
      }

      const existingCourseEditions = this.schedule[srcSchedulePosition.Day - 1][srcSchedulePosition.PeriodIndex - 1]
        .filter((courseEdition) => 
            courseEdition.CourseId == srcSchedulePosition.CourseId 
              && courseEdition.CourseEditionId == srcSchedulePosition.CourseEditionId
              && courseEdition.Room!.RoomId == srcSchedulePosition.RoomId
      );

      if (existingCourseEditions.length == 0) {
        return;
      }

      existingCourseEditions[0].ScheduledMoves.push(addedScheduledMove.scheduledMove);
    }));

    this.signalrSubscriptions.push(this.signalrService.lastRemovedScheduledMove.pipe(skip(1)).subscribe((removedScheduledMove) => {
      if (this.loading || this.loading == null) {
        return;
      }

      const srcSchedulePosition = removedScheduledMove.sourceSchedulePosition;
      const commonWeeks = srcSchedulePosition.Weeks.filter(week => this.currentFilter.weeks.includes(week));

      if (commonWeeks.length == 0) {
        return;
      }

      const existingCourseEditions = this.schedule[srcSchedulePosition.Day - 1][srcSchedulePosition.PeriodIndex - 1]
        .filter((courseEdition) => 
            courseEdition.CourseId == srcSchedulePosition.CourseId 
              && courseEdition.CourseEditionId == srcSchedulePosition.CourseEditionId
              && courseEdition.Room!.RoomId == srcSchedulePosition.RoomId
      );

      if (existingCourseEditions.length == 0) {
        return;
      }

      existingCourseEditions[0].ScheduledMoves = existingCourseEditions[0].ScheduledMoves
        .filter((scheduledMove) => scheduledMove.MoveId != removedScheduledMove.moveId);
    }));

    this.signalrSubscriptions.push(this.signalrService.lastAcceptedScheduledMove.pipe(skip(1)).subscribe((acceptedScheduledMove) => {
      if (this.loading || this.loading == null) {
        return;
      }

      const srcSchedulePosition = acceptedScheduledMove.sourceSchedulePosition;
      const commonWeeks = srcSchedulePosition.Weeks.filter(week => this.currentFilter.weeks.includes(week));

      if (commonWeeks.length == 0) {
        return;
      }

      const existingCourseEditions = this.schedule[srcSchedulePosition.Day - 1][srcSchedulePosition.PeriodIndex - 1]
        .filter((courseEdition) => 
            courseEdition.CourseId == srcSchedulePosition.CourseId 
              && courseEdition.CourseEditionId == srcSchedulePosition.CourseEditionId
              && courseEdition.Room!.RoomId == srcSchedulePosition.RoomId
      );

      if (existingCourseEditions.length == 0) {
        return;
      }

      existingCourseEditions[0].ScheduledMoves = existingCourseEditions[0].ScheduledMoves
        .filter((scheduledMove) => {
          if (scheduledMove.MoveId == acceptedScheduledMove.moveId) {
            scheduledMove.IsConfirmed = true;
          }
          return true;
        });
    }));
  }

  ngOnInit(): void {
    this.setSignalrSubscriptions();
    if (this.currentFilter?.weeks.length > 0 && this.currentFilter.filter) {
      this.loadMySchedule();
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes.currentFilter && changes.currentFilter.currentValue && !changes.currentFilter.currentValue.tabSwitched 
      && changes.currentFilter.currentValue.weeks.length > 0 && changes.currentFilter.currentValue.filter) {
        if (changes.currentFilter.isFirstChange()) {
          return;
        }

        const currentWeeks: number[] = changes.currentFilter.currentValue.weeks;
        const previousWeeks: number[] = changes.currentFilter.previousValue?.weeks ?? [];
        const currentFilter: Filter = changes.currentFilter.currentValue.filter;
        const previousFilter: Filter = changes.currentFilter.previousValue?.filter ?? new Filter([], [], []);

        if (currentWeeks.sort((a,b) => a - b).join(',') 
          !== previousWeeks.sort((a,b) => a - b).join(',')
          || !currentFilter.compare(previousFilter)) {
            this.loadMySchedule();
        }
    }
  }

  private loadMySchedule() {
    this.loadingSubscription?.unsubscribe();
    this.loading = true;

    this.loadingSubscription = this.scheduleDesignerApiService.GetFilteredSchedule(this.currentFilter.weeks, this.currentFilter.filter, this.settings)
      .subscribe(({schedule: schedule, courseEditions: courseEditions}) => {
        this.schedule = schedule;

        if (courseEditions.length == 0) {
          this.loading = false;
          this.onLoaded.emit();
          return;
        }

        courseEditions.sort((a,b) => a.CourseId - b.CourseId);
        forkJoin([
          this.scheduleDesignerApiService.GetCoursesForSchedule(courseEditions, this.courseTypes, this.settings),
          this.scheduleDesignerApiService.GetRoomsNames(courseEditions.map((e) => e.Room?.RoomId!))
        ]).subscribe(([,roomsNames]) => {
          for (let i = 0; i < courseEditions.length; ++i) {
            courseEditions[i].Room!.Name = roomsNames[i];
          }

          courseEditions.sort((a,b) => a.CourseEditionId - b.CourseEditionId);
          forkJoin([
            this.scheduleDesignerApiService.GetScheduleAmount(courseEditions),
            this.scheduleDesignerApiService.GetCoordinatorsForCourses(courseEditions),
            this.scheduleDesignerApiService.GetGroupsForCourses(courseEditions),
          ]).subscribe(() => {
            var allGroups = new Array<Group>();
            for (let i = 0; i < courseEditions.length; ++i) {
              for (let j = 0; j < courseEditions[i].Groups.length; ++j) {
                allGroups.push(courseEditions[i].Groups[j]);
              }
            }

            this.scheduleDesignerApiService.GetGroupsFullNames(allGroups.map((e) => e.GroupId))
              .subscribe((groupsFullNames) => {
                for (let i = 0; i < groupsFullNames.length; ++i) {
                  allGroups[i].FullName = groupsFullNames[i];
                }

                this.loading = false;
                this.onLoaded.emit();
              });
          });
        });
    });
  }

  public PrintSchedule(): void {
    let content = document.getElementById('chosen-schedule')?.innerHTML;
    let popupWindow = window.open('', '_blank', 'top=0,left=0,height=100%,width=auto');
    if (popupWindow == null) {
      return;
    }

    popupWindow.document.open();
    popupWindow.document.write(`
      <html>
        <head>
          <title>${this.labelBefore ?? ''} Schedule ${this.labelAfter ?? ''} ${this.GetViewDescription()}</title>
          <style>
          .schedule-course-field:last-child:not(:only-child) {
            border-bottom: none;
          }
          .schedule-header {
            display: none;
          }
          .schedule-content {
            box-sizing: border-box;
          }
          .schedule-content table {
            border: 1px solid #000000;
          }
          .schedule-content table td, .schedule-content table th {
            text-align: center;
            border: 1px solid #000000;
          }
          .schedule-content table td[course="true"] {
            vertical-align: top;
          }
          .course {
            padding: 2px;
            position: relative;
            text-align: center;
            font-size: 12px;
            font-family: "Rubik";
            box-sizing: border-box;
            line-height: 1.3;
          }
          .course-weeks {
            display: inline;
          }
          .pin {
            display: none;
          }
          .course .mat-badge-medium.mat-badge-above.mat-badge-before .mat-badge-content {
            display: none;
          }
          .course.mat-badge-medium.mat-badge-above.mat-badge-after .mat-badge-content {
            display: none;
          }
          .course.mat-badge-medium.mat-badge-above.mat-badge-after.mat-badge-accent[proposition="true"] .mat-badge-content {
            display: none;
          }
          </style>
        </head>
        <body onload="window.print();">
          ${content ?? ''}
        </body>
      </html>`
    );
    popupWindow.document.close();
  }

  async EditView(): Promise<void> {
    this.onViewEdit.emit();
  }

  GetViewDescription(): string {
    if (this.currentFilter?.weeks) {
      const weeks = CourseEdition.ShowWeeks(this.settings, this.currentFilter.weeks);
      return weeks == '' ? '' : `(${CourseEdition.ShowWeeks(this.settings, this.currentFilter.weeks)})`;
    }
    return '';
  }

  GetMaxElementIndexOnDay(dayIndex: number): number {
    let dayScheduleLength:number[] = [];
    for (let i = 0; i < this.settings.Periods.length - 1; ++i) {
      dayScheduleLength.push(this.schedule[dayIndex][i].length);
    }
    return Math.max(...dayScheduleLength) - 1;
  }

  IsDropDisabled(dayIndex: number, slotIndex: number): boolean {
    return this.schedule[dayIndex][slotIndex].length > 1;
  }

  OnDropped(event:CdkDragDrop<CourseEdition[]>) {
    this.onDrop.emit(event);
  }

  OnDragEnter(event:CdkDragEnter<CourseEdition[]>) {
    this.onDragEnter.emit(event);
  }

  OnStarted(event: CdkDragStart): void {
    this.onStart.emit(event);
  }

  OnMouseEnter(day: number, periodIndex: number) {
    this.onMouseEnter.emit({
      day: day,
      periodIndex: periodIndex
    });
  }

  OnReleased(event: CdkDragRelease): void {
    this.onRelease.emit(event);
  }

  OnMouseLeave(): void {
    this.onMouseLeave.emit();
  }

  OnCourseSelect(event: {courseEdition: CourseEdition, isDisabled: boolean}, day: number, periodIndex: number): void {
    this.onCourseSelect.emit({
      courseEdition: event.courseEdition,
      isDisabled: event.isDisabled,
      day: day,
      periodIndex: periodIndex 
    });
  }

  OnDropSelect(day: number, periodIndex: number): void {
    this.onDropSelect.emit({
      day: day,
      periodIndex: periodIndex
    });
  }

  ngOnDestroy() {
    this.signalrSubscriptions.forEach(
      subscription => subscription.unsubscribe()
    );
  }
}
