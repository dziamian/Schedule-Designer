import { Component, Inject, OnInit } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { forkJoin, Observable, Subscription } from 'rxjs';
import { skip } from 'rxjs/operators';
import { CourseEdition } from 'src/app/others/CourseEdition';
import { ScheduledChangesDialogData, ScheduledChangesDialogResult } from 'src/app/others/dialogs/ScheduledChangesDialog';
import { ScheduledMoveDetails, ScheduledMoveInfo } from 'src/app/others/ScheduledMove';
import { ScheduleDesignerApiService } from 'src/app/services/ScheduleDesignerApiService/schedule-designer-api.service';
import { SignalrService } from 'src/app/services/SignalrService/signalr.service';

@Component({
  selector: 'app-scheduled-changes-view',
  templateUrl: './scheduled-changes-view.component.html',
  styleUrls: ['./scheduled-changes-view.component.css']
})
export class ScheduledChangesViewComponent implements OnInit {

  scheduledMoves:ScheduledMoveDetails[] = [];
  
  isInfoVisible:boolean = false;
  scheduledMoveInfo:ScheduledMoveInfo|null = null;

  loading:boolean = true;
  isConnectedSubscription: Subscription;

  signalrSubscriptions: Subscription[];

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

    this.isConnectedSubscription = this.signalrService.isConnected.pipe(skip(1)).subscribe((status) => {
      if (!status && !this.signalrService.connectionIntentionallyStopped) {
        this.dialogRef.close(ScheduledChangesDialogResult.EMPTY);
      }
    });

    this.signalrSubscriptions = [];

    this.signalrSubscriptions.push(this.signalrService.lastModifiedSchedulePositions.pipe(skip(1)).subscribe((modifiedSchedulePositions) => {
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
    }));

    this.signalrSubscriptions.push(this.signalrService.lastRemovedSchedulePositions.pipe(skip(1)).subscribe((removedSchedulePositions) => {
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
    }));

    this.signalrSubscriptions.push(this.signalrService.lastLockedSchedulePositions.pipe(skip(1)).subscribe((lockedSchedulePositions) => {
      if (this.data.CourseEdition.CourseId != lockedSchedulePositions.CourseId ||
        this.data.CourseEdition.CourseEditionId != lockedSchedulePositions.CourseEditionId ||
        this.data.CourseEdition.Room?.RoomId != lockedSchedulePositions.RoomId ||
        this.data.SrcIndexes[1] != lockedSchedulePositions.PeriodIndex - 1 ||
        this.data.SrcIndexes[0] != lockedSchedulePositions.Day - 1) {
          return;
      }

      this.scheduledMoves.forEach((scheduledMove) => {
        if (scheduledMove.SourceWeeks.some((week) => lockedSchedulePositions.Weeks.includes(week))) {
          scheduledMove.IsLocked = lockedSchedulePositions.IsLocked;
          scheduledMove.IsLockedByAdmin = lockedSchedulePositions.IsLockedByAdmin;
        }
      });
    }));

