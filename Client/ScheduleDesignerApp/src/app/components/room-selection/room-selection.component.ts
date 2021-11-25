import { Component, Inject, OnInit } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { Room } from 'src/app/others/Room';
import { RoomSelectionDialogData, RoomSelectionDialogResult, RoomSelectionDialogStatus } from 'src/app/others/RoomSelectionDialog';
import { ScheduleDesignerApiService } from 'src/app/services/ScheduleDesignerApiService/schedule-designer-api.service';

@Component({
  selector: 'app-room-selection',
  templateUrl: './room-selection.component.html',
  styleUrls: ['./room-selection.component.css']
})
export class RoomSelectionComponent implements OnInit {

  static readonly FAILED:RoomSelectionDialogResult = new RoomSelectionDialogResult(
    RoomSelectionDialogStatus.FAILED,
    null
  );
  static readonly CANCELED:RoomSelectionDialogResult = new RoomSelectionDialogResult(
    RoomSelectionDialogStatus.CANCELED,
    null
  );

  selectedRoom:Room|null;

  courseRooms:Room[];
  mappedCourseRooms:Map<number,Room[]>;

  loading:boolean = true;

  constructor(
    private scheduleDesignerApiService:ScheduleDesignerApiService,
    @Inject(MAT_DIALOG_DATA) public data:RoomSelectionDialogData,
    public dialogRef:MatDialogRef<RoomSelectionComponent>
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
          this.data.SlotIndex[1] + 1,
          this.data.SlotIndex[0] + 1,
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

  GET_FAILED_RESULT():RoomSelectionDialogResult {
    return RoomSelectionComponent.FAILED;
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
}
