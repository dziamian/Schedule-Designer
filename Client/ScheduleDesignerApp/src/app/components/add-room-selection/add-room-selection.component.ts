import { Component, Inject, OnInit } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { forkJoin, Subscription } from 'rxjs';
import { skip } from 'rxjs/operators';
import { AddRoomSelectionDialogData, AddRoomSelectionDialogResult, RoomSelect } from 'src/app/others/dialogs/AddRoomSelectionDialog';
import { Room } from 'src/app/others/Room';
import { ScheduleDesignerApiService } from 'src/app/services/ScheduleDesignerApiService/schedule-designer-api.service';
import { SignalrService } from 'src/app/services/SignalrService/signalr.service';

/**
 * Komponent okna dialogowego do przypisywania pokoju przedmiotowi.
 */
@Component({
  selector: 'app-add-room-selection',
  templateUrl: './add-room-selection.component.html',
  styleUrls: ['./add-room-selection.component.css']
})
export class AddRoomSelectionComponent implements OnInit {

  /** Pokój wybrany przez użytkownika. */
  selectedRoom:Room|null;
  /** Określa czy naciśnięty został przycisk akcji dialogu. */
  actionActivated:boolean = false;

  /** Wszystkie dostępne pokoje. */
  allRooms:RoomSelect[] = [];
  /** 
   * Kolekcja pokojów z przypisanym identyfikatorem rodzaju. 
   * Właściwość używana jest do odpowiedniego wyświetlania listy pokojów.
  */
  mappedAllRooms:Map<number,RoomSelect[]>;

  /** Informuje czy dane zostały załadowane. */
  loading:boolean = true;
  isConnectedSubscription: Subscription;

  constructor(
    private signalrService: SignalrService,
    private scheduleDesignerApiService:ScheduleDesignerApiService,
    @Inject(MAT_DIALOG_DATA) public data:AddRoomSelectionDialogData,
    public dialogRef:MatDialogRef<AddRoomSelectionComponent>,
  ) { }

  /**
   * Metoda przygotowująca komponent.
   * Pobiera dane dotyczące pokojów możliwych do przypisania.
   */
  ngOnInit(): void {
    this.dialogRef.backdropClick().subscribe(event => {
      this.dialogRef.close(AddRoomSelectionDialogResult.EMPTY);
    });

    this.isConnectedSubscription = this.signalrService.isConnected.pipe(skip(1)).subscribe((status) => {
      if (!status && !this.signalrService.connectionIntentionallyStopped) {
        this.dialogRef.close(AddRoomSelectionDialogResult.EMPTY);
      }
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

  /**
   * Metoda mapująca pokoje do ich typów.
   * @returns Zwraca kolekcję pokojów z przypisanym identyfikatorem rodzaju
   */
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

  /**
   * Metoda wykonuje operację przypisania nowego pokoju przedmiotowi.
   */
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
        selectedRoom.RoomId
      ).toPromise();

      message = "Successfully added room for this course.";
    } catch (error:any) {
      message = "Room does not exist or has been already added for this course.";
    }

    const result = new AddRoomSelectionDialogResult();
    result.Message = message;
    this.dialogRef.close(result);
  }

  ngOnDestroy() {
    this.isConnectedSubscription.unsubscribe();
  }
}
