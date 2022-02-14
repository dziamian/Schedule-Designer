import { Component, Inject, OnInit } from '@angular/core';
import { FormControl } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { Subscription } from 'rxjs';
import { skip } from 'rxjs/operators';
import { SelectViewDialogData, SelectViewDialogResult } from 'src/app/others/dialogs/SelectViewDialogData';
import { SignalrService } from 'src/app/services/SignalrService/signalr.service';

/**
 * Komponent okna dialogowego do ustawiania niestandardowego widoku planu zajęć.
 */
@Component({
  selector: 'app-select-view',
  templateUrl: './select-view.component.html',
  styleUrls: ['./select-view.component.css']
})
export class SelectViewComponent implements OnInit {

  /** Tygodnie wybrane przez użytkownika. */
  selectedWeeks = new FormControl();
  /** Wszystkie tygodnie możliwe do wyboru. */
  allWeeks: number[];

  isConnectedSubscription: Subscription;

  constructor(
    @Inject(MAT_DIALOG_DATA) public data: SelectViewDialogData,
    public dialogRef: MatDialogRef<SelectViewComponent>,
    private signalrService: SignalrService
  ) { }

  /**
   * Metoda przygotowująca komponent.
   * Ustawia tygodnie możliwe do wyboru.
   */
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

  /**
   * Metoda zwracająca pusty rezultat operacji (użytkownik zamknął okno dialogowe).
   * @returns Pusty rezultat
   */
  GET_EMPTY_RESULT(): SelectViewDialogResult {
    return SelectViewDialogResult.EMPTY;
  }

  /** Metoda wybierająca wszystkie możliwe tygodnie. */
  SelectAll(): void {
    this.selectedWeeks.patchValue(this.allWeeks);
  }

  /** Metoda anulująca zaznaczenie tygodni. */
  SelectNone(): void {
    this.selectedWeeks.patchValue(null);
  }

  /** Metoda zamykająca okno dialogowe z rezultatem utworzenia nowego widoku niestandardowego. */
  Action(): void {
    this.dialogRef.close(new SelectViewDialogResult(this.selectedWeeks.value));
  }

  ngOnDestroy() {
    this.isConnectedSubscription.unsubscribe();
  }
}
