import { Component, Inject, OnInit } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { forkJoin, Observable } from 'rxjs';
import { skip } from 'rxjs/operators';
import { CourseEdition } from 'src/app/others/CourseEdition';
import { ScheduledChangesDialogData, ScheduledChangesDialogResult } from 'src/app/others/dialogs/ScheduledChangesDialog';
import { ScheduledMoveDetails } from 'src/app/others/ScheduledMove';
import { ScheduleDesignerApiService } from 'src/app/services/ScheduleDesignerApiService/schedule-designer-api.service';
import { SignalrService } from 'src/app/services/SignalrService/signalr.service';

@Component({
  selector: 'app-scheduled-changes-view',
  templateUrl: './scheduled-changes-view.component.html',
  styleUrls: ['./scheduled-changes-view.component.css']
})
export class ScheduledChangesViewComponent implements OnInit {

  scheduledMoves:ScheduledMoveDetails[] = [];

  loading:boolean = true;

  constructor(
    private scheduleDesignerApiService:ScheduleDesignerApiService,
    @Inject(MAT_DIALOG_DATA) public data:ScheduledChangesDialogData,
    public dialogRef:MatDialogRef<ScheduledChangesViewComponent>,
    private signalrService:SignalrService,
    private snackBar:MatSnackBar
  ) { }

