import { Component, Inject, OnInit } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MessageObject } from 'src/app/others/CommunicationObjects';
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

  courseRooms:Room[];
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
      this.dialogRef.close(RoomSelectionComponent.CANCELED);
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

    /*setTimeout(() => {
      this.courseRooms.forEach((room) => {
        if (room.RoomId == 2) {
          room.IsBusy = true;
        }
      })
    }, 5000);*/
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
      const notImplemented:MessageObject = new MessageObject(404);
      notImplemented.Message = "Not implemented yet.";
      const result = (!areModified) 
      ? await this.signalrService.AddSchedulePositions(
        courseEdition.CourseId, courseEdition.CourseEditionId,
        selectedRoom!.RoomId, destIndexes[1] + 1, 
        destIndexes[0] + 1, weeks
      ).toPromise()
      : ((isMoveValid && !selectedRoom.IsBusy) 
      ? await this.signalrService.ModifySchedulePositions(
        courseEdition.Room!.RoomId, srcIndexes[1] + 1,
        srcIndexes[0] + 1, weeks,
        selectedRoom!.RoomId, destIndexes[1] + 1,
        destIndexes[0] + 1, weeks
      ).toPromise() 
      : notImplemented);
      
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
