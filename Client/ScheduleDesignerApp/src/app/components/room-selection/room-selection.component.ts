import { Component, Inject, OnInit } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { forkJoin } from 'rxjs';
import { skip } from 'rxjs/operators';
import { MessageObject } from 'src/app/others/CommunicationObjects';
import { Room } from 'src/app/others/Room';
import { RoomSelectionDialogData, RoomSelectionDialogResult, RoomSelectionDialogStatus } from 'src/app/others/dialogs/RoomSelectionDialog';
import { ScheduleDesignerApiService } from 'src/app/services/ScheduleDesignerApiService/schedule-designer-api.service';
import { SignalrService } from 'src/app/services/SignalrService/signalr.service';

@Component({
  selector: 'app-room-selection',
  templateUrl: './room-selection.component.html',
  styleUrls: ['./room-selection.component.css']
})
export class RoomSelectionComponent implements OnInit {

  static readonly CANCELED:RoomSelectionDialogResult = RoomSelectionDialogResult.CANCELED;

  selectedRoom:Room|null;
  actionActivated:boolean = false;
  isRoomOnlyChanging:boolean;
  groupsSize:number = 0;

  courseRooms:Room[] = [];
  mappedCourseRooms:Map<number,Room[]>;

  loading:boolean = true;

  constructor(
    private scheduleDesignerApiService:ScheduleDesignerApiService,
    @Inject(MAT_DIALOG_DATA) public data:RoomSelectionDialogData,
    public dialogRef:MatDialogRef<RoomSelectionComponent>,
    private signalrService:SignalrService,
  ) { 
    this.isRoomOnlyChanging = data.SrcIndexes[0] == data.DestIndexes[0] 
      && data.SrcIndexes[1] == data.DestIndexes[1] 
      && data.CourseEdition.Weeks?.sort((a,b) => a - b).join(',') === data.Weeks.sort((a,b) => a - b).join(',');
  }

