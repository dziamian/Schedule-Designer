import { Component, Inject, OnInit } from '@angular/core';
import { FormControl } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { Subscription } from 'rxjs';
import { skip } from 'rxjs/operators';
import { SelectViewDialogData, SelectViewDialogResult } from 'src/app/others/dialogs/SelectViewDialogData';
import { SignalrService } from 'src/app/services/SignalrService/signalr.service';

@Component({
  selector: 'app-select-view',
  templateUrl: './select-view.component.html',
  styleUrls: ['./select-view.component.css']
})
export class SelectViewComponent implements OnInit {

  selectedWeeks = new FormControl();
  allWeeks: number[];

  isConnectedSubscription: Subscription;

  constructor(
    @Inject(MAT_DIALOG_DATA) public data: SelectViewDialogData,
    public dialogRef: MatDialogRef<SelectViewComponent>,
    private signalrService: SignalrService
  ) { }

  ngOnInit(): void {
    this.dialogRef.backdropClick().subscribe(event => {
      this.dialogRef.close(SelectViewDialogResult.EMPTY);
    });

    this.isConnectedSubscription = this.signalrService.isConnected.pipe(skip(1)).subscribe((status) => {
      if (!status && !this.signalrService.connectionIntentionallyStopped) {
        this.dialogRef.close(SelectViewDialogResult.EMPTY);
      }
    });

    this.allWeeks = [];
    for (var i = 0; i < this.data.Settings.TermDurationWeeks; ++i) {
      this.allWeeks.push(i + 1);
    }
  }

  GET_EMPTY_RESULT(): SelectViewDialogResult {
    return SelectViewDialogResult.EMPTY;
  }

  SelectAll(): void {
    this.selectedWeeks.patchValue(this.allWeeks);
  }

  SelectNone(): void {
    this.selectedWeeks.patchValue(null);
  }

  Action(): void {
    this.dialogRef.close(new SelectViewDialogResult(this.selectedWeeks.value));
  }

  ngOnDestroy() {
    this.isConnectedSubscription.unsubscribe();
  }
}
