import { Component, Inject, OnInit } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { skip } from 'rxjs/operators';
import { AddedSchedulePositions, MessageObject, ModifiedSchedulePositions, SchedulePosition } from 'src/app/others/CommunicationObjects';
import { Room } from 'src/app/others/Room';
import { RoomSelectionDialogData, RoomSelectionDialogResult, RoomSelectionDialogStatus } from 'src/app/others/RoomSelectionDialog';
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

  courseRooms:Room[] = [];
  mappedCourseRooms:Map<number,Room[]>;

  loading:boolean = true;

  constructor(
    private scheduleDesignerApiService:ScheduleDesignerApiService,
    @Inject(MAT_DIALOG_DATA) public data:RoomSelectionDialogData,
    public dialogRef:MatDialogRef<RoomSelectionComponent>,
    private signalrService:SignalrService,
  ) { }

  ngOnInit(): void {
    this.dialogRef.backdropClick().subscribe(event => {
      this.dialogRef.close(RoomSelectionDialogResult.CANCELED);
    });
    this.signalrService.lastAddedSchedulePositions.subscribe((addedSchedulePositions) => {
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
              this.signalrService.lastAddedSchedulePositions.next(
                new AddedSchedulePositions([],-1,[], new SchedulePosition(-1,-1,-1,-1,-1,[]))
              );
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

    this.signalrService.lastModifiedSchedulePositions.subscribe((modifiedSchedulePositions) => {
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
                  this.signalrService.lastModifiedSchedulePositions.next(
                    new ModifiedSchedulePositions([],-1,[],
                    new SchedulePosition(-1,-1,-1,-1,-1,[]),
                    new SchedulePosition(-1,-1,-1,-1,-1,[]))
                  );
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
            this.scheduleDesignerApiService.GetRoomsAvailability(
              [busyRoom.RoomId], this.data.DestIndexes[1] + 1,
              this.data.DestIndexes[0] + 1, this.data.Weeks
            ).subscribe((rooms) => {
              const room = rooms[0];
              busyRoom.IsBusy = room.IsBusy;
            });
          }
      }
    });

    this.signalrService.lastRemovedSchedulePositions.subscribe((removedSchedulePositions) => {
      const periodIndex = removedSchedulePositions.SchedulePosition.PeriodIndex;
      const day = removedSchedulePositions.SchedulePosition.Day;
      const weeks = removedSchedulePositions.SchedulePosition.Weeks;
      const roomId = removedSchedulePositions.SchedulePosition.RoomId;

      if (this.data.DestIndexes[1] + 1 == periodIndex
        && this.data.DestIndexes[0] + 1 == day 
        && this.data.Weeks.filter((week) => weeks.includes(week)).length != 0) {
          const busyRoom = this.courseRooms.find(courseRoom => courseRoom.RoomId == roomId);
          if (busyRoom != undefined) {
            this.scheduleDesignerApiService.GetRoomsAvailability(
              [busyRoom.RoomId], this.data.DestIndexes[1] + 1,
              this.data.DestIndexes[0] + 1, this.data.Weeks
            ).subscribe((rooms) => {
              const room = rooms[0];
              busyRoom.IsBusy = room.IsBusy;
            });
          }
      }
    });

    this.scheduleDesignerApiService.GetCourseRooms(this.data.CourseEdition.CourseId, this.data.RoomTypes)
      .subscribe((courseRooms) => {
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

          this.mappedCourseRooms = this.GetMappedCourseRooms();

          this.loading = false;
        });
      });
  }

  GET_CANCELED_RESULT():RoomSelectionDialogResult {
    return RoomSelectionComponent.CANCELED;
  }

  GetMappedCourseRooms():Map<number,Room[]> {
    let rooms:Map<number,Room[]> = new Map<number,Room[]>();
    
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
          reject();
        });
        this.signalrService.AddSchedulePositions(
          courseEdition.CourseId, courseEdition.CourseEditionId,
          selectedRoom!.RoomId, destIndexes[1] + 1, 
          destIndexes[0] + 1, weeks
        );
        setTimeout(() => responseSubscription.unsubscribe(), 15000);
      })
      : ((isMoveValid && !selectedRoom.IsBusy) 
      ? await new Promise<MessageObject>((resolve, reject) => {
        const responseSubscription = this.signalrService.lastResponse.pipe(skip(1))
        .subscribe((messageObject) => {
          responseSubscription.unsubscribe();
          resolve(messageObject);
        },() => {
          reject();
        });
        this.signalrService.ModifySchedulePositions(
          courseEdition.Room!.RoomId, srcIndexes[1] + 1,
          srcIndexes[0] + 1, weeks,
          selectedRoom!.RoomId, destIndexes[1] + 1,
          destIndexes[0] + 1, weeks
        );
        setTimeout(() => responseSubscription.unsubscribe(), 15000);
      })
      : await this.signalrService.AddScheduledMove(
        courseEdition.Room!.RoomId, srcIndexes[1] + 1,
        srcIndexes[0] + 1, weeks,
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