  ngOnInit(): void {
    this.dialogRef.backdropClick().subscribe(event => {
      this.dialogRef.close(RoomSelectionDialogResult.CANCELED);
    });
    
    this.signalrService.lastAddedSchedulePositions.pipe(skip(1)).subscribe((addedSchedulePositions) => {
      const coordinatorsIds = addedSchedulePositions.CoordinatorsIds;
      const groupsIds = addedSchedulePositions.GroupsIds;
      const periodIndex = addedSchedulePositions.SchedulePosition.PeriodIndex;
      const day = addedSchedulePositions.SchedulePosition.Day;
      const weeks = addedSchedulePositions.SchedulePosition.Weeks;
      const roomId = addedSchedulePositions.SchedulePosition.RoomId;

      if (this.data.DestIndexes[1] + 1 != periodIndex
        || this.data.DestIndexes[0] + 1 != day || this.data.Weeks.filter((week) => weeks.includes(week)).length == 0) {
        return;
      }

      const item = this.data.CourseEdition;
      if (item != undefined 
        && (item.Coordinators.map(c => c.UserId).some(c => coordinatorsIds.includes(c))
        || item.Groups.map(g => g.GroupId).some(g => groupsIds.includes(g)))) {
          
          if (coordinatorsIds.includes(this.data.FilterCoordinatorId) || !this.data.CanBeScheduled) {
            setTimeout(() => {
              this.dialogRef.close(RoomSelectionDialogResult.CANCELED);
            });
            return;
          }

          this.data.IsMoveValid = false;
          
      }

      const busyRoom = this.courseRooms.find(courseRoom => courseRoom.RoomId == roomId);
      if (busyRoom != undefined) {
        busyRoom.IsBusy = true;
      }
    });

    this.signalrService.lastModifiedSchedulePositions.pipe(skip(1)).subscribe((modifiedSchedulePositions) => {
      const coordinatorsIds = modifiedSchedulePositions.CoordinatorsIds;
      const groupsIds = modifiedSchedulePositions.GroupsIds;
      const dstPeriodIndex = modifiedSchedulePositions.DestinationSchedulePosition.PeriodIndex;
      const srcPeriodIndex = modifiedSchedulePositions.SourceSchedulePosition.PeriodIndex;
      const dstDay = modifiedSchedulePositions.DestinationSchedulePosition.Day;
      const srcDay = modifiedSchedulePositions.SourceSchedulePosition.Day;
      const dstWeeks = modifiedSchedulePositions.DestinationSchedulePosition.Weeks;
      const srcWeeks = modifiedSchedulePositions.SourceSchedulePosition.Weeks;
      const dstRoomId = modifiedSchedulePositions.DestinationSchedulePosition.RoomId;
      const srcRoomId = modifiedSchedulePositions.SourceSchedulePosition.RoomId;

      if (this.data.DestIndexes[1] + 1 == dstPeriodIndex
        && this.data.DestIndexes[0] + 1 == dstDay 
        && this.data.Weeks.filter((week) => dstWeeks.includes(week)).length != 0) {
          
          const item = this.data.CourseEdition;
          if (item != undefined 
            && (item.Coordinators.map(c => c.UserId).some(c => coordinatorsIds.includes(c))
            || item.Groups.map(g => g.GroupId).some(g => groupsIds.includes(g)))) {
              if (coordinatorsIds.includes(this.data.FilterCoordinatorId) || !this.data.CanBeScheduled) {
                setTimeout(() => {
                  this.dialogRef.close(RoomSelectionDialogResult.CANCELED);
                });
                return;
              }
    
              this.data.IsMoveValid = false;
          }

          const busyRoom = this.courseRooms.find(courseRoom => courseRoom.RoomId == dstRoomId);
          if (busyRoom != undefined) {
            busyRoom.IsBusy = true;
          }
      }

      if (this.data.DestIndexes[1] + 1 == srcPeriodIndex
        && this.data.DestIndexes[0] + 1 == srcDay 
        && this.data.Weeks.filter((week) => srcWeeks.includes(week)).length != 0) {
          
          const busyRoom = this.courseRooms.find(courseRoom => courseRoom.RoomId == srcRoomId);
          if (busyRoom != undefined) {
            forkJoin([
              this.scheduleDesignerApiService.IsPeriodBusy(
                this.data.CourseEdition.CourseId, this.data.CourseEdition.CourseEditionId,
                srcPeriodIndex, srcDay,
                this.data.Weeks),
              this.scheduleDesignerApiService.GetRoomsAvailability(
                [busyRoom.RoomId], this.data.DestIndexes[1] + 1,
                this.data.DestIndexes[0] + 1, this.data.Weeks)
            ]).subscribe(([isBusy, rooms]) => {
              this.data.IsMoveValid = !isBusy;
              
              const room = rooms[0];
              busyRoom.IsBusy = room.IsBusy;
            });
          } else {
            this.scheduleDesignerApiService.IsPeriodBusy(
              this.data.CourseEdition.CourseId, this.data.CourseEdition.CourseEditionId,
              srcPeriodIndex, srcDay,
              this.data.Weeks
            ).subscribe(isBusy => {
              this.data.IsMoveValid = !isBusy;
            });
          }
      }
    });

    this.signalrService.lastRemovedSchedulePositions.pipe(skip(1)).subscribe((removedSchedulePositions) => {
      const periodIndex = removedSchedulePositions.SchedulePosition.PeriodIndex;
      const day = removedSchedulePositions.SchedulePosition.Day;
      const weeks = removedSchedulePositions.SchedulePosition.Weeks;
      const roomId = removedSchedulePositions.SchedulePosition.RoomId;

      if (this.data.DestIndexes[1] + 1 == periodIndex
        && this.data.DestIndexes[0] + 1 == day 
        && this.data.Weeks.filter((week) => weeks.includes(week)).length != 0) {
          
          const busyRoom = this.courseRooms.find(courseRoom => courseRoom.RoomId == roomId);
          if (busyRoom != undefined) {
            forkJoin([
              this.scheduleDesignerApiService.IsPeriodBusy(
                this.data.CourseEdition.CourseId, this.data.CourseEdition.CourseEditionId,
                periodIndex, day,
                this.data.Weeks),
              this.scheduleDesignerApiService.GetRoomsAvailability(
                [busyRoom.RoomId], this.data.DestIndexes[1] + 1,
                this.data.DestIndexes[0] + 1, this.data.Weeks)
            ]).subscribe(([isBusy, rooms]) => {
              this.data.IsMoveValid = !isBusy;
              
              const room = rooms[0];
              busyRoom.IsBusy = room.IsBusy;
            });
          } else {
            this.scheduleDesignerApiService.IsPeriodBusy(
              this.data.CourseEdition.CourseId, this.data.CourseEdition.CourseEditionId,
              periodIndex, day,
              this.data.Weeks
            ).subscribe(isBusy => {
              this.data.IsMoveValid = !isBusy;
            });
          }
      }
    });

    forkJoin([
      this.scheduleDesignerApiService.GetCourseEditionGroupsSize(this.data.CourseEdition.CourseId, this.data.CourseEdition.CourseEditionId),
      this.scheduleDesignerApiService.GetCourseRooms(this.data.CourseEdition.CourseId, this.data.RoomTypes),
    ]).subscribe(([size, courseRooms]) => {
      this.groupsSize = size;
      this.courseRooms = courseRooms;

      this.scheduleDesignerApiService.GetRoomsAvailability(
        this.courseRooms.map((room) => room.RoomId),
        this.data.DestIndexes[1] + 1,
        this.data.DestIndexes[0] + 1,
        this.data.Weeks
      ).subscribe((rooms) => {
        for (let i = 0; i < rooms.length; ++i) {
          let courseRoom = this.courseRooms[i];
          let room = rooms[i];
          if (courseRoom.RoomId == room.RoomId) {
            this.courseRooms[i].IsBusy = rooms[i].IsBusy;
          }
        }

        this.mappedCourseRooms = this.getMappedCourseRooms();

        this.loading = false;
      });
    });
  }

