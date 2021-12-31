import { Component, Inject, OnInit } from '@angular/core';
import { FormControl } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { SelectViewDialogData, SelectViewDialogResult } from 'src/app/others/dialogs/SelectViewDialogData';

@Component({
  selector: 'app-select-view',
  templateUrl: './select-view.component.html',
  styleUrls: ['./select-view.component.css']
})
export class SelectViewComponent implements OnInit {

  selectedWeeks = new FormControl();
  allWeeks: number[];

  constructor(
    @Inject(MAT_DIALOG_DATA) public data: SelectViewDialogData,
    public dialogRef: MatDialogRef<SelectViewComponent>
  ) { }

  ngOnInit(): void {
    this.dialogRef.backdropClick().subscribe(event => {
      this.dialogRef.close(SelectViewDialogResult.EMPTY);
    });

    this.allWeeks = [];
    for (var i = 0; i < this.data.Settings.TermDurationWeeks; ++i) {
      this.allWeeks.push(i + 1);
    }
  }

  GET_EMPTY_RESULT(): SelectViewDialogResult {
    return SelectViewDialogResult.EMPTY;
  }

  Action(): void {
    this.dialogRef.close(new SelectViewDialogResult(this.selectedWeeks.value));
  }
}
