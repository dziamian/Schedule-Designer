import { CdkDragDrop, CdkDragEnter, CdkDragRelease, CdkDragStart, transferArrayItem } from '@angular/cdk/drag-drop';
import { ElementRef, Injectable } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { skip } from 'rxjs/operators';
import { AddRoomSelectionComponent } from 'src/app/components/add-room-selection/add-room-selection.component';
import { MyCoursesComponent } from 'src/app/components/my-courses/my-courses.component';
import { RoomSelectionComponent } from 'src/app/components/room-selection/room-selection.component';
import { ScheduleComponent } from 'src/app/components/schedule/schedule.component';
import { ScheduledChangesViewComponent } from 'src/app/components/scheduled-changes-view/scheduled-changes-view.component';
import { AddedSchedulePositions, MessageObject, ModifiedSchedulePositions, RemovedSchedulePositions, SchedulePosition } from 'src/app/others/CommunicationObjects';
import { CourseEdition } from 'src/app/others/CourseEdition';
import { AddRoomSelectionDialogData, AddRoomSelectionDialogResult } from 'src/app/others/dialogs/AddRoomSelectionDialog';
import { RoomSelectionDialogData, RoomSelectionDialogResult, RoomSelectionDialogStatus } from 'src/app/others/dialogs/RoomSelectionDialog';
import { ScheduledChangesDialogData, ScheduledChangesDialogResult } from 'src/app/others/dialogs/ScheduledChangesDialog';
import { Filter } from 'src/app/others/Filter';
import { ModifyingScheduleData } from 'src/app/others/ModifyingScheduleData';
import { SelectedCourseEdition } from 'src/app/others/SelectedCourseEdition';
import { Settings } from 'src/app/others/Settings';
import { RoomType } from 'src/app/others/Types';
import { ScheduleDesignerApiService } from '../ScheduleDesignerApiService/schedule-designer-api.service';
import { SignalrService } from '../SignalrService/signalr.service';

@Injectable({
  providedIn: 'root'
})
export class ScheduleInteractionService {

  constructor(
    private scheduleDesignerApiService: ScheduleDesignerApiService,
    private signalrService: SignalrService,
  ) { }

  private getIndexes(id: string): number[] {
    const indexes = id.split(',');
    const parsedFirst = Number.parseInt(indexes[0]);
    const parsedSecond = Number.parseInt(indexes[1]);
    return [
      isNaN(parsedFirst) ? -1 : parsedFirst,
      isNaN(parsedSecond) ? -1 : parsedSecond,
    ];
  }

  public updateBusyPeriods(
    data: ModifyingScheduleData, 
    tabWeeks: number[][], 
    currentTabIndex: number, 
    settings: Settings, 
    scheduleComponent: ScheduleComponent
  ): void {
    if (data.currentSelectedCourseEdition != null && data.currentSelectedCourseEdition.IsMoving) {
      data.areSlotsValiditySet = false;
      
      const numberOfSlots = settings.Periods.length - 1;
      for (let i = 0; i < scheduleComponent.scheduleSlots.length; ++i) {
        data.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = false;
      }
      
      this.scheduleDesignerApiService.GetBusyPeriods(
        data.currentSelectedCourseEdition.CourseEdition.CourseId, 
        data.currentSelectedCourseEdition.CourseEdition.CourseEditionId,
        tabWeeks[currentTabIndex]
      ).subscribe(busySlots => {
        const numberOfSlots = settings.Periods.length - 1;
        let busySlotIndex = 0;
        
        for (let i = 0; i < scheduleComponent.scheduleSlots.length; ++i) {
          if (i != (busySlots[busySlotIndex]?.Day - 1) * numberOfSlots + (busySlots[busySlotIndex]?.PeriodIndex - 1)) {
            data.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = true;
          } else {
            data.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = false;
            ++busySlotIndex;
          }
        }

        data.areSlotsValiditySet = true;
      });
    }
  }

  public selectCourseIfPossible(
    data: ModifyingScheduleData, 
    tabWeeks: number[][], 
    currentTabIndex: number,
    scheduleComponent: ScheduleComponent
  ): void {
    if (data.currentSelectedCourseEdition != null) {
      if (tabWeeks[currentTabIndex].sort((a,b) => a - b).join(',') 
        === data.currentSelectedCourseEdition.CourseEdition.Weeks?.sort((a,b) => a - b).join(',')) {
          scheduleComponent.schedule[data.currentSelectedCourseEdition.Day][data.currentSelectedCourseEdition.PeriodIndex]
            .forEach((courseEdition) => {
              if (data.currentSelectedCourseEdition?.CourseEdition.CourseId == courseEdition.CourseId
                && data.currentSelectedCourseEdition?.CourseEdition.CourseEditionId == courseEdition.CourseEditionId
                && data.currentSelectedCourseEdition?.CourseEdition.Room?.RoomId == courseEdition.Room?.RoomId) {
                  data.currentSelectedCourseEdition.CourseEdition = courseEdition;
                  data.currentSelectedCourseEdition.CourseEdition.IsCurrentlyActive = true;
                  data.currentSelectedCourseEdition.CanChangeRoom = true;
                  data.currentSelectedCourseEdition.CanMakeMove = true;
              }
            });
      } else {
        data.currentSelectedCourseEdition.CanChangeRoom = false;
        if (tabWeeks[currentTabIndex].length == data.currentSelectedCourseEdition.CourseEdition.Weeks?.length) {
          data.currentSelectedCourseEdition.CanMakeMove = true;
        }
      }
    }
  }

  public updateLockInMyCourses(
    courseId: number,
    courseEditionId: number,
    data: ModifyingScheduleData,
    loading: boolean,
    snackBar: MatSnackBar
  ) {
    if (loading) {
      return;
    }

    //possibly admin took control
    const currentDrag = data.currentDragEvent?.source;
    if (currentDrag != null && currentDrag.dropContainer.id === 'my-courses') {
      if (currentDrag.data.CourseId == courseId 
        && currentDrag.data.CourseEditionId == courseEditionId) {
          if (data.currentDragEvent != null) {
            data.currentDragEvent.source.dropContainer._dropListRef.enter(
              data.currentDragEvent.source._dragRef,
              0,0
            );
            document.dispatchEvent(new Event('mouseup'));
            snackBar.open("Someone took control of your course.", "OK");
          }
      }
    }

    //possibly admin took control
    const currentDialogData = data.currentRoomSelectionDialog?.componentInstance.data;
    if (currentDialogData != null) {
      const currentIndexes = currentDialogData.SrcIndexes;
      if ((currentIndexes == null || currentIndexes.length == 0 || currentIndexes[0] == -1 || currentIndexes[1] == -1)
        && currentDialogData.CourseEdition.CourseId == courseId
        && currentDialogData.CourseEdition.CourseEditionId == courseEditionId) {
          if (data.currentRoomSelectionDialog != null) {
            const result = RoomSelectionDialogResult.CANCELED;
            result.Message = "Someone took control of your course.";
            data.currentRoomSelectionDialog.close(result);
          }
      }
    }
  }

