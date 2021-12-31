import { Component, ElementRef, OnInit, QueryList, ViewChild, ViewChildren } from '@angular/core';
import { CdkDrag, CdkDragDrop, CdkDragEnter, CdkDragRelease, CdkDragStart, CdkDropList, DropListRef, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { ScheduleDesignerApiService } from 'src/app/services/ScheduleDesignerApiService/schedule-designer-api.service';
import { CourseEdition } from 'src/app/others/CourseEdition';
import { SelectedCourseEdition } from 'src/app/others/SelectedCourseEdition';
import { CourseType, RoomType } from 'src/app/others/Types';
import { Group } from 'src/app/others/Group';
import { SignalrService } from 'src/app/services/SignalrService/signalr.service';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { Settings } from 'src/app/others/Settings';
import { forkJoin, Subscription } from 'rxjs';
import { MatSnackBar } from '@angular/material/snack-bar';
import { UsosApiService } from 'src/app/services/UsosApiService/usos-api.service';
import { Router } from '@angular/router';
import { skip } from 'rxjs/operators';
import { RoomSelectionComponent } from 'src/app/components/room-selection/room-selection.component';
import { Room } from 'src/app/others/Room';
import { RoomSelectionDialogData, RoomSelectionDialogResult, RoomSelectionDialogStatus } from 'src/app/others/dialogs/RoomSelectionDialog';
import { MessageObject, SchedulePosition } from 'src/app/others/CommunicationObjects';
import { Store } from '@ngrx/store';
import { Account } from 'src/app/others/Accounts';
import { AddRoomSelectionComponent } from 'src/app/components/add-room-selection/add-room-selection.component';
import { AddRoomSelectionDialogData, AddRoomSelectionDialogResult } from 'src/app/others/dialogs/AddRoomSelectionDialog';
import { ScheduledChangesDialogData, ScheduledChangesDialogResult } from 'src/app/others/dialogs/ScheduledChangesDialog';
import { ScheduledChangesViewComponent } from 'src/app/components/scheduled-changes-view/scheduled-changes-view.component';

@Component({
  selector: 'app-old-schedule',
  templateUrl: './old-schedule.component.html',
  styleUrls: ['./old-schedule.component.css']
})
export class OldScheduleComponent implements OnInit {

  @ViewChild('myCoursesSlot') myCoursesSlot : DropListRef<CourseEdition[]>
  @ViewChildren('scheduleSlots') scheduleSlots : QueryList<DropListRef<CourseEdition[]>>;
  currentDragEvent : CdkDragStart<CourseEdition> | null;
  currentDropContainerIndexes:number[];
  currentSelectedCourseEdition : SelectedCourseEdition | null;
  currentSelectedDropContainerId:string;
  currentAddRoomSelectionDialog : MatDialogRef<AddRoomSelectionComponent, any> | null;
  currentScheduledChangesDialog : MatDialogRef<ScheduledChangesViewComponent, any> | null;
  currentRoomSelectionDialog : MatDialogRef<RoomSelectionComponent, any> | null;
  isReleased:boolean = false;
  
  account:Account;

  loading:boolean = true;
  tabLoading:boolean = true;
  connectionStatus:boolean = false;
  
  settings:Settings;
  weeks:number[][];
  
  currentTabIndex:number = 0;
  currentLoadingSubscription:Subscription;
  
  tabLabels:string[] = ['Semester', 'Even Weeks', 'Odd Weeks'];
  scheduleTimeLabels:string[] = [];
  scheduleDayLabels:string[] = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday'];

  courseTypes:Map<number, CourseType>;
  roomTypes:Map<number, RoomType>;

  myCourses:CourseEdition[];
  schedule:CourseEdition[][][];
  
  scheduleSlotsValidity:boolean[][];
  areSlotsValiditySet:boolean = false;

  isMoveValid:boolean|null = null;

  constructor(
    private store:Store<{account: Account}>,
    private scheduleDesignerApiService:ScheduleDesignerApiService,
    private usosApiService:UsosApiService,
    private signalrService:SignalrService,
    private router:Router,
    private snackBar:MatSnackBar,
    private dialog:MatDialog
  ) { 
    this.store.select('account').subscribe((account) => {
      if (account.UserId == 0) {
        return;
      }
      this.account = account;
    });
  }

  private UpdateLockInMyCourses(courseId:number, courseEditionId:number, value:boolean) {
    if (!this.myCourses) {
      return;
    }

    this.myCourses.forEach((myCourse) => {
      if (myCourse.CourseId == courseId && myCourse.CourseEditionId == courseEditionId) {
        myCourse.Locked = value;
      }
    });
  }

  private UpdateLockInSchedule(position:SchedulePosition, value:boolean) {
    if (!this.schedule) {
      return;
    }

    const courseId = position.CourseId;
    const courseEditionId = position.CourseEditionId;
    const roomId = position.RoomId;
    const day = position.Day - 1;
    const periodIndex = position.PeriodIndex - 1;
    const weeks = position.Weeks;

    if (this.currentSelectedCourseEdition != null 
      && this.currentSelectedCourseEdition.CourseEdition.CourseId == courseId
      && this.currentSelectedCourseEdition.CourseEdition.CourseEditionId == courseEditionId
      && this.currentSelectedCourseEdition.CourseEdition.Room?.RoomId == roomId
      && this.currentSelectedCourseEdition.Day == day && this.currentSelectedCourseEdition.PeriodIndex == periodIndex
      && this.currentSelectedCourseEdition.CourseEdition.Weeks?.some(r => weeks.includes(r))) {
        this.currentSelectedCourseEdition.IsMoving = false;
        this.currentSelectedCourseEdition.CourseEdition.IsCurrentlyActive = false;
        this.currentSelectedCourseEdition = null;
    }

    //TODO: admin took control: currentDrag, all currentDialogs except addRoom,scheduledChanges

    if (!this.weeks[this.currentTabIndex].some(r => weeks.includes(r))) {
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

  private setSignalrSubscriptions():void {
    this.signalrService.lastLockedCourseEdition.pipe(skip(1)).subscribe(({courseId, courseEditionId}) => {
      this.UpdateLockInMyCourses(courseId, courseEditionId, true);
    });

    this.signalrService.lastLockedSchedulePositions.pipe(skip(1)).subscribe((lockedSchedulePositions) => {
      this.UpdateLockInSchedule(lockedSchedulePositions, true);
    });

    this.signalrService.lastUnlockedCourseEdition.pipe(skip(1)).subscribe(({courseId, courseEditionId}) => {
      this.UpdateLockInMyCourses(courseId, courseEditionId, false);
    });

    this.signalrService.lastUnlockedSchedulePositions.pipe(skip(1)).subscribe((unlockedSchedulePositions) => {
      this.UpdateLockInSchedule(unlockedSchedulePositions, false);
    });

    this.signalrService.lastAddedSchedulePositions.pipe(skip(1)).subscribe((addedSchedulePositions) => {
      if (this.tabLoading) {
        return;
      }

      const schedulePosition = addedSchedulePositions.SchedulePosition;
      const addedAmount = schedulePosition.Weeks.length;
      const commonWeeks = schedulePosition.Weeks.filter(week => this.weeks[this.currentTabIndex].includes(week));

      //filter for updated board
      if (addedSchedulePositions.CoordinatorsIds.includes(this.account.UserId)) {
        //remove from my courses
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
        
        //add new
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

      //active dragged and selected fields update
      const event = this.currentDragEvent?.source;
      const item = (event != undefined) ? this.currentDragEvent?.source.data : this.currentSelectedCourseEdition?.CourseEdition;

      if (item == undefined || commonWeeks.length == 0) {
        return;
      }

      if (item.Coordinators.map(c => c.UserId).some(c => addedSchedulePositions.CoordinatorsIds.includes(c))
        || item.Groups.map(g => g.GroupId).some(g => addedSchedulePositions.GroupsIds.includes(g))) {
          if (this.currentDragEvent != null || this.currentSelectedCourseEdition?.IsMoving) {
            this.scheduleSlotsValidity[schedulePosition.Day - 1][schedulePosition.PeriodIndex - 1] = false;
          }
          
          if (event?.dropContainer.id === 'my-courses') {
            const connectedTo = event.dropContainer.connectedTo as string[];
            const id = `${schedulePosition.Day - 1},${schedulePosition.PeriodIndex - 1}`;
            if (this.currentDragEvent != null) {
              this.currentDragEvent.source.dropContainer.connectedTo = connectedTo.filter(e => e !== id);
              event.dropContainer._dropListRef.enter(
                event._dragRef,
                0,0
              );
            }
          }
          else if (event != undefined && this.currentDropContainerIndexes[0] !== -1 && this.currentDropContainerIndexes[1] !== -1) {
            if (this.currentDropContainerIndexes[0] == schedulePosition.Day - 1 
              && this.currentDropContainerIndexes[1] == schedulePosition.PeriodIndex - 1) {
                this.isMoveValid = false;
            }
          }
      }
    });
    
    this.signalrService.lastModifiedSchedulePositions.pipe(skip(1)).subscribe((modifiedSchedulePositions) => {
      if (this.tabLoading) {
        return;
      }
      
      const srcSchedulePosition = modifiedSchedulePositions.SourceSchedulePosition;
      const dstSchedulePosition = modifiedSchedulePositions.DestinationSchedulePosition;
      const commonWeeks = dstSchedulePosition.Weeks.filter(week => this.weeks[this.currentTabIndex].includes(week));
      const movesIds = modifiedSchedulePositions.MovesIds;

      //scheduled change occurred
      const currentDrag = this.currentDragEvent?.source;
      if (currentDrag != null) {
        const currentIndexes = (currentDrag.dropContainer.id !== 'my-courses') ? this.getIndexes(currentDrag.dropContainer.id) : [-1,-1];
        if (currentDrag.data.CourseId == srcSchedulePosition.CourseId 
          && currentDrag.data.CourseEditionId == srcSchedulePosition.CourseEditionId && currentDrag.data.Room?.RoomId == srcSchedulePosition.RoomId
          && currentIndexes[1] == srcSchedulePosition.PeriodIndex - 1 && currentIndexes[0] == srcSchedulePosition.Day - 1
          && currentDrag.data.Weeks?.some(c => srcSchedulePosition.Weeks.includes(c))) {
            if (this.currentDragEvent != null) {
              this.currentDragEvent.source.dropContainer._dropListRef.enter(
                this.currentDragEvent.source._dragRef,
                0,0
              );
              document.dispatchEvent(new Event('mouseup'));
            }
        }
      }

      //scheduled change occurred
      const selectedCourseEdition = this.currentSelectedCourseEdition;
      if (selectedCourseEdition != null) {
        if (selectedCourseEdition.CourseEdition.CourseId == srcSchedulePosition.CourseId
          && selectedCourseEdition.CourseEdition.CourseEditionId == srcSchedulePosition.CourseEditionId 
          && selectedCourseEdition.CourseEdition.Room?.RoomId == srcSchedulePosition.RoomId
          && selectedCourseEdition.PeriodIndex == srcSchedulePosition.PeriodIndex - 1 && selectedCourseEdition.Day == srcSchedulePosition.Day - 1
          && selectedCourseEdition.CourseEdition.Weeks?.some(c => srcSchedulePosition.Weeks.includes(c))) {
            if (this.currentSelectedCourseEdition != null) {
              this.currentSelectedCourseEdition.CourseEdition.IsCurrentlyActive = false;
            }
            this.currentSelectedCourseEdition = null;
          }
      }

      //scheduled change occurred
      const currentDialogData = this.currentRoomSelectionDialog?.componentInstance.data;
      if (currentDialogData != null) {
        const currentIndexes = currentDialogData.SrcIndexes;
        if (currentDialogData.CourseEdition.CourseId == srcSchedulePosition.CourseId
          && currentDialogData.CourseEdition.CourseEditionId == srcSchedulePosition.CourseEditionId && currentDialogData.CourseEdition.Room?.RoomId == srcSchedulePosition.RoomId
          && currentIndexes[1] == srcSchedulePosition.PeriodIndex - 1 && currentIndexes[0] == srcSchedulePosition.Day - 1
          && currentDialogData.CourseEdition.Weeks?.some(c => srcSchedulePosition.Weeks.includes(c))) {
            if (this.currentRoomSelectionDialog != null) {
              this.currentRoomSelectionDialog.close(RoomSelectionComponent.CANCELED);
            }
        }
      }


      //filter for updated board
      if (modifiedSchedulePositions.CoordinatorsIds.includes(this.account.UserId)) {
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

      //active dragged and selected fields update
      const event = this.currentDragEvent?.source;
      const item = (event != undefined) ? this.currentDragEvent?.source.data : this.currentSelectedCourseEdition?.CourseEdition;
      
      if (item == undefined || commonWeeks.length == 0) {
        return;
      }

      if (item.Coordinators.map(c => c.UserId).some(c => modifiedSchedulePositions.CoordinatorsIds.includes(c))
        || item.Groups.map(g => g.GroupId).some(g => modifiedSchedulePositions.GroupsIds.includes(g))) {
        if (this.currentDragEvent != null || this.currentSelectedCourseEdition?.IsMoving) {
          this.scheduleSlotsValidity[dstSchedulePosition.Day - 1][dstSchedulePosition.PeriodIndex - 1] = false;
        }
        
        
        if (event?.dropContainer.id === 'my-courses') {
          const connectedTo = event.dropContainer.connectedTo as string[];
          const id = `${dstSchedulePosition.Day - 1},${dstSchedulePosition.PeriodIndex - 1}`;
          if (this.currentDragEvent != null) {
            this.currentDragEvent.source.dropContainer.connectedTo = connectedTo.filter(e => e !== id);
            event.dropContainer._dropListRef.enter(
              event._dragRef,
              0,0
            );
          }
        }
        else if (event != undefined && this.currentDropContainerIndexes[0] !== -1 && this.currentDropContainerIndexes[1] !== -1) {
          if (this.currentDropContainerIndexes[0] == dstSchedulePosition.Day - 1 
            && this.currentDropContainerIndexes[1] == dstSchedulePosition.PeriodIndex - 1) {
              this.isMoveValid = false;
          }
        }

        this.scheduleDesignerApiService.IsPeriodBusy(
          srcSchedulePosition.CourseId, srcSchedulePosition.CourseEditionId,
          srcSchedulePosition.PeriodIndex, srcSchedulePosition.Day,
          srcSchedulePosition.Weeks.filter(week => this.weeks[this.currentTabIndex].includes(week))
        ).subscribe((isBusy) => {
          if (this.currentDragEvent != null || this.currentSelectedCourseEdition?.IsMoving) {
            this.scheduleSlotsValidity[srcSchedulePosition.Day - 1][srcSchedulePosition.PeriodIndex - 1] = !isBusy;
          }

          if (isBusy) {
            return;
          }

          if (event?.dropContainer.id === 'my-courses') {
            const connectedTo = event.dropContainer.connectedTo as string[];
            const id = `${srcSchedulePosition.Day - 1},${srcSchedulePosition.PeriodIndex - 1}`;
            if (this.currentDragEvent != null) {
              connectedTo.push(id);
              this.currentDragEvent.source.dropContainer.connectedTo = connectedTo;
              event.dropContainer._dropListRef.enter(
                event._dragRef,
                0,0
              );
            }
          } else if (event != undefined && this.currentDropContainerIndexes[0] !== -1 && this.currentDropContainerIndexes[1] !== -1) {
            if (this.currentDropContainerIndexes[0] == srcSchedulePosition.Day - 1 
              && this.currentDropContainerIndexes[1] == srcSchedulePosition.PeriodIndex - 1) {
                this.isMoveValid = true;
            }
          }
          
        });
      }

    });
    
    this.signalrService.lastRemovedSchedulePositions.pipe(skip(1)).subscribe((removedSchedulePositions) => {
      if (this.tabLoading) {
        return;
      }
      
      const schedulePosition = removedSchedulePositions.SchedulePosition;
      const removedAmount = schedulePosition.Weeks.length;
      const commonWeeks = schedulePosition.Weeks.filter(week => this.weeks[this.currentTabIndex].includes(week));
      const movesIds = removedSchedulePositions.MovesIds;

      //filter for updated board
      if (removedSchedulePositions.CoordinatorsIds.includes(this.account.UserId)) {
        //add to my courses
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
            courseEdition.Locked = firstCourse.Locked;
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
            this.scheduleDesignerApiService.GetMyCourseEdition(
              schedulePosition.CourseId, schedulePosition.CourseEditionId,
              this.weeks[this.currentTabIndex].length, this.courseTypes,
              this.settings
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

        //remove old
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

      //active dragged and selected fields update
      const item = this.currentDragEvent?.source.data;
      const event = this.currentDragEvent?.source;
      if (item == undefined || commonWeeks.length == 0) {
        return;
      }

      if (item.Coordinators.map(c => c.UserId).some(c => removedSchedulePositions.CoordinatorsIds.includes(c))
        || item.Groups.map(g => g.GroupId).some(g => removedSchedulePositions.GroupsIds.includes(g))) {

        this.scheduleDesignerApiService.IsPeriodBusy(
          schedulePosition.CourseId, schedulePosition.CourseEditionId,
          schedulePosition.PeriodIndex, schedulePosition.Day,
          schedulePosition.Weeks.filter(week => this.weeks[this.currentTabIndex].includes(week))
        ).subscribe((isBusy) => {
          if (this.currentDragEvent != null || this.currentSelectedCourseEdition?.IsMoving) {
            this.scheduleSlotsValidity[schedulePosition.Day - 1][schedulePosition.PeriodIndex - 1] = !isBusy;
          }

          if (isBusy) {
            return;
          }

          if (event?.dropContainer.id === 'my-courses') {
            const connectedTo = event.dropContainer.connectedTo as string[];
            const id = `${schedulePosition.Day - 1},${schedulePosition.PeriodIndex - 1}`;
            if (this.currentDragEvent != null) {
              connectedTo.push(id);
              this.currentDragEvent.source.dropContainer.connectedTo = connectedTo;
              event.dropContainer._dropListRef.enter(
                event._dragRef,
                0,0
              );
            }
          } else if (event != undefined && this.currentDropContainerIndexes[0] !== -1 && this.currentDropContainerIndexes[1] !== -1) {
            if (this.currentDropContainerIndexes[0] == schedulePosition.Day - 1 
              && this.currentDropContainerIndexes[1] == schedulePosition.PeriodIndex - 1) {
                this.isMoveValid = true;
            }
          }
          
        });
      }
    });

    this.signalrService.lastAddedScheduledMove.pipe(skip(1)).subscribe((addedScheduledMove) => {
      if (this.tabLoading) {
        return;
      }

      const srcSchedulePosition = addedScheduledMove.sourceSchedulePosition;
      const commonWeeks = srcSchedulePosition.Weeks.filter(week => this.weeks[this.currentTabIndex].includes(week));

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
      if (this.tabLoading) {
        return;
      }

      const srcSchedulePosition = removedScheduledMove.sourceSchedulePosition;
      const commonWeeks = srcSchedulePosition.Weeks.filter(week => this.weeks[this.currentTabIndex].includes(week));

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

  ngOnInit(): void { ///DONE
    let isConnectedSubscription = this.signalrService.isConnected.pipe(skip(1)).subscribe((status) => {
      this.connectionStatus = status;
      if (!status && !this.signalrService.connectionIntentionallyStopped) {
        this.snackBar.open("Connection with server has been lost. Please refresh the page to possibly reconnect.", "OK");
      }
    });

    this.setSignalrSubscriptions();

    forkJoin([
      this.signalrService.InitConnection(),
      this.scheduleDesignerApiService.GetSettings(),
      this.scheduleDesignerApiService.GetPeriods(),
      this.scheduleDesignerApiService.GetCourseTypes(),
      this.scheduleDesignerApiService.GetRoomTypes()
    ]).subscribe(([,settings,periods,courseTypes,roomTypes]) => {
      this.connectionStatus = true;
      
      this.settings = settings;
      this.settings.Periods = periods;
      this.courseTypes = courseTypes;
      this.roomTypes = roomTypes;

      this.setLabels();
      this.setFrequenciesAndWeeks();
      this.initializeValues();

      this.getMyCourseEditionsAndScheduleAsCoordinator(0);
    }, (error) => {
      if (error?.status == 401) {
        this.usosApiService.Deauthorize();

        this.snackBar.open('Session expired. Please log in again.', 'OK');
        this.router.navigate(['login']);
      } else if (!isConnectedSubscription.closed) {
        this.snackBar.open("Connection with server failed. Please refresh the page to try again.", "OK");
      }
    });
  }

  private getMyCourseEditionsAndScheduleAsCoordinator(index:number) { ///DONE

    this.currentLoadingSubscription = forkJoin([
      this.scheduleDesignerApiService.GetMyCourseEditions(this.weeks[this.currentTabIndex].length, this.courseTypes, this.settings),
      this.scheduleDesignerApiService.GetScheduleAsCoordinator(this.weeks[index], this.courseTypes, this.settings)
    ]).subscribe(([myCourses, mySchedule]) => {
      this.myCourses = myCourses;
      this.schedule = mySchedule;

      if (this.currentSelectedCourseEdition != null) {
        //update busy periods
        if (this.currentSelectedCourseEdition.IsMoving) {
          this.scheduleDesignerApiService.GetBusyPeriods(
            this.currentSelectedCourseEdition.CourseEdition.CourseId, 
            this.currentSelectedCourseEdition.CourseEdition.CourseEditionId,
            this.weeks[this.currentTabIndex]
          ).subscribe(busySlots => {
            const numberOfSlots = this.settings.Periods.length - 1;
            let busySlotIndex = 0;
            
            for (let i = 0; i < this.scheduleSlots.length; ++i) {
              if (i != (busySlots[busySlotIndex]?.Day - 1) * numberOfSlots + (busySlots[busySlotIndex]?.PeriodIndex - 1)) {
                this.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = true;
              } else {
                this.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = false;
                ++busySlotIndex;
              }
            }
          });
        }
        //look for selected course
        if (this.weeks[index].sort((a,b) => a - b).join(',') === this.currentSelectedCourseEdition.CourseEdition.Weeks?.sort((a,b) => a - b).join(',')) {
          this.schedule[this.currentSelectedCourseEdition.Day][this.currentSelectedCourseEdition.PeriodIndex]
            .forEach((courseEdition) => {
              if (this.currentSelectedCourseEdition?.CourseEdition.CourseId == courseEdition.CourseId
                && this.currentSelectedCourseEdition?.CourseEdition.CourseEditionId == courseEdition.CourseEditionId
                && this.currentSelectedCourseEdition?.CourseEdition.Room?.RoomId == courseEdition.Room?.RoomId) {
                  this.currentSelectedCourseEdition.CourseEdition = courseEdition;
                  this.currentSelectedCourseEdition.CourseEdition.IsCurrentlyActive = true;
                  this.currentSelectedCourseEdition.CanChangeRoom = true;
                  this.currentSelectedCourseEdition.CanMakeMove = true;
              }
          });
        } else {
          this.currentSelectedCourseEdition.CanChangeRoom = false;
          if (this.weeks[this.currentTabIndex].length == this.currentSelectedCourseEdition.CourseEdition.Weeks?.length) {
            this.currentSelectedCourseEdition.CanMakeMove = true;
          }
        }
      }

      let allGroups = new Array<Group>();
      let allRooms = new Array<Room>();
      
      for (let i = 0; i < this.myCourses.length; ++i) {
        for (let j = 0; j < this.myCourses[i].Groups.length; ++j) {
          allGroups.push(this.myCourses[i].Groups[j]);
        }
      }
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
        this.tabLoading = false;
      });
    });
  }

  private setLabels() { ///DONE
    for (let i:number = 0; i < this.settings.TermDurationWeeks; ++i) {
      this.tabLabels.push('Week ' + (i + 1));
    }
  }

  private setFrequenciesAndWeeks() { ///DONE
    this.weeks = [[],[],[]];

    for (let i:number = 0; i < this.settings.TermDurationWeeks; ++i) {
      const weekNumber = i + 1;
      this.weeks[0].push(weekNumber);
      this.weeks.push([weekNumber]);
      if (weekNumber % 2 == 0) {
        this.weeks[1].push(weekNumber);
      } else {
        this.weeks[2].push(weekNumber);
      }
    }
  }

  private initializeValues() { ///DONE
    const periods = this.settings.Periods;
    const numberOfSlots = periods.length - 1;
    
    for (let i:number = 0; i < numberOfSlots; ++i) {
      this.scheduleTimeLabels.push(
        periods[i] + ' - ' + periods[i + 1]
      );
    }

    this.scheduleSlotsValidity = [];
    for (let j:number = 0; j < 5; ++j) {
      this.scheduleSlotsValidity.push([]);
      for (let i:number = 0; i < numberOfSlots; ++i) {
        this.scheduleSlotsValidity[j].push(false);
      }
    }
  }

  private resetSchedule() { ///DONE
    const numberOfSlots = this.settings.Periods.length - 1;

    this.schedule = [];
    for (let j:number = 0; j < 5; ++j) {
      this.schedule.push([]);
      for (let i:number = 0; i < numberOfSlots; ++i) {
        this.schedule[j].push([]);
      }
    }
  }

  private getIndexes(id:string):number[] { ///DONE
    const indexes = id.split(',');
    return [
      Number.parseInt(indexes[0]),
      Number.parseInt(indexes[1])
    ];
  }

  GetMaxElementIndexOnDay(dayIndex:number):number { ///DONE
    let dayScheduleLength:number[] = [];
    for (let i = 0; i < this.settings.Periods.length - 1; ++i) {
      dayScheduleLength.push(this.schedule[dayIndex][i].length);
    }
    return Math.max(...dayScheduleLength) - 1;
  }

  OnTabChange(index:number) { ///DONE
    this.currentLoadingSubscription?.unsubscribe();
    this.currentTabIndex = index;
    this.tabLoading = true;

    this.resetSchedule();
    this.getMyCourseEditionsAndScheduleAsCoordinator(index);
  }

  async DropInMyCourses(event:CdkDragDrop<CourseEdition[], CourseEdition[], CourseEdition>) { ///DONE
    if (this.currentDragEvent == null) {
      return;
    }

    const courseEdition = event.item.data;
    const previousContainer = event.previousContainer;
    const currentContainer = event.container;
    const weeks = this.weeks[this.currentTabIndex];
    const isScheduleSource = event.previousContainer.id !== 'my-courses';

    if (isScheduleSource) {
      const previousIndexes = this.getIndexes(previousContainer.id);

      try {
        const result = await new Promise<MessageObject>((resolve, reject) => {
          const responseSubscription = this.signalrService.lastResponse.pipe(skip(1))
          .subscribe((messageObject) => {
            responseSubscription.unsubscribe();
            resolve(messageObject);
          },() => {
            reject();
          });
          this.signalrService.RemoveSchedulePositions(
            courseEdition.Room!.RoomId, previousIndexes[1] + 1,
            previousIndexes[0] + 1, weeks
          );
          setTimeout(() => responseSubscription.unsubscribe(), 15000);
        });
        
        if (result.StatusCode >= 400) {
          throw result;
        }

        transferArrayItem(
          previousContainer.data,
          currentContainer.data,
          event.previousIndex,
          event.currentIndex
        );
        courseEdition.Room = null;
        courseEdition.CurrentAmount = courseEdition.Weeks?.length ?? 0;
        courseEdition.Weeks = null;
        courseEdition.ScheduledMoves = [];

        this.myCourses.forEach((element) => {
          if (element.CourseId == courseEdition.CourseId 
            && element.CourseEditionId == courseEdition.CourseEditionId) {
              element.ScheduleAmount -= weeks.length;
          }
        });

        for (let i = 0; i < this.schedule.length; ++i) {
          for (let j = 0; j < this.schedule[i].length; ++j) {
            for (let k = 0; k < this.schedule[i][j].length; ++j) {
              const currentCourseEdition = this.schedule[i][j][k];
              if (currentCourseEdition.CourseId == courseEdition.CourseId
                && currentCourseEdition.CourseEditionId == courseEdition.CourseEditionId) {
                  currentCourseEdition.ScheduleAmount -= weeks.length;
              }
            }
          }
        }
      }
      catch (error:any) {
        if (error.Message != undefined) {
          this.snackBar.open(error.Message, "OK");
        }
      }
    }
    
    if (this.currentSelectedCourseEdition != null) {
      this.currentSelectedCourseEdition.CourseEdition.IsCurrentlyActive = false;
    }
    this.currentSelectedCourseEdition = null;

    if (!isScheduleSource) {
      try {
        const result = await this.signalrService.UnlockCourseEdition(
          courseEdition.CourseId, courseEdition.CourseEditionId
        ).toPromise();
        
        if (result.StatusCode >= 400) {
          throw result;
        }
        
        courseEdition.Locked = false;
      } catch (error) {
        
      }
    } else {
      try {
        const result = await this.scheduleDesignerApiService.IsCourseEditionLocked(
          courseEdition.CourseId, courseEdition.CourseEditionId
        ).toPromise();

        courseEdition.Locked = result;
      }
      catch (error) {

      }
    }
    this.currentDragEvent = null;
    this.isMoveValid = null;
    event.item.data.IsCurrentlyActive = false;
  }

  async DropInSchedule(event:CdkDragDrop<CourseEdition[], CourseEdition[], CourseEdition>) { ///DONE
    this.isReleased = true;

    if (this.currentDragEvent == null) {
      return;
    }

    const courseEdition = event.item.data;
    const previousContainer = event.previousContainer;
    const currentContainer = event.container;
    const isScheduleSource = previousContainer.id !== 'my-courses';
    const previousIndexes = (isScheduleSource) ? this.getIndexes(previousContainer.id) : [-1,-1];
    const currentIndexes = this.getIndexes(currentContainer.id);
    const weeks = this.weeks[this.currentTabIndex];
    
    if (previousContainer === currentContainer || !this.areSlotsValiditySet) {
      try {
        const result = await this.signalrService.UnlockSchedulePositions(
          courseEdition.Room!.RoomId, previousIndexes[1] + 1,
          previousIndexes[0] + 1, weeks
        ).toPromise();
        
        if (result.StatusCode >= 400) {
          throw result;
        }
        courseEdition.Locked = false;
      } catch (error) {

      }
      this.currentDragEvent = null;
      if (this.currentSelectedCourseEdition != null) {
        this.currentSelectedCourseEdition.CourseEdition.IsCurrentlyActive = false;
      }
      this.currentSelectedCourseEdition = null;
      this.isMoveValid = null;
      event.item.data.IsCurrentlyActive = false;
      return;
    }
    
    const dialogData = new RoomSelectionDialogData(
      courseEdition,
      previousIndexes,
      currentIndexes,
      weeks,
      this.scheduleDayLabels,
      this.scheduleTimeLabels,
      this.roomTypes,
      this.isMoveValid!,
      isScheduleSource,
      this.account.UserId
    );

    this.currentRoomSelectionDialog = this.dialog.open(RoomSelectionComponent, {
      disableClose: true,
      data: dialogData
    });
    const dialogResult:RoomSelectionDialogResult = await this.currentRoomSelectionDialog.afterClosed().toPromise();
    this.currentRoomSelectionDialog = null;
    this.isMoveValid = null;
    this.currentSelectedCourseEdition = null;

    switch (dialogResult.Status) {
      case RoomSelectionDialogStatus.ACCEPTED: {
        courseEdition.Room = dialogResult.Room;
        courseEdition.Weeks = dialogResult.Weeks;
        courseEdition.ScheduledMoves = [];

        if (!isScheduleSource) {
          
          this.myCourses.forEach((element) => {
            if (element.CourseId == courseEdition.CourseId 
              && element.CourseEditionId == courseEdition.CourseEditionId) {
                element.ScheduleAmount += dialogResult.Weeks.length;
            }
          });

          for (let i = 0; i < this.schedule.length; ++i) {
            for (let j = 0; j < this.schedule[i].length; ++j) {
              for (let k = 0; k < this.schedule[i][j].length; ++j) {
                const currentCourseEdition = this.schedule[i][j][k];
                if (currentCourseEdition.CourseId == courseEdition.CourseId
                  && currentCourseEdition.CourseEditionId == courseEdition.CourseEditionId) {
                    currentCourseEdition.ScheduleAmount += dialogResult.Weeks.length;
                }
              }
            }
          }
        }

        const previousIndex = (isScheduleSource) ? event.previousIndex 
          : this.myCourses.findIndex(value => value.IsCurrentlyActive);

        transferArrayItem(
          event.previousContainer.data,
          event.container.data,
          previousIndex,
          event.currentIndex
        );
      } break;
      case RoomSelectionDialogStatus.SCHEDULED: {

      } break;
      case RoomSelectionDialogStatus.CANCELED: {
        if (dialogResult.Message != undefined) {
          this.snackBar.open(dialogResult.Message, "OK");
        }
      } break;
      case RoomSelectionDialogStatus.FAILED: {
        if (dialogResult.Message != undefined) {
          this.snackBar.open(dialogResult.Message, "OK");
        }
      } break;
    }

    if (!isScheduleSource) {
      try {
        const result = await this.signalrService.UnlockCourseEdition(
          courseEdition.CourseId, courseEdition.CourseEditionId
        ).toPromise();
        
        if (result.StatusCode >= 400) {
          throw result;
        }
        
        courseEdition.Locked = false;
      } catch (error) {
  
      }
    } else if (dialogResult.Status == RoomSelectionDialogStatus.ACCEPTED) {
      courseEdition.Locked = false;
    } else {
      try {
        const result = await this.signalrService.UnlockSchedulePositions(
          courseEdition.Room!.RoomId, previousIndexes[1] + 1,
          previousIndexes[0] + 1, weeks
        ).toPromise();
        
        if (result.StatusCode >= 400) {
          throw result;
        }
        
        courseEdition.Locked = false;
      } catch (error) {
  
      }
    }

    event.item.data.IsCurrentlyActive = false;
  }

  IsScheduleSlotDisabled(dayIndex:number, slotIndex:number) { ///DONE
    return this.schedule[dayIndex][slotIndex].length > 1;
  }

  ScheduleSlotEnterPredicate(drag:CdkDrag<CourseEdition>, drop:CdkDropList<CourseEdition[]>) { ///DONE
    //TODO:return drop.data.length < 1;
    return true;
  }

  OnMyCoursesEnter(drag:CdkDragEnter<CourseEdition[]>) { ///DONE
    this.isMoveValid = null;
    this.currentDropContainerIndexes = [-1,-1];
  }

  OnScheduleSlotEnter(drag:CdkDragEnter<CourseEdition[]>) {
    const indexes = this.getIndexes(drag.container.id);
    this.isMoveValid = this.scheduleSlotsValidity[indexes[0]][indexes[1]];
    this.currentDropContainerIndexes = this.getIndexes(drag.container.id);
  }

  async OnStartDragging(event:CdkDragStart<CourseEdition>) { ///DONE
    this.isReleased = false;
    this.areSlotsValiditySet = false;
    this.currentDragEvent = event;
    event.source.data.IsCurrentlyActive = true;
    
    const courseEdition = event.source.data;
    const dropContainer = event.source.dropContainer;
    const isScheduleSource = dropContainer.id !== 'my-courses';
    const indexes = (isScheduleSource) ? this.getIndexes(dropContainer.id) : [-1,-1];

    if (this.currentSelectedCourseEdition != null) {
      this.currentSelectedCourseEdition.CourseEdition.IsCurrentlyActive = false;
    }
    this.currentSelectedCourseEdition = new SelectedCourseEdition(courseEdition, indexes[1], indexes[0]);
    this.currentSelectedCourseEdition.CanShowScheduledChanges = courseEdition.ScheduledMoves.length > 0;

    try {
      if (!this.isReleased) {
        const result = (!isScheduleSource) 
        ? await this.signalrService.LockCourseEdition(
          courseEdition.CourseId, courseEdition.CourseEditionId
        ).toPromise() 
        : await this.signalrService.LockSchedulePositions(
          courseEdition.Room!.RoomId, indexes[1] + 1,
          indexes[0] + 1, this.weeks[this.currentTabIndex]
        ).toPromise();
        
        if (result.StatusCode >= 400) {
          throw result;
        }
        courseEdition.Locked = true;
      } else {
        return;
      }
    } catch (error:any) {
      if (!this.isReleased) {
        dropContainer._dropListRef.enter(
          event.source._dragRef,
          0,0
        );
        document.dispatchEvent(new Event('mouseup'));
        this.isReleased = true;
      } else {
        if (this.currentRoomSelectionDialog != null) {
          this.currentRoomSelectionDialog.close(RoomSelectionDialogResult.CANCELED);
        }
      }
      courseEdition.Locked = true;
      if (error.Message != undefined) {
        this.snackBar.open(error.Message, "OK");
      }

      this.currentDragEvent = null;
      if (this.currentSelectedCourseEdition != null) {
        this.currentSelectedCourseEdition.CourseEdition.IsCurrentlyActive = false;
      }
      this.currentSelectedCourseEdition = null;
      this.isMoveValid = null;
      event.source.data.IsCurrentlyActive = false;
      return;
    }

    let busySlots = await this.scheduleDesignerApiService.GetBusyPeriods(
      courseEdition.CourseId, 
      courseEdition.CourseEditionId,
      this.weeks[this.currentTabIndex]
    ).toPromise();
    let connectedTo = ['my-courses'];
    
    const numberOfSlots = this.settings.Periods.length - 1;
    let busySlotIndex = 0;
    let scheduleSlots = this.scheduleSlots.toArray();
    
    if (isScheduleSource) {
      for (let i = 0; i < this.scheduleSlots.length; ++i) {
        let element = scheduleSlots[i].element as ElementRef<HTMLElement>;
        connectedTo.push(element.nativeElement.id);
        if (i != (busySlots[busySlotIndex]?.Day - 1) * numberOfSlots + (busySlots[busySlotIndex]?.PeriodIndex - 1)) {
          this.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = true;
        } else {
          this.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = false;
          ++busySlotIndex;
        }
      }
      this.scheduleSlotsValidity[indexes[0]][indexes[1]] = true;
    } else {
      for (let i = 0; i < this.scheduleSlots.length; ++i) {
        let element = scheduleSlots[i].element as ElementRef<HTMLElement>;
        if (i != (busySlots[busySlotIndex]?.Day - 1) * numberOfSlots + (busySlots[busySlotIndex]?.PeriodIndex - 1)) {
          this.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = true;
          connectedTo.push(element.nativeElement.id);
        } else {
          this.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = false;
          ++busySlotIndex;
        }
      }
    }
    this.areSlotsValiditySet = true;
    dropContainer.connectedTo = connectedTo;
    
    if (!this.isReleased) {
      dropContainer._dropListRef.enter(
        event.source._dragRef, 0, 0
      );
    }
    
    if (this.isReleased) {
      try {
        const result = (!isScheduleSource) 
        ? await this.signalrService.UnlockCourseEdition(
          courseEdition.CourseId, courseEdition.CourseEditionId
        ).toPromise()
        : await this.signalrService.UnlockSchedulePositions(
          courseEdition.Room!.RoomId, indexes[1] + 1,
          indexes[0] + 1, this.weeks[this.currentTabIndex]
        ).toPromise();
        
        if (result.StatusCode >= 400) {
          throw result;
        }
        courseEdition.Locked = false;
      } catch (error) {
        
      }
      this.currentDragEvent = null;
      if (this.currentSelectedCourseEdition != null) {
        this.currentSelectedCourseEdition.CourseEdition.IsCurrentlyActive = false;
      }
      this.currentSelectedCourseEdition = null;
      for (let i = 0; i < this.scheduleSlots.length; ++i) {
        this.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = false;
      }
    }
  }

  async OnReleaseDragging(event:CdkDragRelease<CourseEdition>) { ///DONE
    this.isReleased = true;

    event.source.dropContainer.connectedTo = [];

    this.currentDragEvent = null;
    const numberOfSlots = this.settings.Periods.length - 1;
    for (let i = 0; i < this.scheduleSlots.length; ++i) {
      this.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = false;
    }
  }


  Select(event:{courseEdition:CourseEdition,isDisabled:boolean}, day:number, periodIndex:number) {
    if (this.currentSelectedCourseEdition != null) {
      if (this.currentSelectedCourseEdition.IsMoving) {
        return;
      }
      this.currentSelectedCourseEdition.CourseEdition.IsCurrentlyActive = false;
      if (event.courseEdition == this.currentSelectedCourseEdition.CourseEdition) {
        this.currentSelectedCourseEdition = null;
        return;
      }
    }
    this.currentSelectedCourseEdition = new SelectedCourseEdition(event.courseEdition, periodIndex, day);
    this.currentSelectedCourseEdition.CanChangeRoom = !event.isDisabled;
    this.currentSelectedCourseEdition.CanMakeMove = !event.isDisabled;
    this.currentSelectedCourseEdition.CanShowScheduledChanges = event.courseEdition.ScheduledMoves.length > 0;
    event.courseEdition.IsCurrentlyActive = true;
  }

  async AddRoom() {
    if (this.currentSelectedCourseEdition == null) {
      return;
    }
    const selectedCourseEdition = this.currentSelectedCourseEdition;

    const dialogData = new AddRoomSelectionDialogData(
      selectedCourseEdition.CourseEdition,
      this.roomTypes,
      this.account.UserId
    );

    this.currentAddRoomSelectionDialog = this.dialog.open(AddRoomSelectionComponent, {
      disableClose: true,
      data: dialogData
    });
    const dialogResult:AddRoomSelectionDialogResult = await this.currentAddRoomSelectionDialog.afterClosed().toPromise();
    this.currentAddRoomSelectionDialog = null;

    if (dialogResult.Message != undefined) {
      this.snackBar.open(dialogResult.Message, "OK");
    }
  }

  async ChangeRoom() {
    if (this.currentSelectedCourseEdition == null) {
      return;
    }
    const selectedCourseEdition = this.currentSelectedCourseEdition;

    try {
      const result = await this.signalrService.LockSchedulePositions(
        selectedCourseEdition.CourseEdition.Room!.RoomId, selectedCourseEdition.PeriodIndex + 1,
        selectedCourseEdition.Day + 1, selectedCourseEdition.CourseEdition.Weeks!
      ).toPromise();
      
      if (result.StatusCode >= 400) {
        throw result;
      }
      
      this.currentSelectedCourseEdition.CourseEdition.Locked = true;
    } catch (error:any) {
      if (error.Message != undefined) {
        this.snackBar.open(error.Message, "OK");
      }
      return;
    }

    const dialogData = new RoomSelectionDialogData(
      selectedCourseEdition.CourseEdition,
      [selectedCourseEdition.Day,selectedCourseEdition.PeriodIndex],
      [selectedCourseEdition.Day,selectedCourseEdition.PeriodIndex],
      selectedCourseEdition.CourseEdition.Weeks!,
      this.scheduleDayLabels,
      this.scheduleTimeLabels,
      this.roomTypes,
      true,
      true,
      this.account.UserId
    );

    this.currentRoomSelectionDialog = this.dialog.open(RoomSelectionComponent, {
      disableClose: true,
      data: dialogData
    });
    const dialogResult:RoomSelectionDialogResult = await this.currentRoomSelectionDialog.afterClosed().toPromise();
    this.currentRoomSelectionDialog = null;

    switch (dialogResult.Status) {
      case RoomSelectionDialogStatus.ACCEPTED: {
        this.currentSelectedCourseEdition.CourseEdition.Room = dialogResult.Room;
        this.currentSelectedCourseEdition.CourseEdition.Weeks = dialogResult.Weeks;
      } break;
      case RoomSelectionDialogStatus.SCHEDULED: {

      } break;
      case RoomSelectionDialogStatus.CANCELED: {
        if (dialogResult.Message != undefined) {
          this.snackBar.open(dialogResult.Message, "OK");
        }
      } break;
      case RoomSelectionDialogStatus.FAILED: {
        if (dialogResult.Message != undefined) {
          this.snackBar.open(dialogResult.Message, "OK");
        }
      } break;
    }
    if (dialogResult.Status != RoomSelectionDialogStatus.ACCEPTED) {
      try {
        const result = await this.signalrService.UnlockSchedulePositions(
          selectedCourseEdition.CourseEdition.Room?.RoomId!, selectedCourseEdition.PeriodIndex + 1,
          selectedCourseEdition.Day + 1, selectedCourseEdition.CourseEdition.Weeks!
        ).toPromise();
        
        if (result.StatusCode >= 400) {
          throw result;
        }
        
        this.currentSelectedCourseEdition.CourseEdition.Locked = false;
      } catch (error) {
  
      }
    } else {
      this.currentSelectedCourseEdition.CourseEdition.Locked = false;
    }
  }

  async ShowScheduledChanges() {
    if (this.currentSelectedCourseEdition == null) {
      return;
    }
    const selectedCourseEdition = this.currentSelectedCourseEdition;

    const dialogData = new ScheduledChangesDialogData(
      selectedCourseEdition.CourseEdition,
      [selectedCourseEdition.Day,selectedCourseEdition.PeriodIndex],
      this.scheduleDayLabels,
      this.scheduleTimeLabels,
      this.roomTypes,
      this.settings
    );

    this.currentScheduledChangesDialog = this.dialog.open(ScheduledChangesViewComponent, {
      disableClose: true,
      data: dialogData
    });
    const dialogResult:ScheduledChangesDialogResult = await this.currentScheduledChangesDialog.afterClosed().toPromise();
    this.currentScheduledChangesDialog = null;
    if (this.currentSelectedCourseEdition != null) {
      this.currentSelectedCourseEdition.CanShowScheduledChanges 
        = this.currentSelectedCourseEdition.CourseEdition.ScheduledMoves.length > 0;
    }

    if (dialogResult.Message != undefined) {
      this.snackBar.open(dialogResult.Message, "OK");
    }
  }

  async Move() {
    if (this.currentSelectedCourseEdition == null) {
      return;
    }

    if (this.currentSelectedCourseEdition.IsMoving) {
      try {
        const selectedCourseEdition = this.currentSelectedCourseEdition;
        const result = await this.signalrService.UnlockSchedulePositions(
          selectedCourseEdition.CourseEdition.Room?.RoomId!, selectedCourseEdition.PeriodIndex + 1,
          selectedCourseEdition.Day + 1, selectedCourseEdition.CourseEdition.Weeks!
        ).toPromise();
        
        if (result.StatusCode >= 400) {
          throw result;
        }
        
        this.currentSelectedCourseEdition.CourseEdition.Locked = false;
      } catch (error) {
  
      }
      
      this.currentSelectedCourseEdition.IsMoving = false;
      
      const numberOfSlots = this.settings.Periods.length - 1;
      for (let i = 0; i < this.scheduleSlots.length; ++i) {
        this.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = false;
      }
      this.areSlotsValiditySet = false;
      return;
    }

    const selectedCourseEdition = this.currentSelectedCourseEdition;
    try {
      const result = await this.signalrService.LockSchedulePositions(
        selectedCourseEdition.CourseEdition.Room!.RoomId, selectedCourseEdition.PeriodIndex + 1,
        selectedCourseEdition.Day + 1, selectedCourseEdition.CourseEdition.Weeks!
      ).toPromise();
      
      if (result.StatusCode >= 400) {
        throw result;
      }
      
      this.currentSelectedCourseEdition.CourseEdition.Locked = true;
    } catch (error:any) {
      if (error.Message != undefined) {
        this.snackBar.open(error.Message, "OK");
      }
      return;
    }
    
    let busySlots = await this.scheduleDesignerApiService.GetBusyPeriods(
      selectedCourseEdition.CourseEdition.CourseId, 
      selectedCourseEdition.CourseEdition.CourseEditionId,
      this.weeks[this.currentTabIndex]
    ).toPromise();
    
    const numberOfSlots = this.settings.Periods.length - 1;
    let busySlotIndex = 0;
    
    for (let i = 0; i < this.scheduleSlots.length; ++i) {
      if (i != (busySlots[busySlotIndex]?.Day - 1) * numberOfSlots + (busySlots[busySlotIndex]?.PeriodIndex - 1)) {
        this.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = true;
      } else {
        this.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = false;
        ++busySlotIndex;
      }
    }

    this.areSlotsValiditySet = true;

    this.currentSelectedCourseEdition.IsMoving = true;
  }

  CancelSelection() {
    if (this.currentSelectedCourseEdition != null) {
      this.currentSelectedCourseEdition.CourseEdition.IsCurrentlyActive = false;
    }
    this.currentSelectedCourseEdition = null;
  }

  OnMouseEnter(day:number, periodIndex:number) {
    if (this.currentSelectedCourseEdition?.IsMoving) {
      this.currentDropContainerIndexes = [day,periodIndex];
      if (this.schedule[day][periodIndex].length > 0) {
        this.isMoveValid = null;
      } else {
        this.isMoveValid = this.scheduleSlotsValidity[day][periodIndex];
      }
    }
  }

  async SelectRoom(day:number, periodIndex:number) {
    if (!this.currentSelectedCourseEdition?.IsMoving || !this.areSlotsValiditySet) {
      return;
    }
    
    if (this.schedule[day][periodIndex].length > 0) {
      return;
    }

    this.isMoveValid = this.scheduleSlotsValidity[day][periodIndex];
    
    const selectedCourseEdition = this.currentSelectedCourseEdition;
    const dialogData = new RoomSelectionDialogData(
      selectedCourseEdition.CourseEdition,
      [selectedCourseEdition.Day,selectedCourseEdition.PeriodIndex],
      [day,periodIndex],
      this.weeks[this.currentTabIndex],
      this.scheduleDayLabels,
      this.scheduleTimeLabels,
      this.roomTypes,
      this.isMoveValid!,
      true,
      this.account.UserId
    );

    this.currentRoomSelectionDialog = this.dialog.open(RoomSelectionComponent, {
      disableClose: true,
      data: dialogData
    });
    const dialogResult:RoomSelectionDialogResult = await this.currentRoomSelectionDialog.afterClosed().toPromise();
    this.currentRoomSelectionDialog = null;

    switch (dialogResult.Status) {
      case RoomSelectionDialogStatus.ACCEPTED: {
        //remove old
        if (this.weeks[this.currentTabIndex].sort((a,b) => a - b).join(',') 
          === this.currentSelectedCourseEdition.CourseEdition.Weeks?.sort((a,b) => a - b).join(',')) {
            
            const selectedDay = this.currentSelectedCourseEdition.Day;
            const selectedPeriodIndex = this.currentSelectedCourseEdition.PeriodIndex;
            const selectedCourseEdition = this.currentSelectedCourseEdition.CourseEdition;
            
            const existingSrcCourseEditions = this.schedule[selectedDay][selectedPeriodIndex].filter((courseEdition) => 
            selectedCourseEdition.CourseId == courseEdition.CourseId 
                && selectedCourseEdition.CourseEditionId == courseEdition.CourseEditionId
                && selectedCourseEdition.Room!.RoomId == courseEdition.Room!.RoomId
            );
            
            if (existingSrcCourseEditions.length > 0) {
              existingSrcCourseEditions[0].Weeks = existingSrcCourseEditions[0].Weeks
                ?.filter(week => !selectedCourseEdition.Weeks?.includes(week)) ?? [];
              
              if (existingSrcCourseEditions[0].Weeks.length == 0) {
                this.schedule[selectedDay][selectedPeriodIndex] 
                  = this.schedule[selectedDay][selectedPeriodIndex].filter(courseEdition => courseEdition.Weeks != null 
                    && courseEdition.Weeks.length > 0);
              }
            }
        }
        
        this.currentSelectedCourseEdition.CourseEdition.Room = dialogResult.Room;
        this.currentSelectedCourseEdition.CourseEdition.Weeks = dialogResult.Weeks;
        this.currentSelectedCourseEdition.CourseEdition.ScheduledMoves = [];

        //add new
        this.schedule[day][periodIndex].push(this.currentSelectedCourseEdition.CourseEdition);
      } break;
      case RoomSelectionDialogStatus.SCHEDULED: {

      } break;
      case RoomSelectionDialogStatus.CANCELED: {
        if (dialogResult.Message != undefined) {
          this.snackBar.open(dialogResult.Message, "OK");
        }
      } break;
      case RoomSelectionDialogStatus.FAILED: {
        if (dialogResult.Message != undefined) {
          this.snackBar.open(dialogResult.Message, "OK");
        }
      } break;
    }
    if (dialogResult.Status != RoomSelectionDialogStatus.ACCEPTED) {
      try {
        const result = await this.signalrService.UnlockSchedulePositions(
          selectedCourseEdition.CourseEdition.Room?.RoomId!, selectedCourseEdition.PeriodIndex + 1,
          selectedCourseEdition.Day + 1, selectedCourseEdition.CourseEdition.Weeks!
        ).toPromise();
        
        if (result.StatusCode >= 400) {
          throw result;
        }
        
        this.currentSelectedCourseEdition.CourseEdition.Locked = false;
      } catch (error) {
  
      }
    } else {
      this.currentSelectedCourseEdition.CourseEdition.Locked = false;
    }
    
    this.isMoveValid = null;
    this.currentSelectedCourseEdition.IsMoving = false;
    const numberOfSlots = this.settings.Periods.length - 1;
    for (let i = 0; i < this.scheduleSlots.length; ++i) {
      this.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = false;
    }
    this.areSlotsValiditySet = false;
  }

  OnMouseLeave() {
    if (this.currentSelectedCourseEdition?.IsMoving) {
      this.isMoveValid = null;
      this.currentDropContainerIndexes = [-1,-1];
    }
  }
}
