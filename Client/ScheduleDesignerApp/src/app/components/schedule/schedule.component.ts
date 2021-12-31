import { CdkDrag, CdkDragDrop, CdkDragEnter, CdkDragRelease, CdkDragStart, CdkDropList, DropListRef } from '@angular/cdk/drag-drop';
import { Component, EventEmitter, Input, OnInit, Output, QueryList, SimpleChanges, ViewChildren } from '@angular/core';
import { forkJoin, Subscription } from 'rxjs';
import { skip } from 'rxjs/operators';
import { SchedulePosition } from 'src/app/others/CommunicationObjects';
import { CourseEdition } from 'src/app/others/CourseEdition';
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

  @Input() settings: Settings;
  @Input() courseTypes: Map<number, CourseType>;
  @Input() modifyingScheduleData: ModifyingScheduleData;
  @Input() currentWeeks: {weeks: number[], tabSwitched: boolean};
  @Input() userIdFilter: number;

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

  loadingSubscription: Subscription;
  loading: boolean = true;

  schedule: CourseEdition[][][];

  constructor(
    private signalrService: SignalrService,
    private scheduleDesignerApiService:ScheduleDesignerApiService
  ) { }

  private updateLockInSchedule(position:SchedulePosition, value:boolean) {
    if (!this.schedule) {
      return;
    }

    const courseId = position.CourseId;
    const courseEditionId = position.CourseEditionId;
    const roomId = position.RoomId;
    const day = position.Day - 1;
    const periodIndex = position.PeriodIndex - 1;
    const weeks = position.Weeks;

    if (!this.currentWeeks.weeks.some(r => weeks.includes(r))) {
      return;
    }

    let courseEditions = this.schedule[day][periodIndex];
    courseEditions.forEach((courseEdition) => {
      if (courseEdition.CourseId == courseId && courseEdition.CourseEditionId == courseEditionId 
        && courseEdition.Room?.RoomId == roomId) {
        courseEdition.Locked = value;
      }
    });
  }

  private setSignalrSubscriptions(): void {
    this.signalrService.lastLockedSchedulePositions.pipe(skip(1)).subscribe((lockedSchedulePositions) => {
      this.updateLockInSchedule(lockedSchedulePositions, true);
    });

    this.signalrService.lastUnlockedSchedulePositions.pipe(skip(1)).subscribe((unlockedSchedulePositions) => {
      this.updateLockInSchedule(unlockedSchedulePositions, false);
    });

    this.signalrService.lastAddedSchedulePositions.pipe(skip(1)).subscribe((addedSchedulePositions) => {
      if (this.loading) {
        return;
      }

      const schedulePosition = addedSchedulePositions.SchedulePosition;
      const commonWeeks = schedulePosition.Weeks.filter(week => this.currentWeeks.weeks.includes(week));

      //filter for updated board
      if (addedSchedulePositions.CoordinatorsIds.includes(this.userIdFilter)) {
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
              this.scheduleDesignerApiService.GetCoordinators(addedSchedulePositions.CoordinatorsIds),
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
    });

    this.signalrService.lastModifiedSchedulePositions.pipe(skip(1)).subscribe((modifiedSchedulePositions) => {
      if (this.loading) {
        return;
      }

      const srcSchedulePosition = modifiedSchedulePositions.SourceSchedulePosition;
      const dstSchedulePosition = modifiedSchedulePositions.DestinationSchedulePosition;
      const commonWeeks = dstSchedulePosition.Weeks.filter(week => this.currentWeeks.weeks.includes(week));
      const movesIds = modifiedSchedulePositions.MovesIds;

      //filter for updated board
      if (modifiedSchedulePositions.CoordinatorsIds.includes(this.userIdFilter)) {
        let srcScheduleSlot = this.schedule[srcSchedulePosition.Day - 1][srcSchedulePosition.PeriodIndex - 1];
        let dstScheduleSlot = this.schedule[dstSchedulePosition.Day - 1][dstSchedulePosition.PeriodIndex - 1];
        
        if (srcSchedulePosition.PeriodIndex == dstSchedulePosition.PeriodIndex && srcSchedulePosition.Day == dstSchedulePosition.Day
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
                existingCourseEditions[0].Locked = false;
              });
            }
            this.scheduleDesignerApiService.AreSchedulePositionsLocked(
              dstSchedulePosition.RoomId, dstSchedulePosition.PeriodIndex,
              dstSchedulePosition.Day, existingCourseEditions[0].Weeks!
            ).subscribe((areLocked) => {
              existingCourseEditions[0].Locked = areLocked;
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
          
          if (existingSrcCourseEditions.length > 0) {
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
                existingSrcCourseEditions[0].Locked = areLocked;
              });
            }
          }

          //add or update new
          if (commonWeeks.length > 0) {
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
                  existingSrcCourseEditions[0].Locked = areLocked;
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
                this.scheduleDesignerApiService.GetCoordinators(modifiedSchedulePositions.CoordinatorsIds),
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
    });

    this.signalrService.lastRemovedSchedulePositions.pipe(skip(1)).subscribe((removedSchedulePositions) => {
      if (this.loading) {
        return;
      }

      const schedulePosition = removedSchedulePositions.SchedulePosition;
      const movesIds = removedSchedulePositions.MovesIds;

      //filter for updated board
      if (removedSchedulePositions.CoordinatorsIds.includes(this.userIdFilter)) {
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
              existingCourseEditions[0].Locked = areLocked;
            });
          }
        }
      }
    });

    this.signalrService.lastAddedScheduledMove.pipe(skip(1)).subscribe((addedScheduledMove) => {
      if (this.loading) {
        return;
      }
      
      const srcSchedulePosition = addedScheduledMove.sourceSchedulePosition;
      const commonWeeks = srcSchedulePosition.Weeks.filter(week => this.currentWeeks.weeks.includes(week));

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
    });

    this.signalrService.lastRemovedScheduledMove.pipe(skip(1)).subscribe((removedScheduledMove) => {
      if (this.loading) {
        return;
      }

      const srcSchedulePosition = removedScheduledMove.sourceSchedulePosition;
      const commonWeeks = srcSchedulePosition.Weeks.filter(week => this.currentWeeks.weeks.includes(week));

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
    });
  }

  ngOnInit(): void {
    this.setSignalrSubscriptions();
    if (this.currentWeeks.weeks.length > 0) {
      this.loadMySchedule();
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes.currentWeeks && !changes.currentWeeks.currentValue.tabSwitched && changes.currentWeeks.currentValue.weeks.length > 0) {
      const currentWeeks: number[] = changes.currentWeeks.currentValue.weeks;
      const previousWeeks: number[] = changes.currentWeeks.previousValue.weeks ?? [];

      if (currentWeeks.sort((a,b) => a - b).join(',') 
        !== previousWeeks.sort((a,b) => a - b).join(',')) {
          this.loadMySchedule();
      }
    }
  }

  private loadMySchedule() {
    this.loadingSubscription?.unsubscribe();
    this.loading = true;

    //TODO: getFilteredSchedule (not only as Coordinator)
    this.loadingSubscription = this.scheduleDesignerApiService.GetScheduleAsCoordinator(this.currentWeeks.weeks, this.courseTypes, this.settings).subscribe((mySchedule) => {
      this.schedule = mySchedule;

      let allGroups = new Array<Group>();
      let allRooms = new Array<Room>();

      for (let i = 0; i < this.schedule.length; ++i) {
        for (let j = 0; j < this.schedule[i].length; ++j) {
          for (let k = 0; k < this.schedule[i][j].length; ++k) {
            for (let l = 0; l < this.schedule[i][j][k].Groups.length; ++l) {
              allGroups.push(this.schedule[i][j][k].Groups[l]);
            }
            allRooms.push(this.schedule[i][j][k].Room!);
          }
        }
      }

      forkJoin([
        this.scheduleDesignerApiService.GetGroupsFullNames(allGroups.map((e) => e.GroupId)),
        this.scheduleDesignerApiService.GetRoomsNames(allRooms.map((e) => e.RoomId))
      ]).subscribe(([groupsFullNames, roomsNames]) => {
        for (let i = 0; i < groupsFullNames.length; ++i) {
          allGroups[i].FullName = groupsFullNames[i];
        }

        for (let i = 0; i < roomsNames.length; ++i) {
          allRooms[i].Name = roomsNames[i];
        }

        this.loading = false;
        this.onLoaded.emit();
      });
    });
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

  DragEnterPredicate(drag:CdkDrag<CourseEdition>, drop:CdkDropList<CourseEdition[]>) {
    //TODO:return drop.data.length < 1;
    return true;
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
}