  public updateLockInSchedule(
    position: SchedulePosition, 
    data: ModifyingScheduleData,
    isModifying: boolean,
    settings: Settings,
    scheduleComponent: ScheduleComponent,
    loading: boolean,
    snackBar: MatSnackBar
  ) {
    if (loading) {
      return;
    }

    const courseId = position.CourseId;
    const courseEditionId = position.CourseEditionId;
    const roomId = position.RoomId;
    const day = position.Day - 1;
    const periodIndex = position.PeriodIndex - 1;
    const weeks = position.Weeks;

    //possibly admin took control
    const currentDrag = data.currentDragEvent?.source;
    if (currentDrag != null) {
      const currentIndexes = (currentDrag.dropContainer.id !== 'my-courses') ? this.getIndexes(currentDrag.dropContainer.id) : [-1,-1];
      if (currentDrag.data.CourseId == courseId 
        && currentDrag.data.CourseEditionId == courseEditionId && currentDrag.data.Room?.RoomId == roomId
        && currentIndexes[1] == periodIndex && currentIndexes[0] == day
        && currentDrag.data.Weeks?.some(c => weeks.includes(c))) {
          if (data.currentDragEvent != null) {
            data.currentDragEvent.source.dropContainer._dropListRef.enter(
              data.currentDragEvent.source._dragRef,
              0,0
            );
            document.dispatchEvent(new Event('mouseup'));
            snackBar.open("Someone took control of your course.", "OK");
          }
      }
    }

    //possibly admin took control
    const selectedCourseEdition = data.currentSelectedCourseEdition;
    if (isModifying && selectedCourseEdition != null 
      && selectedCourseEdition.CourseEdition.CourseId == courseId
      && selectedCourseEdition.CourseEdition.CourseEditionId == courseEditionId
      && selectedCourseEdition.CourseEdition.Room?.RoomId == roomId
      && selectedCourseEdition.Day == day && selectedCourseEdition.PeriodIndex == periodIndex
      && selectedCourseEdition.CourseEdition.Weeks?.some(r => weeks.includes(r))) {
        this.cancelMove(data, settings, scheduleComponent);
    }

    //possibly admin took control
    const currentDialogData = data.currentRoomSelectionDialog?.componentInstance.data;
    if (currentDialogData != null) {
      const currentIndexes = currentDialogData.SrcIndexes;
      if (currentDialogData.CourseEdition.CourseId == courseId
        && currentDialogData.CourseEdition.CourseEditionId == courseEditionId && currentDialogData.CourseEdition.Room?.RoomId == roomId
        && currentIndexes[1] == periodIndex && currentIndexes[0] == day
        && currentDialogData.CourseEdition.Weeks?.some(c => weeks.includes(c))) {
          if (data.currentRoomSelectionDialog != null) {
            const result = RoomSelectionDialogResult.CANCELED;
            result.Message = "Someone took control of your course.";
            data.currentRoomSelectionDialog.close(result);
          }
      }
    }
  }

  public lastAddedSchedulePositionsReaction(
    addedSchedulePositions: AddedSchedulePositions,
    data: ModifyingScheduleData,
    tabWeeks: number[][],
    currentTabIndex: number,
    loading: boolean
  ) {
    if (loading) {
        return;
      }
      
      const schedulePosition = addedSchedulePositions.SchedulePosition;
      const commonWeeks = schedulePosition.Weeks.filter(week => tabWeeks[currentTabIndex].includes(week));

      //active dragged and selected fields update
      const event = data.currentDragEvent?.source;
      const item = (event != undefined) ? data.currentDragEvent?.source.data : data.currentSelectedCourseEdition?.CourseEdition;

      if (item == undefined || commonWeeks.length == 0) {
        return;
      }

      if (item.Coordinators.map(c => c.User.UserId).some(c => addedSchedulePositions.CoordinatorsIds.includes(c))
        || item.Groups.map(g => g.GroupId).some(g => addedSchedulePositions.GroupsIds.includes(g))) {
          if (data.currentDragEvent != null || data.currentSelectedCourseEdition?.IsMoving) {
            data.scheduleSlotsValidity[schedulePosition.Day - 1][schedulePosition.PeriodIndex - 1] = false;
          }
          
          if (event?.dropContainer.id === 'my-courses') {
            const connectedTo = event.dropContainer.connectedTo as string[];
            const id = `${schedulePosition.Day - 1},${schedulePosition.PeriodIndex - 1}`;
            if (data.currentDragEvent != null) {
              data.currentDragEvent.source.dropContainer.connectedTo = connectedTo.filter(e => e !== id);
              event.dropContainer._dropListRef.enter(
                event._dragRef,
                0,0
              );
            }
          }
          else if (event != undefined && data.currentDropContainerIndexes[0] !== -1 && data.currentDropContainerIndexes[1] !== -1) {
            if (data.currentDropContainerIndexes[0] == schedulePosition.Day - 1 
              && data.currentDropContainerIndexes[1] == schedulePosition.PeriodIndex - 1) {
                data.isCurrentMoveValid = false;
            }
          }
      }
  }