  GET_CANCELED_RESULT():RoomSelectionDialogResult {
    return RoomSelectionComponent.CANCELED;
  }

  private getMappedCourseRooms():Map<number,Room[]> {
    const rooms:Map<number,Room[]> = new Map<number,Room[]>();
    
    this.courseRooms.forEach((room) => {
      let currentRooms:Room[]|undefined = rooms.get(room.RoomType.RoomTypeId);
      if (currentRooms == undefined) {
        rooms.set(room.RoomType.RoomTypeId, new Array<Room>(room));
      } else {
        currentRooms.push(room);
        rooms.set(room.RoomType.RoomTypeId, currentRooms);
      }
    });

    return rooms;
  }

  async Action() {
    this.actionActivated = true;
    const selectedRoom = this.selectedRoom;

    if (selectedRoom == null) {
      return;
    }

    const areModified = this.data.SrcIndexes[0] != -1;
    const isMoveValid = this.data.IsMoveValid;
    const courseEdition = this.data.CourseEdition;
    const srcIndexes = this.data.SrcIndexes;
    const destIndexes = this.data.DestIndexes;
    const weeks = this.data.Weeks;

    let message:string = '';
    let status:RoomSelectionDialogStatus;
    try {
      const result = (!areModified) 
      ? await new Promise<MessageObject>((resolve, reject) => {
        const responseSubscription = this.signalrService.lastResponse.pipe(skip(1))
        .subscribe((messageObject) => {
          responseSubscription.unsubscribe();
          resolve(messageObject);
        },() => {
          reject({Message: "Connection error."});
        });
        this.signalrService.AddSchedulePositions(
          courseEdition.CourseId, courseEdition.CourseEditionId,
          selectedRoom!.RoomId, destIndexes[1] + 1, 
          destIndexes[0] + 1, weeks
        );
        
        setTimeout(() => {
          responseSubscription.unsubscribe(); 
          reject({Message: "Connection error."});
        }, 15000);
      })
      : ((isMoveValid && !selectedRoom.IsBusy) 
      ? await new Promise<MessageObject>((resolve, reject) => {
        const responseSubscription = this.signalrService.lastResponse.pipe(skip(1))
        .subscribe((messageObject) => {
          responseSubscription.unsubscribe();
          resolve(messageObject);
        },() => {
          reject({Message: "Connection error."});
        });
        this.signalrService.ModifySchedulePositions(
          courseEdition.Room!.RoomId, srcIndexes[1] + 1,
          srcIndexes[0] + 1, courseEdition.Weeks!,
          selectedRoom!.RoomId, destIndexes[1] + 1,
          destIndexes[0] + 1, weeks
        );
        
        setTimeout(() => {
          responseSubscription.unsubscribe(); 
          reject({Message: "Connection error."});
        }, 15000);
      })
      : await this.signalrService.AddScheduledMove(
        courseEdition.Room!.RoomId, srcIndexes[1] + 1,
        srcIndexes[0] + 1, courseEdition.Weeks!,
        selectedRoom!.RoomId, destIndexes[1] + 1,
        destIndexes[0] + 1, weeks
      ).toPromise());
      
      if (result.StatusCode >= 400) {
        throw result;
      }

      status = (isMoveValid) ? RoomSelectionDialogStatus.ACCEPTED : RoomSelectionDialogStatus.SCHEDULED;
    }
    catch (error:any) {
      status = RoomSelectionDialogStatus.FAILED;
      message = error.Message;
    }
    
    const dialogResult = new RoomSelectionDialogResult(
      status,
      selectedRoom,
      weeks
    );
    dialogResult.Message = message;
    this.dialogRef.close(dialogResult);
  }
}