  ngOnInit(): void {
    this.dialogRef.backdropClick().subscribe(event => {
      this.dialogRef.close(ScheduledChangesDialogResult.EMPTY);
    });

    this.signalrService.lastModifiedSchedulePositions.pipe(skip(1)).subscribe((modifiedSchedulePositions) => {
      this.scheduledMoves = this.scheduledMoves.filter((scheduledMove) => !modifiedSchedulePositions.MovesIds.includes(scheduledMove.MoveId));

      if (this.data.CourseEdition.CourseId == modifiedSchedulePositions.SourceSchedulePosition.CourseId &&
        this.data.CourseEdition.CourseEditionId != modifiedSchedulePositions.SourceSchedulePosition.CourseEditionId &&
        this.data.CourseEdition.Room?.RoomId != modifiedSchedulePositions.SourceSchedulePosition.RoomId &&
        this.data.SrcIndexes[1] != modifiedSchedulePositions.SourceSchedulePosition.PeriodIndex - 1 &&
        this.data.SrcIndexes[0] != modifiedSchedulePositions.SourceSchedulePosition.Day - 1) {
          if (this.data.CourseEdition.Weeks != null) {
            this.data.CourseEdition.Weeks = this.data.CourseEdition.Weeks?.filter((week) => !modifiedSchedulePositions.SourceSchedulePosition.Weeks.includes(week));
          }
      }

      if (this.data.CourseEdition.Weeks?.length == 0) {
        const dialogResult = new ScheduledChangesDialogResult();
        dialogResult.Message = "This course does not exist here anymore.";
        this.dialogRef.close(dialogResult);
        return;
      }

      if (this.scheduledMoves.length == 0) {
        const dialogResult = new ScheduledChangesDialogResult();
        dialogResult.Message = "No scheduled changes left for this position.";
        this.dialogRef.close(dialogResult);
        return;
      }
    });

    this.signalrService.lastRemovedSchedulePositions.pipe(skip(1)).subscribe((removedSchedulePositions) => {
      this.scheduledMoves = this.scheduledMoves.filter((scheduledMove) => !removedSchedulePositions.MovesIds.includes(scheduledMove.MoveId));

      if (this.data.CourseEdition.CourseId == removedSchedulePositions.SchedulePosition.CourseId &&
        this.data.CourseEdition.CourseEditionId != removedSchedulePositions.SchedulePosition.CourseEditionId &&
        this.data.CourseEdition.Room?.RoomId != removedSchedulePositions.SchedulePosition.RoomId &&
        this.data.SrcIndexes[1] != removedSchedulePositions.SchedulePosition.PeriodIndex - 1 &&
        this.data.SrcIndexes[0] != removedSchedulePositions.SchedulePosition.Day - 1) {
          if (this.data.CourseEdition.Weeks != null) {
            this.data.CourseEdition.Weeks = this.data.CourseEdition.Weeks?.filter((week) => !removedSchedulePositions.SchedulePosition.Weeks.includes(week));
          }
      }

      if (this.data.CourseEdition.Weeks?.length == 0) {
        const dialogResult = new ScheduledChangesDialogResult();
        dialogResult.Message = "This course does not exist here anymore.";
        this.dialogRef.close(dialogResult);
        return;
      }

      if (this.scheduledMoves.length == 0) {
        const dialogResult = new ScheduledChangesDialogResult();
        dialogResult.Message = "No scheduled changes left for this position.";
        this.dialogRef.close(dialogResult);
        return;
      }
    });

    this.signalrService.lastLockedSchedulePositions.pipe(skip(1)).subscribe((lockedSchedulePositions) => {
      if (this.data.CourseEdition.CourseId != lockedSchedulePositions.CourseId ||
        this.data.CourseEdition.CourseEditionId != lockedSchedulePositions.CourseEditionId ||
        this.data.CourseEdition.Room?.RoomId != lockedSchedulePositions.RoomId ||
        this.data.SrcIndexes[1] != lockedSchedulePositions.PeriodIndex - 1 ||
        this.data.SrcIndexes[0] != lockedSchedulePositions.Day - 1) {
          return;
      }

      this.scheduledMoves.forEach((scheduledMove) => {
        if (scheduledMove.SourceWeeks.some((week) => lockedSchedulePositions.Weeks.includes(week))) {
          scheduledMove.Locked = true;
        }
      });
    });

    this.signalrService.lastUnlockedSchedulePositions.pipe(skip(1)).subscribe((unlockedSchedulePositions) => {
      if (this.data.CourseEdition.CourseId != unlockedSchedulePositions.CourseId ||
        this.data.CourseEdition.CourseEditionId != unlockedSchedulePositions.CourseEditionId ||
        this.data.CourseEdition.Room?.RoomId != unlockedSchedulePositions.RoomId ||
        this.data.SrcIndexes[1] != unlockedSchedulePositions.PeriodIndex - 1 ||
        this.data.SrcIndexes[0] != unlockedSchedulePositions.Day - 1) {
          return;
      }

      this.scheduledMoves.forEach((scheduledMove) => {
        if (scheduledMove.SourceWeeks.some((week) => unlockedSchedulePositions.Weeks.includes(week))) {
          this.scheduleDesignerApiService.AreSchedulePositionsLocked(
            this.data.CourseEdition.Room?.RoomId!, this.data.SrcIndexes[1] + 1, this.data.SrcIndexes[0] + 1,
              scheduledMove.SourceWeeks
          ).subscribe((response) => {
            scheduledMove.Locked = response;
          })
        }
      })
    });

    this.signalrService.lastAddedScheduledMove.pipe(skip(1)).subscribe((addedScheduledMove) => {
      if (this.data.CourseEdition.CourseId != addedScheduledMove.sourceSchedulePosition.CourseId ||
        this.data.CourseEdition.CourseEditionId != addedScheduledMove.sourceSchedulePosition.CourseEditionId ||
        this.data.CourseEdition.Room?.RoomId != addedScheduledMove.sourceSchedulePosition.RoomId ||
        this.data.SrcIndexes[1] != addedScheduledMove.sourceSchedulePosition.PeriodIndex - 1 ||
        this.data.SrcIndexes[0] != addedScheduledMove.sourceSchedulePosition.Day - 1) {
          return;
      }

      const commonWeeks = addedScheduledMove.sourceSchedulePosition.Weeks.filter((week) => this.data.CourseEdition.Weeks);

      if (commonWeeks.length > 0) {
        this.scheduleDesignerApiService
          .GetConcreteScheduledMoves([addedScheduledMove.scheduledMove.MoveId], this.data.RoomTypes)
            .subscribe((scheduledMoveDetails) => {
              this.scheduleDesignerApiService.AreSchedulePositionsLocked(
                this.data.CourseEdition.Room?.RoomId!, this.data.SrcIndexes[1] + 1, this.data.SrcIndexes[0] + 1,
                scheduledMoveDetails[0].SourceWeeks
              ).subscribe((response) => {
                scheduledMoveDetails[0].Locked = response;
                
                this.scheduledMoves.push(scheduledMoveDetails[0]);
              });
            });
      }
    });

    this.signalrService.lastRemovedScheduledMove.pipe(skip(1)).subscribe((removedScheduledMove) => {
      this.scheduledMoves = this.scheduledMoves.filter((scheduledMove) => removedScheduledMove.moveId != scheduledMove.MoveId);

      if (this.scheduledMoves.length > 0) {
        return;
      }

      const dialogResult = new ScheduledChangesDialogResult();
      dialogResult.Message = "No scheduled changes left for this position.";
      this.dialogRef.close(dialogResult);
    });

    this.scheduleDesignerApiService
      .GetConcreteScheduledMoves(this.data.CourseEdition.ScheduledMoves.map(e => e.MoveId), this.data.RoomTypes)
        .subscribe((scheduledMovesDetails) => {
          this.scheduledMoves = scheduledMovesDetails;

          const courseEdition = this.data.CourseEdition;
          const srcIndexes = this.data.SrcIndexes;
          const tasks:Observable<boolean>[] = [];
          this.scheduledMoves.forEach((scheduledMove) => {
            tasks.push(this.scheduleDesignerApiService.AreSchedulePositionsLocked(
              courseEdition.Room?.RoomId!, srcIndexes[1] + 1, srcIndexes[0] + 1,
              scheduledMove.SourceWeeks
            ));
          });
          
          forkJoin(tasks).subscribe((responses) => {
            for (let i = 0; i < this.scheduledMoves.length; ++i) {
              this.scheduledMoves[i].Locked = responses[i];
            }

            this.loading = false;
          });
        }, () => {
          const dialogResult = new ScheduledChangesDialogResult();
          dialogResult.Message = "Could not find scheduled changes for this course.";
          this.dialogRef.close(dialogResult);
        });
  }