  public lastModifiedSchedulePositionsReaction(
    modifiedSchedulePositions: ModifiedSchedulePositions,
    data: ModifyingScheduleData,
    tabWeeks: number[][],
    currentTabIndex: number,
    settings: Settings,
    scheduleComponent: ScheduleComponent,
    loading: boolean,
    snackBar: MatSnackBar
  ) {
    if (loading) {
      return;
    }

    const srcSchedulePosition = modifiedSchedulePositions.SourceSchedulePosition;
    const dstSchedulePosition = modifiedSchedulePositions.DestinationSchedulePosition;
    const commonWeeks = dstSchedulePosition.Weeks.filter(week => tabWeeks[currentTabIndex].includes(week));

    //scheduled change occurred
    const currentDrag = data.currentDragEvent?.source;
    if (currentDrag != null) {
      const currentIndexes = (currentDrag.dropContainer.id !== 'my-courses') ? this.getIndexes(currentDrag.dropContainer.id) : [-1,-1];
      if (currentDrag.data.CourseId == srcSchedulePosition.CourseId 
        && currentDrag.data.CourseEditionId == srcSchedulePosition.CourseEditionId && currentDrag.data.Room?.RoomId == srcSchedulePosition.RoomId
        && currentIndexes[1] == srcSchedulePosition.PeriodIndex - 1 && currentIndexes[0] == srcSchedulePosition.Day - 1
        && currentDrag.data.Weeks?.some(c => srcSchedulePosition.Weeks.includes(c))) {
          if (data.currentDragEvent != null) {
            data.currentDragEvent.source.dropContainer._dropListRef.enter(
              data.currentDragEvent.source._dragRef,
              0,0
            );
            document.dispatchEvent(new Event('mouseup'));
            snackBar.open("System took control of your course.", "OK");
          }
      }
    }

    //scheduled change occurred
    const selectedCourseEdition = data.currentSelectedCourseEdition;
    if (selectedCourseEdition != null) {
      if (selectedCourseEdition.CourseEdition.CourseId == srcSchedulePosition.CourseId
        && selectedCourseEdition.CourseEdition.CourseEditionId == srcSchedulePosition.CourseEditionId 
        && selectedCourseEdition.CourseEdition.Room?.RoomId == srcSchedulePosition.RoomId
        && selectedCourseEdition.PeriodIndex == srcSchedulePosition.PeriodIndex - 1 && selectedCourseEdition.Day == srcSchedulePosition.Day - 1
        && selectedCourseEdition.CourseEdition.Weeks?.some(c => srcSchedulePosition.Weeks.includes(c))) {
          if (data.currentSelectedCourseEdition != null) {
            data.currentSelectedCourseEdition.IsMoving = false;
            data.currentSelectedCourseEdition.CourseEdition.IsCurrentlyActive = false;
          }
          data.currentSelectedCourseEdition = null;
          
          const numberOfSlots = settings.Periods.length - 1;
          for (let i = 0; i < scheduleComponent.scheduleSlots.length; ++i) {
            data.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = false;
          }
        }
    }

    //scheduled change occurred
    const currentDialogData = data.currentRoomSelectionDialog?.componentInstance.data;
    if (currentDialogData != null) {
      const currentIndexes = currentDialogData.SrcIndexes;
      if (currentDialogData.CourseEdition.CourseId == srcSchedulePosition.CourseId
        && currentDialogData.CourseEdition.CourseEditionId == srcSchedulePosition.CourseEditionId && currentDialogData.CourseEdition.Room?.RoomId == srcSchedulePosition.RoomId
        && currentIndexes[1] == srcSchedulePosition.PeriodIndex - 1 && currentIndexes[0] == srcSchedulePosition.Day - 1
        && currentDialogData.CourseEdition.Weeks?.some(c => srcSchedulePosition.Weeks.includes(c))) {
          if (data.currentRoomSelectionDialog != null) {
            const result = RoomSelectionDialogResult.CANCELED;
            result.Message = "System took control of your course."
            data.currentRoomSelectionDialog.close(result);
          }
      }
    }

    //active dragged and selected fields update
    const event = data.currentDragEvent?.source;
    const item = (event != undefined) ? data.currentDragEvent?.source.data : data.currentSelectedCourseEdition?.CourseEdition;
    
    if (item == undefined || commonWeeks.length == 0) {
      return;
    }

    if (item.Coordinators.map(c => c.User.UserId).some(c => modifiedSchedulePositions.CoordinatorsIds.includes(c))
      || item.Groups.map(g => g.GroupId).some(g => modifiedSchedulePositions.GroupsIds.includes(g))) {
      if (data.currentDragEvent != null || data.currentSelectedCourseEdition?.IsMoving) {
        data.scheduleSlotsValidity[dstSchedulePosition.Day - 1][dstSchedulePosition.PeriodIndex - 1] = false;
      }
      
      
      if (event?.dropContainer.id === 'my-courses') {
        const connectedTo = event.dropContainer.connectedTo as string[];
        const id = `${dstSchedulePosition.Day - 1},${dstSchedulePosition.PeriodIndex - 1}`;
        if (data.currentDragEvent != null) {
          data.currentDragEvent.source.dropContainer.connectedTo = connectedTo.filter(e => e !== id);
          event.dropContainer._dropListRef.enter(
            event._dragRef,
            0,0
          );
        }
      }
      else if (event != undefined && data.currentDropContainerIndexes[0] !== -1 && data.currentDropContainerIndexes[1] !== -1) {
        if (data.currentDropContainerIndexes[0] == dstSchedulePosition.Day - 1 
          && data.currentDropContainerIndexes[1] == dstSchedulePosition.PeriodIndex - 1) {
            data.isCurrentMoveValid = false;
        }
      }

      this.scheduleDesignerApiService.IsPeriodBusy(
        srcSchedulePosition.CourseId, srcSchedulePosition.CourseEditionId,
        srcSchedulePosition.PeriodIndex, srcSchedulePosition.Day,
        srcSchedulePosition.Weeks.filter(week => tabWeeks[currentTabIndex].includes(week))
      ).subscribe((isBusy) => {
        if (data.currentDragEvent != null || data.currentSelectedCourseEdition?.IsMoving) {
          data.scheduleSlotsValidity[srcSchedulePosition.Day - 1][srcSchedulePosition.PeriodIndex - 1] = !isBusy;
        }

        if (isBusy) {
          return;
        }

        if (event?.dropContainer.id === 'my-courses') {
          const connectedTo = event.dropContainer.connectedTo as string[];
          const id = `${srcSchedulePosition.Day - 1},${srcSchedulePosition.PeriodIndex - 1}`;
          if (data.currentDragEvent != null) {
            connectedTo.push(id);
            data.currentDragEvent.source.dropContainer.connectedTo = connectedTo;
            event.dropContainer._dropListRef.enter(
              event._dragRef,
              0,0
            );
          }
        } else if (event != undefined && data.currentDropContainerIndexes[0] !== -1 && data.currentDropContainerIndexes[1] !== -1) {
          if (data.currentDropContainerIndexes[0] == srcSchedulePosition.Day - 1 
            && data.currentDropContainerIndexes[1] == srcSchedulePosition.PeriodIndex - 1) {
              data.isCurrentMoveValid = true;
          }
        }
      });
    }
  }

  public lastRemovedSchedulePositionsReaction(
    removedSchedulePositions: RemovedSchedulePositions,
    data: ModifyingScheduleData,
    tabWeeks: number[][],
    currentTabIndex: number,
    loading: boolean
  ) {
    if (loading) {
      return;
    }

    const schedulePosition = removedSchedulePositions.SchedulePosition;
    const commonWeeks = schedulePosition.Weeks.filter(week => tabWeeks[currentTabIndex].includes(week));

    //active dragged and selected fields update
    const item = data.currentDragEvent?.source.data;
    const event = data.currentDragEvent?.source;
    if (item == undefined || commonWeeks.length == 0) {
      return;
    }

    if (item.Coordinators.map(c => c.User.UserId).some(c => removedSchedulePositions.CoordinatorsIds.includes(c))
      || item.Groups.map(g => g.GroupId).some(g => removedSchedulePositions.GroupsIds.includes(g))) {

      this.scheduleDesignerApiService.IsPeriodBusy(
        schedulePosition.CourseId, schedulePosition.CourseEditionId,
        schedulePosition.PeriodIndex, schedulePosition.Day,
        schedulePosition.Weeks.filter(week => tabWeeks[currentTabIndex].includes(week))
      ).subscribe((isBusy) => {
        if (data.currentDragEvent != null || data.currentSelectedCourseEdition?.IsMoving) {
          data.scheduleSlotsValidity[schedulePosition.Day - 1][schedulePosition.PeriodIndex - 1] = !isBusy;
        }

        if (isBusy) {
          return;
        }

        if (event?.dropContainer.id === 'my-courses') {
          const connectedTo = event.dropContainer.connectedTo as string[];
          const id = `${schedulePosition.Day - 1},${schedulePosition.PeriodIndex - 1}`;
          if (data.currentDragEvent != null) {
            connectedTo.push(id);
            data.currentDragEvent.source.dropContainer.connectedTo = connectedTo;
            event.dropContainer._dropListRef.enter(
              event._dragRef,
              0,0
            );
          }
        } else if (event != undefined && data.currentDropContainerIndexes[0] !== -1 && data.currentDropContainerIndexes[1] !== -1) {
          if (data.currentDropContainerIndexes[0] == schedulePosition.Day - 1 
            && data.currentDropContainerIndexes[1] == schedulePosition.PeriodIndex - 1) {
              data.isCurrentMoveValid = true;
          }
        }
        
      });
    }
  }

  public ModifySchedule(
    data: ModifyingScheduleData,
    isModifying: boolean,
  ): boolean {
    if (data.currentSelectedCourseEdition != null) {
      if (data.currentSelectedCourseEdition.IsMoving) {
        return isModifying;
      }
      data.currentSelectedCourseEdition.CourseEdition.IsCurrentlyActive = false;
      data.currentSelectedCourseEdition = null;
    }
    isModifying = !isModifying;
    return isModifying;
  }

