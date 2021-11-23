import { Component, Inject, OnInit } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { RoomSelectionDialogData, RoomSelectionDialogResult } from 'src/app/others/RoomSelectionDialog';

@Component({
  selector: 'app-room-selection',
  templateUrl: './room-selection.component.html',
  styleUrls: ['./room-selection.component.css']
})
export class RoomSelectionComponent implements OnInit {

  readonly FAILED:RoomSelectionDialogResult = RoomSelectionDialogResult.FAILED;
  readonly ACCEPTED:RoomSelectionDialogResult = RoomSelectionDialogResult.ACCEPTED;

  constructor(
    @Inject(MAT_DIALOG_DATA) public data:RoomSelectionDialogData,
    public dialogRef:MatDialogRef<RoomSelectionComponent>
  ) { }

  ngOnInit(): void {
    this.dialogRef.backdropClick().subscribe(event => {
      this.dialogRef.close(RoomSelectionDialogResult.FAILED);
    });
  }
}