  ShowFrequency(weeks:number[]) { 
    return CourseEdition.ShowFrequency(this.data.Settings, weeks);
  }

  Confirm(moveId:number) {
    
  }

  async Remove(selectedScheduledMove:ScheduledMoveDetails) {
    selectedScheduledMove.IsRemoving = true;
    try {
      const lockingResult = await this.signalrService.LockSchedulePositions(
        this.data.CourseEdition.Room?.RoomId!, this.data.SrcIndexes[1] + 1,
        this.data.SrcIndexes[0] + 1, selectedScheduledMove.SourceWeeks
      ).toPromise();

      if (lockingResult.StatusCode >= 400) {
        throw lockingResult;
      }

      const result = await this.signalrService.RemoveScheduledMove(
        this.data.CourseEdition.Room?.RoomId!, this.data.SrcIndexes[1] + 1,
        this.data.SrcIndexes[0] + 1, selectedScheduledMove.SourceWeeks,
        selectedScheduledMove.DestRoom.RoomId, selectedScheduledMove.DestPeriodIndex, 
        selectedScheduledMove.DestDay, selectedScheduledMove.DestWeeks
      ).toPromise();

      if (result.StatusCode >= 400) {
        throw result;
      }
      
      this.scheduledMoves = this.scheduledMoves.filter((scheduledMove) => scheduledMove.MoveId != selectedScheduledMove.MoveId);

      const unlockingResult = await this.signalrService.UnlockSchedulePositions(
        this.data.CourseEdition.Room?.RoomId!, this.data.SrcIndexes[1] + 1,
        this.data.SrcIndexes[0] + 1, selectedScheduledMove.SourceWeeks
      ).toPromise();

      if (unlockingResult.StatusCode >= 400) {
        throw unlockingResult;
      }
    }
    catch (error:any) {
      if (error.Message != undefined) {
        this.snackBar.open(error.Message, "OK");
      }
      selectedScheduledMove.IsRemoving = false;
    }
  }
}