  public async onMyCoursesDrop(
    event: CdkDragDrop<CourseEdition[], CourseEdition[], CourseEdition>,
    data: ModifyingScheduleData,
    tabWeeks: number[][],
    currentTabIndex: number,
    myCoursesComponent: MyCoursesComponent,
    scheduleComponent: ScheduleComponent,
    snackBar: MatSnackBar,
  ): Promise<void> {
    if (data.isCurrentDragCanceled) {
      data.isCurrentDragCanceled = false;
      data.currentDragEvent = null;
      if (data.currentSelectedCourseEdition != null) {
        data.currentSelectedCourseEdition.CourseEdition.IsCurrentlyActive = false;
      }
      data.currentSelectedCourseEdition = null;
      data.isCurrentMoveValid = null;
      event.item.data.IsCurrentlyActive = false;
      return;
    }

    const courseEdition = event.item.data;
    const previousContainer = event.previousContainer;
    const currentContainer = event.container;
    const weeks = tabWeeks[currentTabIndex];
    const isScheduleSource = event.previousContainer.id !== 'my-courses';
    const previousIndexes = this.getIndexes(previousContainer.id);

    var status = true;
    if (isScheduleSource) {
      try {
        const result = await new Promise<MessageObject>((resolve, reject) => {
          const responseSubscription = this.signalrService.lastResponse.pipe(skip(1))
          .subscribe((messageObject) => {
            responseSubscription.unsubscribe();
            resolve(messageObject);
          },(errorObject) => {
            reject(errorObject);
          });
          this.signalrService.RemoveSchedulePositions(
            courseEdition.Room!.RoomId, previousIndexes[1] + 1,
            previousIndexes[0] + 1, weeks
          );
          
          setTimeout(() => {
            responseSubscription.unsubscribe(); 
            const errorObject = new MessageObject(400);
            errorObject.Message = "Request timeout.";
            reject(errorObject);
          }, 15000);
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

        myCoursesComponent.myCourses.forEach((element) => {
          if (element.CourseId == courseEdition.CourseId 
            && element.CourseEditionId == courseEdition.CourseEditionId) {
              element.ScheduleAmount -= weeks.length;
          }
        });

        for (let i = 0; i < scheduleComponent.schedule.length; ++i) {
          for (let j = 0; j < scheduleComponent.schedule[i].length; ++j) {
            for (let k = 0; k < scheduleComponent.schedule[i][j].length; ++j) {
              const currentCourseEdition = scheduleComponent.schedule[i][j][k];
              if (currentCourseEdition.CourseId == courseEdition.CourseId
                && currentCourseEdition.CourseEditionId == courseEdition.CourseEditionId) {
                  currentCourseEdition.ScheduleAmount -= weeks.length;
              }
            }
          }
        }
      }
      catch (error:any) {
        status = false;
        if (error.Message != undefined) {
          snackBar.open(error.Message, "OK");
        }
      }
    }
    
    if (data.currentSelectedCourseEdition != null) {
      data.currentSelectedCourseEdition.CourseEdition.IsCurrentlyActive = false;
    }
    data.currentSelectedCourseEdition = null;

    if (!isScheduleSource) {
      try {
        const result = await this.signalrService.UnlockCourseEdition(
          courseEdition.CourseId, courseEdition.CourseEditionId
        ).toPromise();
        
        if (result.StatusCode >= 400) {
          throw result;
        }
        
        courseEdition.IsLocked = false;
        courseEdition.IsLockedByAdmin = false;

        const myCoursesLength = myCoursesComponent.myCourses.length;
        for (var i = 0; i < myCoursesLength; ++i) {
          const currentCourseEdition = myCoursesComponent.myCourses[i];
          if (currentCourseEdition.CourseId == courseEdition.CourseId 
            && currentCourseEdition.CourseEditionId == courseEdition.CourseEditionId) {
              currentCourseEdition.IsLocked = false;
              currentCourseEdition.IsLockedByAdmin = false;
          }
        }
      } catch (error) {
        
      }
    } else if (!status) {
      try {
        const result = await this.signalrService.UnlockSchedulePositions(
          courseEdition.Room!.RoomId, previousIndexes[1] + 1,
          previousIndexes[0] + 1, weeks
        ).toPromise();
        
        if (result.StatusCode >= 400) {
          throw result;
        }
        
        courseEdition.IsLocked = false;
        courseEdition.IsLockedByAdmin = false;
      } catch (error) {

      }
    } else {
      try {
        const result = await this.scheduleDesignerApiService.IsCourseEditionLocked(
          courseEdition.CourseId, courseEdition.CourseEditionId
        ).toPromise();

        courseEdition.IsLocked = result.value;
        courseEdition.IsLockedByAdmin = result.byAdmin;
      }
      catch (error) {

      }
    }
    data.currentDragEvent = null;
    data.isCurrentMoveValid = null;
    event.item.data.IsCurrentlyActive = false;
  }

  public onMyCoursesEnter(
    drag: CdkDragEnter<CourseEdition[]>,
    data: ModifyingScheduleData,
  ): void {
    data.isCurrentMoveValid = null;
    data.currentDropContainerIndexes = [-1, -1];
  }

  public async onMyCoursesStart(
    event: CdkDragStart<CourseEdition>,
    data: ModifyingScheduleData,
    tabWeeks: number[][],
    currentTabIndex: number,
    isAdmin: boolean,
    settings: Settings,
    myCoursesComponent: MyCoursesComponent,
    scheduleComponent: ScheduleComponent,
    snackBar: MatSnackBar,
  ): Promise<void> {
    data.isCurrentDragReleased = false;
    data.areSlotsValiditySet = false;
    data.currentDragEvent = event;
    event.source.data.IsCurrentlyActive = true;
    
    const courseEdition = event.source.data;
    const dropContainer = event.source.dropContainer;

    if (data.currentSelectedCourseEdition != null) {
      data.currentSelectedCourseEdition.CourseEdition.IsCurrentlyActive = false;
    }
    data.currentSelectedCourseEdition = new SelectedCourseEdition(courseEdition, -1, -1);
    data.currentSelectedCourseEdition.CanShowScheduledChanges = courseEdition.ScheduledMoves.length > 0;

    try {
      if (!data.isCurrentDragReleased) {
        const result = await this.signalrService.LockCourseEdition(
          courseEdition.CourseId, courseEdition.CourseEditionId
        ).toPromise();
        
        if (result.StatusCode >= 400) {
          throw result;
        }
        
        courseEdition.IsLocked = true;
        courseEdition.IsLockedByAdmin = isAdmin;

        const myCoursesLength = myCoursesComponent.myCourses.length;
        for (var i = 0; i < myCoursesLength; ++i) {
          const currentCourseEdition = myCoursesComponent.myCourses[i];
          if (currentCourseEdition.CourseId == courseEdition.CourseId 
            && currentCourseEdition.CourseEditionId == courseEdition.CourseEditionId) {
              currentCourseEdition.IsLocked = true;
              currentCourseEdition.IsLockedByAdmin = isAdmin;
          }
        }
      } else {
        return;
      }
    } catch (error:any) {
      if (!data.isCurrentDragReleased) {
        dropContainer._dropListRef.enter(
          event.source._dragRef,
          0,0
        );
        document.dispatchEvent(new Event('mouseup'));
        data.isCurrentDragReleased = true;
      } else {
        if (data.currentRoomSelectionDialog != null) {
          data.currentRoomSelectionDialog.close(RoomSelectionDialogResult.CANCELED);
        }
      }
      if (error.Message != undefined) {
        snackBar.open(error.Message, "OK");
      } else {
        snackBar.open("You are not authorized to do this.", "OK");
      }

      data.currentDragEvent = null;
      if (data.currentSelectedCourseEdition != null) {
        data.currentSelectedCourseEdition.CourseEdition.IsCurrentlyActive = false;
      }
      data.currentSelectedCourseEdition = null;
      data.isCurrentMoveValid = null;
      event.source.data.IsCurrentlyActive = false;
      return;
    }

    let busySlots = await this.scheduleDesignerApiService.GetBusyPeriods(
      courseEdition.CourseId, 
      courseEdition.CourseEditionId,
      tabWeeks[currentTabIndex]
    ).toPromise();
    let connectedTo = ['my-courses'];
    
    const numberOfSlots = settings.Periods.length - 1;
    let busySlotIndex = 0;
    let scheduleSlots = scheduleComponent.scheduleSlots.toArray();
    
    for (let i = 0; i < scheduleComponent.scheduleSlots.length; ++i) {
      let element = scheduleSlots[i].element as ElementRef<HTMLElement>;
      if (i != (busySlots[busySlotIndex]?.Day - 1) * numberOfSlots + (busySlots[busySlotIndex]?.PeriodIndex - 1)) {
        data.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = true;
        connectedTo.push(element.nativeElement.id);
      } else {
        data.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = false;
        ++busySlotIndex;
      }
    }
    data.areSlotsValiditySet = true;
    dropContainer.connectedTo = connectedTo;
    
    if (!data.isCurrentDragReleased) {
      dropContainer._dropListRef.enter(
        event.source._dragRef, 0, 0
      );
    }
    
    if (data.isCurrentDragReleased) {
      try {
        const result = await this.signalrService.UnlockCourseEdition(
          courseEdition.CourseId, courseEdition.CourseEditionId
        ).toPromise();
        
        if (result.StatusCode >= 400) {
          throw result;
        }
        
        courseEdition.IsLocked = false;
        courseEdition.IsLockedByAdmin = false;

        const myCoursesLength = myCoursesComponent.myCourses.length;
        for (var i = 0; i < myCoursesLength; ++i) {
          const currentCourseEdition = myCoursesComponent.myCourses[i];
          if (currentCourseEdition.CourseId == courseEdition.CourseId 
            && currentCourseEdition.CourseEditionId == courseEdition.CourseEditionId) {
              currentCourseEdition.IsLocked = false;
              currentCourseEdition.IsLockedByAdmin = false;
          }
        }
      } catch (error) {
        
      }
      data.currentDragEvent = null;
      if (data.currentSelectedCourseEdition != null) {
        data.currentSelectedCourseEdition.CourseEdition.IsCurrentlyActive = false;
      }
      data.currentSelectedCourseEdition = null;
      for (let i = 0; i < scheduleComponent.scheduleSlots.length; ++i) {
        data.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = false;
      }
    }
  }

  public async onScheduleDrop(
    event:CdkDragDrop<CourseEdition[], CourseEdition[], CourseEdition>,
    data: ModifyingScheduleData,
    tabWeeks: number[][],
    currentTabIndex: number,
    settings: Settings,
    roomTypes: Map<number, RoomType>,
    isMoveAvailable: boolean,
    isPropositionAvailable: boolean,
    filter: Filter,
    scheduleComponent: ScheduleComponent,
    myCoursesComponent: MyCoursesComponent,
    dialogService: MatDialog,
    snackBar: MatSnackBar
  ): Promise<void> {
    data.isCurrentDragReleased = true;

    if (data.isCurrentDragCanceled) {
      data.isCurrentDragCanceled = false;
      data.currentDragEvent = null;
      if (data.currentSelectedCourseEdition != null) {
        data.currentSelectedCourseEdition.CourseEdition.IsCurrentlyActive = false;
      }
      data.currentSelectedCourseEdition = null;
      data.isCurrentMoveValid = null;
      event.item.data.IsCurrentlyActive = false;
      return;
    }

    const courseEdition = event.item.data;
    const previousContainer = event.previousContainer;
    const currentContainer = event.container;
    const isScheduleSource = previousContainer.id !== 'my-courses';
    const previousIndexes = (isScheduleSource) ? this.getIndexes(previousContainer.id) : [-1,-1];
    const currentIndexes = this.getIndexes(currentContainer.id);
    const weeks = tabWeeks[currentTabIndex];
    
    if (previousContainer === currentContainer || !data.areSlotsValiditySet) {
      try {
        const result = await this.signalrService.UnlockSchedulePositions(
          courseEdition.Room!.RoomId, previousIndexes[1] + 1,
          previousIndexes[0] + 1, weeks
        ).toPromise();
        
        if (result.StatusCode >= 400) {
          throw result;
        }
        
        courseEdition.IsLocked = false;
        courseEdition.IsLockedByAdmin = false;
      } catch (error) {

      }
      data.currentDragEvent = null;
      if (data.currentSelectedCourseEdition != null) {
        data.currentSelectedCourseEdition.CourseEdition.IsCurrentlyActive = false;
      }
      data.currentSelectedCourseEdition = null;
      data.isCurrentMoveValid = null;
      event.item.data.IsCurrentlyActive = false;
      return;
    }
    
    const dialogData = new RoomSelectionDialogData(
      courseEdition,
      previousIndexes,
      currentIndexes,
      weeks,
      settings.DayLabels,
      settings.TimeLabels,
      roomTypes,
      data.isCurrentMoveValid!,
      isScheduleSource,
      isMoveAvailable,
      isPropositionAvailable,
      filter
    );

    data.currentRoomSelectionDialog = dialogService.open(RoomSelectionComponent, {
      disableClose: true,
      data: dialogData
    });
    const dialogResult:RoomSelectionDialogResult = await data.currentRoomSelectionDialog.afterClosed().toPromise();
    data.currentRoomSelectionDialog = null;
    data.isCurrentMoveValid = null;
    data.currentSelectedCourseEdition = null;

    switch (dialogResult.Status) {
      case RoomSelectionDialogStatus.ACCEPTED: {
        courseEdition.Room = dialogResult.Room;
        courseEdition.Weeks = dialogResult.Weeks;
        courseEdition.ScheduledMoves = [];

        if (!isScheduleSource) {
          
          myCoursesComponent.myCourses.forEach((element) => {
            if (element.CourseId == courseEdition.CourseId 
              && element.CourseEditionId == courseEdition.CourseEditionId) {
                element.ScheduleAmount += dialogResult.Weeks.length;
            }
          });

          for (let i = 0; i < scheduleComponent.schedule.length; ++i) {
            for (let j = 0; j < scheduleComponent.schedule[i].length; ++j) {
              for (let k = 0; k < scheduleComponent.schedule[i][j].length; ++j) {
                const currentCourseEdition = scheduleComponent.schedule[i][j][k];
                if (currentCourseEdition.CourseId == courseEdition.CourseId
                  && currentCourseEdition.CourseEditionId == courseEdition.CourseEditionId) {
                    currentCourseEdition.ScheduleAmount += dialogResult.Weeks.length;
                }
              }
            }
          }
        }

        const previousIndex = (isScheduleSource) ? event.previousIndex 
          : myCoursesComponent.myCourses.findIndex(value => value.IsCurrentlyActive);

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
          snackBar.open(dialogResult.Message, "OK");
        }
      } break;
      case RoomSelectionDialogStatus.FAILED: {
        if (dialogResult.Message != undefined) {
          snackBar.open(dialogResult.Message, "OK");
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
        
        courseEdition.IsLocked = false;
        courseEdition.IsLockedByAdmin = false;
        
        const myCoursesLength = myCoursesComponent.myCourses.length;
        for (var i = 0; i < myCoursesLength; ++i) {
          const currentCourseEdition = myCoursesComponent.myCourses[i];
          if (currentCourseEdition.CourseId == courseEdition.CourseId 
            && currentCourseEdition.CourseEditionId == courseEdition.CourseEditionId) {
              currentCourseEdition.IsLocked = false;
              currentCourseEdition.IsLockedByAdmin = false;
          }
        }
      } catch (error) {
  
      }
    } else if (dialogResult.Status == RoomSelectionDialogStatus.ACCEPTED) {
      courseEdition.IsLocked = false;
      courseEdition.IsLockedByAdmin = false;
    } else {
      try {
        const result = await this.signalrService.UnlockSchedulePositions(
          courseEdition.Room!.RoomId, previousIndexes[1] + 1,
          previousIndexes[0] + 1, weeks
        ).toPromise();
        
        if (result.StatusCode >= 400) {
          throw result;
        }
        
        courseEdition.IsLocked = false;
        courseEdition.IsLockedByAdmin = false;
      } catch (error) {
  
      }
    }

    event.item.data.IsCurrentlyActive = false;
  }

  public onScheduleEnter(
    drag: CdkDragEnter<CourseEdition[]>,
    data: ModifyingScheduleData
  ): void {
    const indexes = this.getIndexes(drag.container.id);
    data.isCurrentMoveValid = data.scheduleSlotsValidity[indexes[0]][indexes[1]];
    data.currentDropContainerIndexes = this.getIndexes(drag.container.id);
  }

  public async onScheduleStart(
    event: CdkDragStart<CourseEdition>,
    data: ModifyingScheduleData,
    tabWeeks: number[][],
    currentTabIndex: number,
    isAdmin: boolean,
    isPropositionOnly: boolean,
    settings: Settings,
    scheduleComponent: ScheduleComponent,
    snackBar: MatSnackBar
  ): Promise<void> {
    data.isCurrentDragReleased = false;
    data.areSlotsValiditySet = false;
    data.currentDragEvent = event;
    event.source.data.IsCurrentlyActive = true;
    
    const courseEdition = event.source.data;
    const dropContainer = event.source.dropContainer;
    const indexes = this.getIndexes(dropContainer.id);

    if (data.currentSelectedCourseEdition != null) {
      data.currentSelectedCourseEdition.CourseEdition.IsCurrentlyActive = false;
    }
    data.currentSelectedCourseEdition = new SelectedCourseEdition(courseEdition, indexes[1], indexes[0]);
    data.currentSelectedCourseEdition.CanShowScheduledChanges = courseEdition.ScheduledMoves.length > 0;

    try {
      if (!data.isCurrentDragReleased) {
        const result = await this.signalrService.LockSchedulePositions(
          courseEdition.Room!.RoomId, indexes[1] + 1,
          indexes[0] + 1, tabWeeks[currentTabIndex]
        ).toPromise();
        
        if (result.StatusCode >= 400) {
          throw result;
        }

        courseEdition.IsLocked = true;
        courseEdition.IsLockedByAdmin = isAdmin;
      } else {
        return;
      }
    } catch (error:any) {
      data.isCurrentDragCanceled = true;
      if (!data.isCurrentDragReleased) {
        dropContainer._dropListRef.enter(
          event.source._dragRef,
          0,0
        );
        document.dispatchEvent(new Event('mouseup'));
      } else {
        if (data.currentRoomSelectionDialog != null) {
          data.currentRoomSelectionDialog.close(RoomSelectionDialogResult.CANCELED);
        }
      }
      if (error.Message != undefined) {
        snackBar.open(error.Message, "OK");
      } else {
        snackBar.open("You are not authorized to do this.", "OK");
      }
      return;
    }

    let busySlots = await this.scheduleDesignerApiService.GetBusyPeriods(
      courseEdition.CourseId, 
      courseEdition.CourseEditionId,
      tabWeeks[currentTabIndex]
    ).toPromise();
    let connectedTo = (isPropositionOnly && !isAdmin) ? [] : ['my-courses'];
    
    const numberOfSlots = settings.Periods.length - 1;
    let busySlotIndex = 0;
    let scheduleSlots = scheduleComponent.scheduleSlots.toArray();
    
    for (let i = 0; i < scheduleComponent.scheduleSlots.length; ++i) {
      let element = scheduleSlots[i].element as ElementRef<HTMLElement>;
      connectedTo.push(element.nativeElement.id);
      if (i != (busySlots[busySlotIndex]?.Day - 1) * numberOfSlots + (busySlots[busySlotIndex]?.PeriodIndex - 1)) {
        data.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = true;
      } else {
        data.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = false;
        ++busySlotIndex;
      }
    }
    data.scheduleSlotsValidity[indexes[0]][indexes[1]] = true;
    data.areSlotsValiditySet = true;
    dropContainer.connectedTo = connectedTo;
    
    if (!data.isCurrentDragReleased) {
      dropContainer._dropListRef.enter(
        event.source._dragRef, 0, 0
      );
    }
    
    if (data.isCurrentDragReleased) {
      try {
        const result = await this.signalrService.UnlockSchedulePositions(
          courseEdition.Room!.RoomId, indexes[1] + 1,
          indexes[0] + 1, tabWeeks[currentTabIndex]
        ).toPromise();
        
        if (result.StatusCode >= 400) {
          throw result;
        }

        courseEdition.IsLocked = false;
        courseEdition.IsLockedByAdmin = false;
      } catch (error) {
        
      }
      data.currentDragEvent = null;
      if (data.currentSelectedCourseEdition != null) {
        data.currentSelectedCourseEdition.CourseEdition.IsCurrentlyActive = false;
      }
      data.currentSelectedCourseEdition = null;
      for (let i = 0; i < scheduleComponent.scheduleSlots.length; ++i) {
        data.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = false;
      }
    }
  }

  public async onRelease(
    event: CdkDragRelease<CourseEdition>,
    data: ModifyingScheduleData,
    settings: Settings,
    scheduleComponent: ScheduleComponent,
  ): Promise<void> {
    data.isCurrentDragReleased = true;

    event.source.dropContainer.connectedTo = [];

    data.currentDragEvent = null;
    const numberOfSlots = settings.Periods.length - 1;
    for (let i = 0; i < scheduleComponent.scheduleSlots.length; ++i) {
      data.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = false;
    }
  }

  public onSelect(
    event: {courseEdition: CourseEdition, isDisabled: boolean, day: number, periodIndex: number},
    data: ModifyingScheduleData, isModifying: boolean
  ): void {
    if (data.currentSelectedCourseEdition != null) {
      if (data.currentSelectedCourseEdition.IsMoving) {
        return;
      }
      data.currentSelectedCourseEdition.CourseEdition.IsCurrentlyActive = false;
      if (event.courseEdition == data.currentSelectedCourseEdition.CourseEdition) {
        data.currentSelectedCourseEdition = null;
        return;
      }
    }
    data.currentSelectedCourseEdition = new SelectedCourseEdition(event.courseEdition, event.periodIndex, event.day);
    data.currentSelectedCourseEdition.CanChangeRoom = !event.isDisabled;
    data.currentSelectedCourseEdition.CanMakeMove = !event.isDisabled;
    data.currentSelectedCourseEdition.CanShowScheduledChanges = event.courseEdition.getScheduledMovesBadge(isModifying) > 0;
    event.courseEdition.IsCurrentlyActive = true;
  }

  public async onRoomSelect(
    event: {day: number, periodIndex: number},
    data: ModifyingScheduleData,
    tabWeeks: number[][],
    currentTabIndex: number,
    settings: Settings,
    roomTypes: Map<number, RoomType>,
    isMoveAvailable: boolean,
    isPropositionAvailable: boolean,
    filter: Filter,
    scheduleComponent: ScheduleComponent,
    dialogService: MatDialog,
    snackBar: MatSnackBar,
  ): Promise<void> {
    if (!data.currentSelectedCourseEdition?.IsMoving || !data.areSlotsValiditySet) {
      return;
    }

    data.isCurrentMoveValid = data.scheduleSlotsValidity[event.day][event.periodIndex];
    
    const selectedCourseEdition = data.currentSelectedCourseEdition;
    const dialogData = new RoomSelectionDialogData(
      selectedCourseEdition.CourseEdition,
      [selectedCourseEdition.Day,selectedCourseEdition.PeriodIndex],
      [event.day,event.periodIndex],
      tabWeeks[currentTabIndex],
      settings.DayLabels,
      settings.TimeLabels,
      roomTypes,
      data.isCurrentMoveValid!,
      true,
      isMoveAvailable,
      isPropositionAvailable,
      filter
    );

    data.currentRoomSelectionDialog = dialogService.open(RoomSelectionComponent, {
      disableClose: true,
      data: dialogData
    });
    const dialogResult:RoomSelectionDialogResult = await data.currentRoomSelectionDialog.afterClosed().toPromise();
    data.currentRoomSelectionDialog = null;

    switch (dialogResult.Status) {
      case RoomSelectionDialogStatus.ACCEPTED: {
        //remove old
        if (tabWeeks[currentTabIndex].sort((a,b) => a - b).join(',') 
          === data.currentSelectedCourseEdition.CourseEdition.Weeks?.sort((a,b) => a - b).join(',')) {
            
            const selectedDay = data.currentSelectedCourseEdition.Day;
            const selectedPeriodIndex = data.currentSelectedCourseEdition.PeriodIndex;
            const selectedCourseEdition = data.currentSelectedCourseEdition.CourseEdition;
            
            const existingSrcCourseEditions = scheduleComponent.schedule[selectedDay][selectedPeriodIndex].filter((courseEdition) => 
            selectedCourseEdition.CourseId == courseEdition.CourseId 
                && selectedCourseEdition.CourseEditionId == courseEdition.CourseEditionId
                && selectedCourseEdition.Room!.RoomId == courseEdition.Room!.RoomId
            );
            
            if (existingSrcCourseEditions.length > 0) {
              existingSrcCourseEditions[0].Weeks = existingSrcCourseEditions[0].Weeks
                ?.filter(week => !selectedCourseEdition.Weeks?.includes(week)) ?? [];
              
              if (existingSrcCourseEditions[0].Weeks.length == 0) {
                scheduleComponent.schedule[selectedDay][selectedPeriodIndex] 
                  = scheduleComponent.schedule[selectedDay][selectedPeriodIndex].filter(courseEdition => courseEdition.Weeks != null 
                    && courseEdition.Weeks.length > 0);
              }
            }
        }
        
        data.currentSelectedCourseEdition.CourseEdition.Room = dialogResult.Room;
        data.currentSelectedCourseEdition.CourseEdition.Weeks = dialogResult.Weeks;
        data.currentSelectedCourseEdition.CourseEdition.ScheduledMoves = [];

        //add new
        scheduleComponent.schedule[event.day][event.periodIndex].push(data.currentSelectedCourseEdition.CourseEdition);
      } break;
      case RoomSelectionDialogStatus.SCHEDULED: {

      } break;
      case RoomSelectionDialogStatus.CANCELED: {
        if (dialogResult.Message != undefined) {
          snackBar.open(dialogResult.Message, "OK");
        }
      } break;
      case RoomSelectionDialogStatus.FAILED: {
        if (dialogResult.Message != undefined) {
          snackBar.open(dialogResult.Message, "OK");
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
        
        data.currentSelectedCourseEdition.CourseEdition.IsLocked = false;
        data.currentSelectedCourseEdition.CourseEdition.IsLockedByAdmin = false;
      } catch (error) {
  
      }
    } else {
      data.currentSelectedCourseEdition.CourseEdition.IsLocked = false;
      data.currentSelectedCourseEdition.CourseEdition.IsLockedByAdmin = false;
    }
    
    data.isCurrentMoveValid = null;
    if (data.currentSelectedCourseEdition) {
      data.currentSelectedCourseEdition.IsMoving = false;
    }
    const numberOfSlots = settings.Periods.length - 1;
    for (let i = 0; i < scheduleComponent.scheduleSlots.length; ++i) {
      data.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = false;
    }
    data.areSlotsValiditySet = false;
  }

  public async addRoom(
    data: ModifyingScheduleData,
    roomTypes: Map<number, RoomType>,
    dialogService: MatDialog,
    snackBar: MatSnackBar,
  ): Promise<void> {
    if (data.currentSelectedCourseEdition == null) {
      return;
    }
    const selectedCourseEdition = data.currentSelectedCourseEdition;

    const dialogData = new AddRoomSelectionDialogData(
      selectedCourseEdition.CourseEdition,
      roomTypes
    );

    data.currentAddRoomSelectionDialog = dialogService.open(AddRoomSelectionComponent, {
      disableClose: true,
      data: dialogData
    });
    const dialogResult:AddRoomSelectionDialogResult = await data.currentAddRoomSelectionDialog.afterClosed().toPromise();
    data.currentAddRoomSelectionDialog = null;

    if (dialogResult.Message != undefined) {
      snackBar.open(dialogResult.Message, "OK");
    }
  }

  public async changeRoom(
    data: ModifyingScheduleData,
    isAdmin: boolean,
    settings: Settings,
    roomTypes: Map<number, RoomType>,
    isMoveAvailable: boolean,
    isPropositionAvailable: boolean,
    filter: Filter,
    dialogService: MatDialog,
    snackBar: MatSnackBar,
  ): Promise<void> {
    if (data.currentSelectedCourseEdition == null) {
      return;
    }
    const selectedCourseEdition = data.currentSelectedCourseEdition;

    try {
      const result = await this.signalrService.LockSchedulePositions(
        selectedCourseEdition.CourseEdition.Room!.RoomId, selectedCourseEdition.PeriodIndex + 1,
        selectedCourseEdition.Day + 1, selectedCourseEdition.CourseEdition.Weeks!
      ).toPromise();
      
      if (result.StatusCode >= 400) {
        throw result;
      }
      
      data.currentSelectedCourseEdition.CourseEdition.IsLocked = true;
      data.currentSelectedCourseEdition.CourseEdition.IsLockedByAdmin = isAdmin;
    } catch (error:any) {
      if (error.Message != undefined) {
        snackBar.open(error.Message, "OK");
      }
      return;
    }

    const dialogData = new RoomSelectionDialogData(
      selectedCourseEdition.CourseEdition,
      [selectedCourseEdition.Day,selectedCourseEdition.PeriodIndex],
      [selectedCourseEdition.Day,selectedCourseEdition.PeriodIndex],
      selectedCourseEdition.CourseEdition.Weeks!,
      settings.DayLabels,
      settings.TimeLabels,
      roomTypes,
      true,
      true,
      isMoveAvailable,
      isPropositionAvailable,
      filter
    );

    data.currentRoomSelectionDialog = dialogService.open(RoomSelectionComponent, {
      disableClose: true,
      data: dialogData
    });
    const dialogResult:RoomSelectionDialogResult = await data.currentRoomSelectionDialog.afterClosed().toPromise();
    data.currentRoomSelectionDialog = null;

    switch (dialogResult.Status) {
      case RoomSelectionDialogStatus.ACCEPTED: {
        data.currentSelectedCourseEdition.CourseEdition.Room = dialogResult.Room;
        data.currentSelectedCourseEdition.CourseEdition.Weeks = dialogResult.Weeks;
        data.currentSelectedCourseEdition.CourseEdition.ScheduledMoves = [];
      } break;
      case RoomSelectionDialogStatus.SCHEDULED: {

      } break;
      case RoomSelectionDialogStatus.CANCELED: {
        if (dialogResult.Message != undefined) {
          snackBar.open(dialogResult.Message, "OK");
        }
      } break;
      case RoomSelectionDialogStatus.FAILED: {
        if (dialogResult.Message != undefined) {
          snackBar.open(dialogResult.Message, "OK");
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
        
        data.currentSelectedCourseEdition.CourseEdition.IsLocked = false;
        data.currentSelectedCourseEdition.CourseEdition.IsLockedByAdmin = false;
      } catch (error) {
  
      }
    } else {
      data.currentSelectedCourseEdition.CourseEdition.IsLocked = false;
      data.currentSelectedCourseEdition.CourseEdition.IsLockedByAdmin = false;
    }
  }
  
  public async showScheduledChanges(
    data: ModifyingScheduleData,
    settings: Settings,
    propositionUserId: number | null,
    ignoreLocks: boolean,
    isModifying: boolean,
    roomTypes: Map<number, RoomType>,
    dialogService: MatDialog,
    snackBar: MatSnackBar,
  ): Promise<void> {
    if (data.currentSelectedCourseEdition == null) {
      return;
    }
    const selectedCourseEdition = data.currentSelectedCourseEdition;

    const dialogData = new ScheduledChangesDialogData(
      selectedCourseEdition.CourseEdition,
      [selectedCourseEdition.Day,selectedCourseEdition.PeriodIndex],
      settings.DayLabels,
      settings.TimeLabels,
      roomTypes,
      settings,
      propositionUserId,
      ignoreLocks,
      isModifying
    );

    data.currentScheduledChangesDialog = dialogService.open(ScheduledChangesViewComponent, {
      disableClose: true,
      data: dialogData
    });
    const dialogResult: ScheduledChangesDialogResult = await data.currentScheduledChangesDialog.afterClosed().toPromise();
    
    this.scheduleDesignerApiService.AreSchedulePositionsLocked(
      selectedCourseEdition.CourseEdition.Room?.RoomId!, selectedCourseEdition.PeriodIndex + 1, selectedCourseEdition.Day + 1,
      selectedCourseEdition.CourseEdition.Weeks!
    ).subscribe((response) => {
      selectedCourseEdition.CourseEdition.IsLocked = response.value;
      selectedCourseEdition.CourseEdition.IsLockedByAdmin = response.byAdmin;
    });

    data.currentScheduledChangesDialog = null;
    if (data.currentSelectedCourseEdition != null) {
      data.currentSelectedCourseEdition.CanShowScheduledChanges 
        = data.currentSelectedCourseEdition.CourseEdition.ScheduledMoves.length > 0;
    }

    if (dialogResult.Message != undefined) {
      snackBar.open(dialogResult.Message, "OK");
    }
  }

  private async cancelMove(
    data: ModifyingScheduleData,
    settings: Settings,
    scheduleComponent: ScheduleComponent): Promise<boolean> {
    if (data.currentSelectedCourseEdition == null) {
      return false;
    }

    if (data.currentSelectedCourseEdition.IsMoving) {
      try {
        const selectedCourseEdition = data.currentSelectedCourseEdition;
        const result = await this.signalrService.UnlockSchedulePositions(
          selectedCourseEdition.CourseEdition.Room?.RoomId!, selectedCourseEdition.PeriodIndex + 1,
          selectedCourseEdition.Day + 1, selectedCourseEdition.CourseEdition.Weeks!
        ).toPromise();
        
        if (result.StatusCode >= 400) {
          throw result;
        }
        
        data.currentSelectedCourseEdition.CourseEdition.IsLocked = false;
        data.currentSelectedCourseEdition.CourseEdition.IsLockedByAdmin = false;
      } catch (error) {
  
      }
      
      data.currentSelectedCourseEdition.IsMoving = false;
      
      const numberOfSlots = settings.Periods.length - 1;
      for (let i = 0; i < scheduleComponent.scheduleSlots.length; ++i) {
        data.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = false;
      }
      data.areSlotsValiditySet = false;
      return false;
    }

    return true;
  }

  public async move(
    data: ModifyingScheduleData,
    tabWeeks: number[][],
    currentTabIndex: number,
    isAdmin: boolean,
    settings: Settings,
    scheduleComponent: ScheduleComponent,
    snackBar: MatSnackBar,
  ): Promise<void> {
    const cancelMoveResult = await this.cancelMove(data, settings, scheduleComponent);
    if (!cancelMoveResult) {
      return;
    }

    const selectedCourseEdition = data.currentSelectedCourseEdition!;
    try {
      const result = await this.signalrService.LockSchedulePositions(
        selectedCourseEdition.CourseEdition.Room!.RoomId, selectedCourseEdition.PeriodIndex + 1,
        selectedCourseEdition.Day + 1, selectedCourseEdition.CourseEdition.Weeks!
      ).toPromise();
      
      if (result.StatusCode >= 400) {
        throw result;
      }

      data.currentSelectedCourseEdition!.CourseEdition.IsLocked = true;
      data.currentSelectedCourseEdition!.CourseEdition.IsLockedByAdmin = isAdmin;
    } catch (error:any) {
      if (error.Message != undefined) {
        snackBar.open(error.Message, "OK");
      }
      return;
    }
    
    let busySlots = await this.scheduleDesignerApiService.GetBusyPeriods(
      selectedCourseEdition.CourseEdition.CourseId, 
      selectedCourseEdition.CourseEdition.CourseEditionId,
      tabWeeks[currentTabIndex]
    ).toPromise();
    
    const numberOfSlots = settings.Periods.length - 1;
    let busySlotIndex = 0;
    
    for (let i = 0; i < scheduleComponent.scheduleSlots.length; ++i) {
      if (i != (busySlots[busySlotIndex]?.Day - 1) * numberOfSlots + (busySlots[busySlotIndex]?.PeriodIndex - 1)) {
        data.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = true;
      } else {
        data.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = false;
        ++busySlotIndex;
      }
    }

    data.areSlotsValiditySet = true;

    data.currentSelectedCourseEdition!.IsMoving = true;
  }

  public cancelSelection(
    data: ModifyingScheduleData,
  ): void {
    if (data.currentSelectedCourseEdition != null) {
      data.currentSelectedCourseEdition.CourseEdition.IsCurrentlyActive = false;
    }
    data.currentSelectedCourseEdition = null;
  }

  public onMouseEnter(
    event: {day: number, periodIndex: number},
    data: ModifyingScheduleData
  ): void {
    if (data.currentSelectedCourseEdition?.IsMoving) {
      data.currentDropContainerIndexes = [event.day, event.periodIndex];
      data.isCurrentMoveValid = data.scheduleSlotsValidity[event.day][event.periodIndex];
    }
  }

  public onMouseLeave(
    data: ModifyingScheduleData,
  ): void {
    if (data.currentSelectedCourseEdition?.IsMoving) {
      data.isCurrentMoveValid = null;
      data.currentDropContainerIndexes = [-1,-1];
    }
  }
}