    this.signalrSubscriptions.push(this.signalrService.lastUnlockedSchedulePositions.pipe(skip(1)).subscribe((unlockedSchedulePositions) => {
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
            scheduledMove.IsLocked = response.value;
            scheduledMove.IsLockedByAdmin = response.byAdmin;
          })
        }
      });
    }));

    this.signalrSubscriptions.push(this.signalrService.lastAddedScheduledMove.pipe(skip(1)).subscribe((addedScheduledMove) => {
      if (!this.data.IsModifying && !addedScheduledMove.scheduledMove.IsConfirmed) {
        return;
      }
      
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
                scheduledMoveDetails[0].IsLocked = response.value;
                scheduledMoveDetails[0].IsLockedByAdmin = response.byAdmin;
                
                this.scheduledMoves.push(scheduledMoveDetails[0]);
              });
            });
      }
    }));

    this.signalrSubscriptions.push(this.signalrService.lastRemovedScheduledMove.pipe(skip(1)).subscribe((removedScheduledMove) => {
      this.scheduledMoves = this.scheduledMoves.filter((scheduledMove) => removedScheduledMove.moveId != scheduledMove.MoveId);

      if (this.scheduledMoves.length > 0) {
        return;
      }

      const dialogResult = new ScheduledChangesDialogResult();
      dialogResult.Message = "No changes left for this position.";
      this.dialogRef.close(dialogResult);
    }));

    this.signalrSubscriptions.push(this.signalrService.lastAcceptedScheduledMove.pipe(skip(1)).subscribe((acceptedScheduledMove) => {
      this.scheduledMoves = this.scheduledMoves.filter((scheduledMove) => {
        if (acceptedScheduledMove.moveId == scheduledMove.MoveId) {
          scheduledMove.IsConfirmed = true;
        }
        return true;
      });
    }));

    const scheduledMoves = (!this.data.IsModifying) ? this.data.CourseEdition.ScheduledMoves.filter(x => x.IsConfirmed) : this.data.CourseEdition.ScheduledMoves;

    this.scheduleDesignerApiService
      .GetConcreteScheduledMoves(
        scheduledMoves.map(e => e.MoveId), this.data.RoomTypes)
          .subscribe((scheduledMovesDetails) => {
            this.scheduledMoves = scheduledMovesDetails;

            const courseEdition = this.data.CourseEdition;
            const srcIndexes = this.data.SrcIndexes;
            const tasks:Observable<{value: boolean, byAdmin: boolean}>[] = [];
            this.scheduledMoves.forEach((scheduledMove) => {
              tasks.push(this.scheduleDesignerApiService.AreSchedulePositionsLocked(
                courseEdition.Room?.RoomId!, srcIndexes[1] + 1, srcIndexes[0] + 1,
                scheduledMove.SourceWeeks
              ));
            });
            
            forkJoin(tasks).subscribe((responses) => {
              for (let i = 0; i < this.scheduledMoves.length; ++i) {
                this.scheduledMoves[i].IsLocked = responses[i].value;
                this.scheduledMoves[i].IsLockedByAdmin = responses[i].byAdmin;
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

  ShowInfo(selectedScheduledMove:ScheduledMoveDetails) {
    if (!this.isInfoVisible) {
      this.isInfoVisible = true;
    }
    this.scheduleDesignerApiService.GetScheduledMoveInfo(selectedScheduledMove.MoveId).subscribe((info) => {
      this.scheduledMoveInfo = info;
    },() => {
      this.scheduledMoveInfo = null;
    });
  }

  async Remove(selectedScheduledMove:ScheduledMoveDetails) {
    selectedScheduledMove.IsRemoving = true;
    var isLocked = false;
    
    try {
      const lockingResult = await this.signalrService.LockSchedulePositions(
        this.data.CourseEdition.Room?.RoomId!, this.data.SrcIndexes[1] + 1,
        this.data.SrcIndexes[0] + 1, selectedScheduledMove.SourceWeeks
      ).toPromise();

      if (lockingResult.StatusCode >= 400) {
        throw lockingResult;
      }
      isLocked = true;
      this.scheduledMoves.forEach((scheduledMove) => {
        if (scheduledMove.SourceWeeks.some((week) => selectedScheduledMove.SourceWeeks.includes(week))) {
          scheduledMove.IsLocked = true;
          scheduledMove.IsLockedByAdmin = this.data.IgnoreUsersLocks;
        }
      });

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
      
    } catch (error:any) {
      if (error.Message != undefined) {
        this.snackBar.open(error.Message, "OK");
      } else {
        this.snackBar.open("You are not authorized to do this.", "OK");
      }
    }
    
    if (isLocked) {
      try {
        const unlockingResult = await this.signalrService.UnlockSchedulePositions(
          this.data.CourseEdition.Room?.RoomId!, this.data.SrcIndexes[1] + 1,
          this.data.SrcIndexes[0] + 1, selectedScheduledMove.SourceWeeks
        ).toPromise();
  
        if (unlockingResult.StatusCode >= 400) {
          throw unlockingResult;
        }

      } catch (error:any) {
      }
    }

    this.scheduledMoves.forEach((scheduledMove) => {
      if (scheduledMove.SourceWeeks.some((week) => selectedScheduledMove.SourceWeeks.includes(week))) {
        this.scheduleDesignerApiService.AreSchedulePositionsLocked(
          this.data.CourseEdition.Room?.RoomId!, this.data.SrcIndexes[1] + 1, this.data.SrcIndexes[0] + 1,
            scheduledMove.SourceWeeks
        ).subscribe((response) => {
          scheduledMove.IsLocked = response.value;
          scheduledMove.IsLockedByAdmin = response.byAdmin;
        })
      }
    });
    selectedScheduledMove.IsRemoving = false;
  }

  async Accept(selectedScheduledMove:ScheduledMoveDetails) {
    selectedScheduledMove.IsAccepting = true;
    var isLocked = false;

    try {
      const lockingResult = await this.signalrService.LockSchedulePositions(
        this.data.CourseEdition.Room?.RoomId!, this.data.SrcIndexes[1] + 1,
        this.data.SrcIndexes[0] + 1, selectedScheduledMove.SourceWeeks
      ).toPromise();

      if (lockingResult.StatusCode >= 400) {
        throw lockingResult;
      }
      isLocked = true;
      this.scheduledMoves.forEach((scheduledMove) => {
        if (scheduledMove.SourceWeeks.some((week) => selectedScheduledMove.SourceWeeks.includes(week))) {
          scheduledMove.IsLocked = true;
          scheduledMove.IsLockedByAdmin = this.data.IgnoreUsersLocks;
        }
      });

      const result = await this.signalrService.AcceptProposition(
        this.data.CourseEdition.Room?.RoomId!, this.data.SrcIndexes[1] + 1,
        this.data.SrcIndexes[0] + 1, selectedScheduledMove.SourceWeeks,
        selectedScheduledMove.DestRoom.RoomId, selectedScheduledMove.DestPeriodIndex, 
        selectedScheduledMove.DestDay, selectedScheduledMove.DestWeeks
      ).toPromise();

      if (result.StatusCode >= 400) {
        throw result;
      }
      
      selectedScheduledMove.IsConfirmed = true;
    } catch (error:any) {
      if (error.Message != undefined) {
        this.snackBar.open(error.Message, "OK");
      } else {
        this.snackBar.open("You are not authorized to do this.", "OK");
      }
    }

    if (isLocked) {
      try {
        const unlockingResult = await this.signalrService.UnlockSchedulePositions(
          this.data.CourseEdition.Room?.RoomId!, this.data.SrcIndexes[1] + 1,
          this.data.SrcIndexes[0] + 1, selectedScheduledMove.SourceWeeks
        ).toPromise();
  
        if (unlockingResult.StatusCode >= 400) {
          throw unlockingResult;
        }
        
        selectedScheduledMove.IsLocked = false;
        selectedScheduledMove.IsLockedByAdmin = false;
      } catch (error:any) {

      }
    }

    this.scheduledMoves.forEach((scheduledMove) => {
      if (scheduledMove.SourceWeeks.some((week) => selectedScheduledMove.SourceWeeks.includes(week))) {
        this.scheduleDesignerApiService.AreSchedulePositionsLocked(
          this.data.CourseEdition.Room?.RoomId!, this.data.SrcIndexes[1] + 1, this.data.SrcIndexes[0] + 1,
            scheduledMove.SourceWeeks
        ).subscribe((response) => {
          scheduledMove.IsLocked = response.value;
          scheduledMove.IsLockedByAdmin = response.byAdmin;
        })
      }
    });
    selectedScheduledMove.IsAccepting = true;
  }

  ngOnDestroy() {
    this.signalrSubscriptions.forEach(
      subscription => subscription.unsubscribe()
    );
    this.isConnectedSubscription.unsubscribe();
  }
}
