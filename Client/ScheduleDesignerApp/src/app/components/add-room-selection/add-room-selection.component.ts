import { Component, Inject, OnInit } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { forkJoin } from 'rxjs';
import { AddRoomSelectionDialogData, AddRoomSelectionDialogResult, RoomSelect } from 'src/app/others/AddRoomSelectionDialog';
import { Room } from 'src/app/others/Room';
import { ScheduleDesignerApiService } from 'src/app/services/ScheduleDesignerApiService/schedule-designer-api.service';

@Component({
  selector: 'app-add-room-selection',
  templateUrl: './add-room-selection.component.html',
  styleUrls: ['./add-room-selection.component.css']
})
export class AddRoomSelectionComponent implements OnInit {

  selectedRoom:Room|null;
  actionActivated:boolean = false;

  allRooms:RoomSelect[] = [];
  mappedAllRooms:Map<number,RoomSelect[]>;

  loading:boolean = true;

  constructor(
    private scheduleDesignerApiService:ScheduleDesignerApiService,
    @Inject(MAT_DIALOG_DATA) public data:AddRoomSelectionDialogData,
    public dialogRef:MatDialogRef<AddRoomSelectionComponent>,
  ) { }

  ngOnInit(): void {
    this.dialogRef.backdropClick().subscribe(event => {
      this.dialogRef.close(AddRoomSelectionDialogResult.EMPTY);
    });

    forkJoin([
      this.scheduleDesignerApiService.GetRooms(this.data.RoomTypes),
      this.scheduleDesignerApiService.GetCourseRooms(this.data.CourseEdition.CourseId, this.data.RoomTypes)
    ]).subscribe(([allRooms, courseRooms]) => {
      const courseRoomsIds = courseRooms.map((courseRoom) => courseRoom.RoomId);

      allRooms.forEach((room) => {
        const isDisabled = courseRoomsIds.includes(room.RoomId);
        this.allRooms.push(new RoomSelect(room, isDisabled));
      });

      this.mappedAllRooms = this.getMappedAllRooms();

      this.loading = false;
    });
  }

  GET_EMPTY_RESULT():AddRoomSelectionDialogResult {
    return AddRoomSelectionDialogResult.EMPTY;
  }

  private getMappedAllRooms():Map<number,RoomSelect[]> {
    const rooms:Map<number,RoomSelect[]> = new Map<number,RoomSelect[]>();
    
    this.allRooms.forEach((roomSelect) => {
      let currentRooms:RoomSelect[]|undefined = rooms.get(roomSelect.Room.RoomType.RoomTypeId);
      if (currentRooms == undefined) {
        rooms.set(roomSelect.Room.RoomType.RoomTypeId, new Array<RoomSelect>(roomSelect));
      } else {
        currentRooms.push(roomSelect);
        rooms.set(roomSelect.Room.RoomType.RoomTypeId, currentRooms);
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

    let message:string;
    try {
      await this.scheduleDesignerApiService.AddCourseRoom(
        this.data.CourseEdition.CourseId,
        selectedRoom.RoomId,
        this.data.CoordinatorId
      ).toPromise();

      message = "Successfully added room for this course.";
    } catch (error:any) {
      message = "Room does not exist or has been already added for this course.";
    }

    const result = new AddRoomSelectionDialogResult();
    result.Message = message;
    this.dialogRef.close(result);
  }

}
