import { Component, ElementRef, OnInit, QueryList, ViewChild, ViewChildren } from '@angular/core';
import { CdkDrag, CdkDragDrop, CdkDragEnter, CdkDragRelease, CdkDragStart, CdkDropList, DropListRef, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { ScheduleDesignerApiService } from 'src/app/services/ScheduleDesignerApiService/schedule-designer-api.service';
import { CourseEdition } from 'src/app/others/CourseEdition';
import { CourseType, RoomType } from 'src/app/others/Types';
import { Group } from 'src/app/others/Group';
import { SignalrService } from 'src/app/services/SignalrService/signalr.service';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { CourseComponent } from 'src/app/components/course/course.component';
import { Settings } from 'src/app/others/Settings';
import { forkJoin } from 'rxjs';
import { MatSnackBar } from '@angular/material/snack-bar';
import { UsosApiService } from 'src/app/services/UsosApiService/usos-api.service';
import { Router } from '@angular/router';
import { skip } from 'rxjs/operators';
import { RoomSelectionComponent } from 'src/app/components/room-selection/room-selection.component';
import { Room } from 'src/app/others/Room';
import { RoomSelectionDialogData, RoomSelectionDialogResult, RoomSelectionDialogStatus } from 'src/app/others/RoomSelectionDialog';
import { SchedulePosition } from 'src/app/others/CommunicationObjects';
import { Store } from '@ngrx/store';
import { Account } from 'src/app/others/Accounts';

@Component({
  selector: 'app-schedule',
  templateUrl: './schedule.component.html',
  styleUrls: ['./schedule.component.css']
})
export class ScheduleComponent implements OnInit {

  @ViewChild('myCoursesSlot') myCoursesSlot : DropListRef<CourseEdition>
  @ViewChildren('scheduleSlots') scheduleSlots : QueryList<DropListRef<CourseEdition>>;
  @ViewChildren(CourseComponent) courses : QueryList<CourseComponent>;
  currentDragEvent : CdkDragStart<CourseEdition> | null;
  currentOpenedDialog : MatDialogRef<RoomSelectionComponent, any> | null;
  isReleased:boolean = false;
  isCanceled:boolean = false;
  
  account:Account;

  loading:boolean = true;
  tabLoading:boolean = true;
  connectionStatus:boolean = false;
  
  settings:Settings;
  frequencies:number[];
  weeks:number[][];
  
  currentTabIndex:number = 0;
  
  tabLabels:string[] = ['Semester', 'Even Weeks', 'Odd Weeks'];
  scheduleTimeLabels:string[] = [];
  scheduleDayLabels:string[] = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday'];

  courseTypes:Map<number, CourseType>;
  roomTypes:Map<number, RoomType>;

  myCourses:CourseEdition[];
  schedule:CourseEdition[][][];
  
  scheduleSlotsValidity:boolean[][];
  isMoveValid:boolean|null = null;

  constructor(
    private store:Store<{account: Account}>,
    private scheduleDesignerApiService:ScheduleDesignerApiService,
    private usosApiService:UsosApiService,
    private signalrService:SignalrService,
    private router:Router,
    private snackBar:MatSnackBar,
    private dialog:MatDialog
  ) { 
    this.store.select('account').subscribe((account) => {
      if (account.UserId == 0) {
        return;
      }
      this.account = account;
    });
  }

  private UpdateLockInMyCourses(courseId:number, courseEditionId:number, value:boolean) {
    if (!this.myCourses) {
      return;
    }

    this.myCourses.forEach((myCourse) => {
      if (myCourse.CourseId == courseId && myCourse.CourseEditionId == courseEditionId) {
        myCourse.Locked = value;
      }
    });
  }

  private UpdateLockInSchedule(position:SchedulePosition, value:boolean) {
    if (!this.schedule) {
      return;
    }

    if (!this.weeks[this.currentTabIndex].some(r => position.Weeks.includes(r))) {
      return;
    }

    const courseId = position.CourseId;
    const courseEditionId = position.CourseEditionId;
    const roomId = position.RoomId;
    const day = position.Day - 1;
    const periodIndex = position.PeriodIndex - 1;

    let courseEditions = this.schedule[day][periodIndex];
    courseEditions.forEach((courseEdition) => {
      if (courseEdition.CourseId == courseId && courseEdition.CourseEditionId == courseEditionId 
        && courseEdition.Room?.RoomId == roomId) {
        courseEdition.Locked = value;
      }
    });
  }

  private setSignalrSubscriptions():void {
    this.signalrService.lastLockedCourseEdition.pipe(skip(1)).subscribe(({courseId, courseEditionId}) => {
      this.UpdateLockInMyCourses(courseId, courseEditionId, true);
    });

    this.signalrService.lastLockedSchedulePositions.pipe(skip(1)).subscribe((lockedSchedulePositions) => {
      this.UpdateLockInSchedule(lockedSchedulePositions, true);
    });

    this.signalrService.lastUnlockedCourseEdition.pipe(skip(1)).subscribe(({courseId, courseEditionId}) => {
      this.UpdateLockInMyCourses(courseId, courseEditionId, false);
    });

    this.signalrService.lastUnlockedSchedulePositions.pipe(skip(1)).subscribe((unlockedSchedulePositions) => {
      this.UpdateLockInSchedule(unlockedSchedulePositions, false);
    });

    this.signalrService.lastAddedSchedulePositions.pipe(skip(1)).subscribe((addedSchedulePositions) => {
      const schedulePosition = addedSchedulePositions.SchedulePosition;
      const addedAmount = schedulePosition.Weeks.length;

      //filter for updating board
      if (addedSchedulePositions.CoordinatorsIds.includes(this.account.UserId)) {
        //remove from my courses
        const courses = this.myCourses.filter((course) => 
          course.CourseId == schedulePosition.CourseId 
          && course.CourseEditionId == schedulePosition.CourseEditionId 
        );

        if (courses.length != 0) {
          const firstCourse = courses[0];
          let currentScheduleAmount = firstCourse.ScheduleAmount + addedAmount;

          this.myCourses = this.myCourses.filter((course) => {
            if (course.CourseId != schedulePosition.CourseId 
              || course.CourseEditionId != schedulePosition.CourseEditionId) {
              return true;
            }
            
            if (currentScheduleAmount + course.CurrentAmount <= course.FullAmount) {
              course.ScheduleAmount += addedAmount;
              currentScheduleAmount += course.CurrentAmount;
              return true;
            }

            return false;
          });
        }
        
        //add new
        const existingCourseEditions = this.schedule[schedulePosition.Day - 1][schedulePosition.PeriodIndex - 1].filter((courseEdition) => 
          courseEdition.CourseId == schedulePosition.CourseId 
            && courseEdition.CourseEditionId == schedulePosition.CourseEditionId
            && courseEdition.Room!.RoomId == schedulePosition.RoomId
        );

        if (existingCourseEditions.length > 0) {
          existingCourseEditions[0].Weeks?.push(
            ...schedulePosition.Weeks.filter(week => this.weeks[this.currentTabIndex].includes(week))
          );
        } else {
          const mainGroupsIds = addedSchedulePositions.GroupsIds.slice(
            0, addedSchedulePositions.MainGroupsAmount
          );
  
          forkJoin([
            this.scheduleDesignerApiService.GetCourseEditionInfo(
              schedulePosition.CourseId, schedulePosition.CourseEditionId, this.settings),
            this.scheduleDesignerApiService.GetGroupsFullNames(mainGroupsIds),
            this.scheduleDesignerApiService.GetCoordinators(addedSchedulePositions.CoordinatorsIds),
            this.scheduleDesignerApiService.GetRoomsNames([schedulePosition.RoomId])
          ]).subscribe(([courseEditionInfo, groupsNames, coordinators, roomNames]) => {
            let groups:Group[] = [];
            for (let i = 0; i < mainGroupsIds.length; ++i) {
              const group = new Group(mainGroupsIds[i]);
              group.FullName = groupsNames[i];
              groups.push(group);
            }
            const room = new Room(schedulePosition.RoomId);
            room.Name = roomNames[0];
            
            const addedCourseEdition = new CourseEdition(
              schedulePosition.CourseId, schedulePosition.CourseEditionId,
              courseEditionInfo.Name, this.courseTypes.get(courseEditionInfo.CourseTypeId)!,
              0, groups, coordinators
            );
            const tabWeeks = this.weeks[this.currentTabIndex];
  
            addedCourseEdition.Room = room;
            addedCourseEdition.Weeks = schedulePosition.Weeks.filter(week => tabWeeks.includes(week));
            addedCourseEdition.ScheduleAmount = courseEditionInfo.ScheduleAmount;
            addedCourseEdition.FullAmount = courseEditionInfo.FullAmount;
  
            this.schedule[schedulePosition.Day - 1][schedulePosition.PeriodIndex - 1].push(addedCourseEdition);
          });
        }
      }
      
      //active drag fields update
      const item = this.currentDragEvent?.source.data;
      if (item != undefined 
        && (item.Coordinators.map(c => c.UserId).some(c => addedSchedulePositions.CoordinatorsIds.includes(c))
        || item.Groups.map(g => g.GroupId).some(g => addedSchedulePositions.GroupsIds.includes(g)))) {
        this.scheduleSlotsValidity[schedulePosition.Day - 1][schedulePosition.PeriodIndex - 1] = false;
      }
    });
    
    this.signalrService.lastModifiedSchedulePositions.pipe(skip(1)).subscribe((modifiedSchedulePositions) => {
      console.log(modifiedSchedulePositions);

      const srcSchedulePosition = modifiedSchedulePositions.SourceSchedulePosition;
      const dstSchedulePosition = modifiedSchedulePositions.DestinationSchedulePosition;

      //filter for updating board
      if (modifiedSchedulePositions.CoordinatorsIds.includes(this.account.UserId)) {
        //remove or update old
        let srcScheduleSlot = this.schedule[srcSchedulePosition.Day - 1][srcSchedulePosition.PeriodIndex - 1];
        const existingSrcCourseEditions = this.schedule[srcSchedulePosition.Day - 1][srcSchedulePosition.PeriodIndex - 1].filter((courseEdition) => 
          courseEdition.CourseId == srcSchedulePosition.CourseId 
            && courseEdition.CourseEditionId == srcSchedulePosition.CourseEditionId
            && courseEdition.Room!.RoomId == srcSchedulePosition.RoomId
        );
        
        if (existingSrcCourseEditions.length > 0) {
          existingSrcCourseEditions[0].Weeks = existingSrcCourseEditions[0].Weeks
            ?.filter(week => !srcSchedulePosition.Weeks.includes(week)) ?? [];
          
          if (existingSrcCourseEditions[0].Weeks.length == 0) {
            this.schedule[srcSchedulePosition.Day - 1][srcSchedulePosition.PeriodIndex - 1] 
              = srcScheduleSlot.filter(courseEdition => courseEdition.Weeks != null 
                && courseEdition.Weeks.length > 0);
          } else {
            this.scheduleDesignerApiService.AreSchedulePositionsLocked(
              srcSchedulePosition.RoomId, srcSchedulePosition.PeriodIndex,
              srcSchedulePosition.Day, existingSrcCourseEditions[0].Weeks
            ).subscribe((areLocked) => {
              existingSrcCourseEditions[0].Locked = areLocked;
            });
          }
        }

        //add new
        let dstScheduleSlot = this.schedule[dstSchedulePosition.Day - 1][dstSchedulePosition.PeriodIndex - 1];
        const existingDstCourseEditions = dstScheduleSlot.filter((courseEdition) => 
          courseEdition.CourseId == dstSchedulePosition.CourseId 
            && courseEdition.CourseEditionId == dstSchedulePosition.CourseEditionId
            && courseEdition.Room!.RoomId == dstSchedulePosition.RoomId
        );

        if (existingDstCourseEditions.length > 0) {
          existingDstCourseEditions[0].Weeks?.push(
            ...dstSchedulePosition.Weeks.filter(week => this.weeks[this.currentTabIndex].includes(week))
          );
        } else {
          const mainGroupsIds = modifiedSchedulePositions.GroupsIds.slice(
            0, modifiedSchedulePositions.MainGroupsAmount
          );

          forkJoin([
            this.scheduleDesignerApiService.GetCourseEditionInfo(
              dstSchedulePosition.CourseId, dstSchedulePosition.CourseEditionId, this.settings),
            this.scheduleDesignerApiService.GetGroupsFullNames(mainGroupsIds),
            this.scheduleDesignerApiService.GetCoordinators(modifiedSchedulePositions.CoordinatorsIds),
            this.scheduleDesignerApiService.GetRoomsNames([dstSchedulePosition.RoomId])
          ]).subscribe(([courseEditionInfo, groupsNames, coordinators, roomNames]) => {
            let groups:Group[] = [];
            for (let i = 0; i < mainGroupsIds.length; ++i) {
              const group = new Group(mainGroupsIds[i]);
              group.FullName = groupsNames[i];
              groups.push(group);
            }
            const room = new Room(dstSchedulePosition.RoomId);
            room.Name = roomNames[0];
            
            const addedCourseEdition = new CourseEdition(
              dstSchedulePosition.CourseId, dstSchedulePosition.CourseEditionId,
              courseEditionInfo.Name, this.courseTypes.get(courseEditionInfo.CourseTypeId)!,
              0, groups, coordinators
            );
            const tabWeeks = this.weeks[this.currentTabIndex];
  
            addedCourseEdition.Room = room;
            addedCourseEdition.Weeks = dstSchedulePosition.Weeks.filter(week => tabWeeks.includes(week));
            addedCourseEdition.ScheduleAmount = courseEditionInfo.ScheduleAmount;
            addedCourseEdition.FullAmount = courseEditionInfo.FullAmount;
  
            dstScheduleSlot.push(addedCourseEdition);
          });
        }
      }

      //active drag fields update
      const item = this.currentDragEvent?.source.data;
      if (item == undefined) {
        return;
      }

      if (item.Coordinators.map(c => c.UserId).some(c => modifiedSchedulePositions.CoordinatorsIds.includes(c))
        || item.Groups.map(g => g.GroupId).some(g => modifiedSchedulePositions.GroupsIds.includes(g))) {
        this.scheduleSlotsValidity[dstSchedulePosition.Day - 1][dstSchedulePosition.PeriodIndex - 1] = false;

        this.scheduleDesignerApiService.IsPeriodBusy(
          srcSchedulePosition.CourseId, srcSchedulePosition.CourseEditionId,
          srcSchedulePosition.PeriodIndex + 1, srcSchedulePosition.Day + 1,
          srcSchedulePosition.Weeks
        ).subscribe((isBusy) => {
          if (this.currentDragEvent != null) {
            this.scheduleSlotsValidity[srcSchedulePosition.Day - 1][srcSchedulePosition.PeriodIndex - 1] = !isBusy;
          }
        });
      }

    });
    
    this.signalrService.lastRemovedSchedulePositions.pipe(skip(1)).subscribe((removedSchedulePositions) => {
      console.log(removedSchedulePositions);
    });
  }

  ngOnInit(): void {
    let isConnectedSubscription = this.signalrService.isConnected.pipe(skip(1)).subscribe((status) => {
      this.connectionStatus = status;
      if (!status && !this.signalrService.connectionIntentionallyStopped) {
        this.snackBar.open("Connection with server has been lost. Please refresh the page to possibly reconnect.", "OK");
      }
    });

    this.setSignalrSubscriptions();

    forkJoin([
      this.signalrService.InitConnection(),
      this.scheduleDesignerApiService.GetSettings(),
      this.scheduleDesignerApiService.GetPeriods(),
      this.scheduleDesignerApiService.GetCourseTypes(),
      this.scheduleDesignerApiService.GetRoomTypes()
    ]).subscribe(([,settings,periods,courseTypes,roomTypes]) => {
      this.connectionStatus = true;
      
      this.settings = settings;
      this.settings.periods = periods;
      this.courseTypes = courseTypes;
      this.roomTypes = roomTypes;

      this.setLabels();
      this.setFrequenciesAndWeeks();
      this.initializeValues();

      this.getMyCourseEditionsAndScheduleAsCoordinator(0);
    }, (error) => {
      if (error?.status == 401) {
        this.usosApiService.Deauthorize();

        this.snackBar.open('Session expired. Please log in again.', 'OK');
        this.router.navigate(['login']);
      } else if (!isConnectedSubscription.closed) {
        this.snackBar.open("Connection with server failed. Please refresh the page to try again.", "OK");
      }
    });
  }

  private getMyCourseEditionsAndScheduleAsCoordinator(index:number) {
    let roundUp = (index != 1);

    forkJoin([
      this.scheduleDesignerApiService.GetMyCourseEditions(this.frequencies[index], this.courseTypes, this.settings, roundUp),
      this.scheduleDesignerApiService.GetScheduleAsCoordinator(this.weeks[index], this.courseTypes, this.settings)
    ]).subscribe(([myCourses, mySchedule]) => {
      this.myCourses = myCourses;
      this.schedule = mySchedule;

      let allGroups = new Array<Group>();
      let allRooms = new Array<Room>();
      
      for (let i = 0; i < this.myCourses.length; ++i) {
        for (let j = 0; j < this.myCourses[i].Groups.length; ++j) {
          allGroups.push(this.myCourses[i].Groups[j]);
        }
      }
      for (let i = 0; i < this.schedule.length; ++i) {
        for (let j = 0; j < this.schedule[i].length; ++j) {
          for (let k = 0; k < this.schedule[i][j].length; ++k) {
            for (let l = 0; l < this.schedule[i][j][k].Groups.length; ++l) {
              allGroups.push(this.schedule[i][j][k].Groups[l]);
            }
            allRooms.push(this.schedule[i][j][k].Room!);
          }
        }
      }

      forkJoin([
        this.scheduleDesignerApiService.GetGroupsFullNames(allGroups.map((e) => e.GroupId)),
        this.scheduleDesignerApiService.GetRoomsNames(allRooms.map((e) => e.RoomId))
      ]).subscribe(([groupsFullNames, roomsNames]) => {
        for (let i = 0; i < groupsFullNames.length; ++i) {
          allGroups[i].FullName = groupsFullNames[i];
        }

        for (let i = 0; i < roomsNames.length; ++i) {
          allRooms[i].Name = roomsNames[i];
        }
        
        this.loading = false;
        this.tabLoading = false;
      });

      //TEST
        /*setTimeout(() => {
          console.log("TEST");
          console.log(this.myCoursesSlot);
          console.log(this.scheduleSlots);
          if (this.currentDragEvent != null) {
            const numberOfSlots = this.settings.periods.length - 1;
            this.currentDragEvent.source.dropContainer.connectedTo = ['1,1', '1,4', '1,3'];
            console.log(this.currentDragEvent.source.dropContainer.connectedTo);
            this.currentDragEvent.source.dropContainer._dropListRef.enter(
              this.currentDragEvent.source._dragRef, 0, 0
            );
          }
        }, 5000);*/
    });
  }

  private setLabels() {
    for (let i:number = 0; i < this.settings.TermDurationWeeks; ++i) {
      this.tabLabels.push('Week ' + (i + 1));
    }
  }

  private setFrequenciesAndWeeks() {
    this.frequencies = [this.settings.TermDurationWeeks, this.settings.TermDurationWeeks / 2, this.settings.TermDurationWeeks / 2];
    this.weeks = [[],[],[]];

    for (let i:number = 0; i < this.settings.TermDurationWeeks; ++i) {
      this.frequencies.push(1);
      const weekNumber = i + 1;
      this.weeks[0].push(weekNumber);
      this.weeks.push([weekNumber]);
      if (weekNumber % 2 == 0) {
        this.weeks[1].push(weekNumber);
      } else {
        this.weeks[2].push(weekNumber);
      }
    }
  }

  private initializeValues() {
    const periods = this.settings.periods;
    const numberOfSlots = this.settings.periods.length - 1;
    
    for (let i:number = 0; i < numberOfSlots; ++i) {
      this.scheduleTimeLabels.push(
        periods[i] + ' - ' + periods[i + 1]
      );
    }

    this.scheduleSlotsValidity = [];
    for (let j:number = 0; j < 5; ++j) {
      this.scheduleSlotsValidity.push([]);
      for (let i:number = 0; i < numberOfSlots; ++i) {
        this.scheduleSlotsValidity[j].push(false);
      }
    }
  }

  private resetSchedule() {
    const numberOfSlots = this.settings.periods.length - 1;

    this.schedule = [];
    for (let j:number = 0; j < 5; ++j) {
      this.schedule.push([]);
      for (let i:number = 0; i < numberOfSlots; ++i) {
        this.schedule[j].push([]);
      }
    }
  }

  private getIndexes(id:string):number[] {
    const indexes = id.split(',');
    return [
      Number.parseInt(indexes[0]),
      Number.parseInt(indexes[1])
    ];
  }

  GetMaxElementIndexOnDay(dayIndex:number):number {
    let dayScheduleLength:number[] = [];
    for (let i = 0; i < this.settings.periods.length - 1; ++i) {
      dayScheduleLength.push(this.schedule[dayIndex][i].length);
    }
    return Math.max(...dayScheduleLength) - 1;
  }

  OnTabChange(index:number) {
    this.currentTabIndex = index;
    this.tabLoading = true;

    this.resetSchedule();
    this.getMyCourseEditionsAndScheduleAsCoordinator(index);
  }

  async DropInMyCourses(event:CdkDragDrop<CourseEdition[], CourseEdition[], CourseEdition>) {
    if (this.isCanceled) {
      this.currentDragEvent = null;
      this.isMoveValid = null;
      return;
    }

    const courseEdition = event.item.data;
    const previousContainer = event.previousContainer;
    const currentContainer = event.container;
    const weeks = this.weeks[this.currentTabIndex];
    const isScheduleSource = event.previousContainer.id !== 'my-courses';

    if (!isScheduleSource) {
      moveItemInArray(
        currentContainer.data,
        event.previousIndex,
        event.currentIndex
      );
    } else {
      const previousIndexes = this.getIndexes(previousContainer.id);

      try {
        const result = await this.signalrService.RemoveSchedulePositions(
          courseEdition.Room!.RoomId, previousIndexes[1] + 1,
          previousIndexes[0] + 1, weeks
        ).toPromise();
        if (result.StatusCode >= 400) {
          throw result;
        }

        transferArrayItem(
          previousContainer.data,
          currentContainer.data,
          event.previousIndex,
          event.currentIndex
        );
        courseEdition.Room = null;
        courseEdition.CurrentAmount = courseEdition.Weeks?.length ?? 0;
        courseEdition.Weeks = null;
      }
      catch (error:any) {
        this.snackBar.open(error.Message, "OK");
      }
    }
    
    if (!isScheduleSource) {
      try {
        const result = await this.signalrService.UnlockCourseEdition(
          courseEdition.CourseId, courseEdition.CourseEditionId
        ).toPromise();
        
        if (result.StatusCode >= 400) {
          throw result;
        }
        
        courseEdition.Locked = false;
      } catch (error) {
        
      }
    } else {
      try {
        const result = await this.scheduleDesignerApiService.IsCourseEditionLocked(
          courseEdition.CourseId, courseEdition.CourseEditionId
        ).toPromise();

        courseEdition.Locked = result;
      }
      catch (error) {

      }
    }
    this.currentDragEvent = null;
    this.isMoveValid = null;
  }

  async DropInSchedule(event:CdkDragDrop<CourseEdition[], CourseEdition[], CourseEdition>) {
    this.isReleased = true;

    if (this.isCanceled) {
      this.currentDragEvent = null;
      this.isMoveValid = null;
      return;
    }

    const courseEdition = event.item.data;
    const previousContainer = event.previousContainer;
    const currentContainer = event.container;
    const isScheduleSource = previousContainer.id !== 'my-courses';
    const previousIndexes = (isScheduleSource) ? this.getIndexes(previousContainer.id) : [-1,-1];
    const currentIndexes = this.getIndexes(currentContainer.id);
    const weeks = this.weeks[this.currentTabIndex];
    
    if (previousContainer === currentContainer) {
      try {
        const result = await this.signalrService.UnlockSchedulePositions(
          courseEdition.Room!.RoomId, previousIndexes[1] + 1,
          previousIndexes[0] + 1, weeks
        ).toPromise();
        
        if (result.StatusCode >= 400) {
          throw result;
        }
        courseEdition.Locked = false;
      } catch (error) {

      }
      this.currentDragEvent = null;
      this.isMoveValid = null;
      return;
    }

    const dialogData = new RoomSelectionDialogData(
      courseEdition,
      previousIndexes,
      currentIndexes,
      weeks,
      this.scheduleDayLabels,
      this.scheduleTimeLabels,
      this.roomTypes,
      this.isMoveValid!,
      isScheduleSource,
      this.account.UserId
    );
    this.currentOpenedDialog = this.dialog.open(RoomSelectionComponent, {
      disableClose: true,
      data: dialogData
    });
    const dialogResult:RoomSelectionDialogResult = await this.currentOpenedDialog.afterClosed().toPromise();
    this.currentOpenedDialog = null;

    switch (dialogResult.Status) {
      case RoomSelectionDialogStatus.ACCEPTED: {
        courseEdition.Room = dialogResult.Room;
        courseEdition.Weeks = dialogResult.Weeks;

        transferArrayItem(
          event.previousContainer.data,
          event.container.data,
          event.previousIndex,
          event.currentIndex
        );
      } break;
      case RoomSelectionDialogStatus.SCHEDULED: {

      } break;
      case RoomSelectionDialogStatus.CANCELED: {

      } break;
      case RoomSelectionDialogStatus.FAILED: {
        this.snackBar.open(dialogResult?.Message, "OK");
      } break;
    }

    if (!isScheduleSource) {
      try {
        const result = await this.signalrService.UnlockCourseEdition(
          courseEdition.CourseId, courseEdition.CourseEditionId
        ).toPromise();
        
        if (result.StatusCode >= 400) {
          throw result;
        }
        
        courseEdition.Locked = false;
      } catch (error) {
  
      }
    } else if (dialogResult.Status == RoomSelectionDialogStatus.ACCEPTED) {
      courseEdition.Locked = false;
    } else {
      try {
        const result = await this.signalrService.UnlockSchedulePositions(
          courseEdition.Room!.RoomId, previousIndexes[1] + 1,
          previousIndexes[0] + 1, weeks
        ).toPromise();
        
        if (result.StatusCode >= 400) {
          throw result;
        }
        
        courseEdition.Locked = false;
      } catch (error) {
  
      }
    }
    this.currentDragEvent = null;
    this.isMoveValid = null;
  }

  IsScheduleSlotDisabled(dayIndex:number, slotIndex:number) {
    return this.schedule[dayIndex][slotIndex].length > 1;
  }

  ScheduleSlotEnterPredicate(drag:CdkDrag<CourseEdition>, drop:CdkDropList<CourseEdition[]>) {
    return drop.data.length < 1;
  }

  OnMyCoursesEnter(drag:CdkDragEnter<CourseEdition[]>) {
    this.isMoveValid = null;
  }

  OnScheduleSlotEnter(drag:CdkDragEnter<CourseEdition[]>) {
    const indexes = this.getIndexes(drag.container.id);
    this.isMoveValid = this.scheduleSlotsValidity[indexes[0]][indexes[1]];
  }

  async OnStartDragging(event:CdkDragStart<CourseEdition>) {
    this.isReleased = false;
    this.isCanceled = false;
    this.currentDragEvent = event;
    
    const courseEdition = event.source.data;
    const dropContainer = event.source.dropContainer;
    const isScheduleSource = dropContainer.id !== 'my-courses';
    const indexes = (isScheduleSource) ? this.getIndexes(dropContainer.id) : [-1,-1];

    try {
      if (!this.isReleased) {
        const result = (!isScheduleSource) 
        ? await this.signalrService.LockCourseEdition(
          courseEdition.CourseId, courseEdition.CourseEditionId
        ).toPromise() 
        : await this.signalrService.LockSchedulePositions(
          courseEdition.Room!.RoomId, indexes[1] + 1,
          indexes[0] + 1, this.weeks[this.currentTabIndex]
        ).toPromise();
        
        if (result.StatusCode >= 400) {
          throw result;
        }
        courseEdition.Locked = true;
      } else {
        return;
      }
    } catch (error:any) {
      this.isCanceled = true;
      if (!this.isReleased) {
        dropContainer._dropListRef.enter(
          event.source._dragRef,
          0,0
        );
        document.dispatchEvent(new Event('mouseup'));
      } else {
        if (this.currentOpenedDialog != null) {
          this.currentOpenedDialog.close(RoomSelectionDialogResult.CANCELED);
        }
      }
      courseEdition.Locked = true;
      this.snackBar.open(error.Message, "OK");
      return;
    }

    let busySlots = await this.scheduleDesignerApiService.GetBusyPeriods(
      courseEdition.CourseId, 
      courseEdition.CourseEditionId,
      this.weeks[this.currentTabIndex]
    ).toPromise();
    let connectedTo = ['my-courses'];
    
    const numberOfSlots = this.settings.periods.length - 1;
    let busySlotIndex = 0;
    let scheduleSlots = this.scheduleSlots.toArray();
    
    if (isScheduleSource) {
      for (let i = 0; i < this.scheduleSlots.length; ++i) {
        let element = scheduleSlots[i].element as ElementRef<HTMLElement>;
        connectedTo.push(element.nativeElement.id);
        if (i != (busySlots[busySlotIndex]?.Day - 1) * numberOfSlots + (busySlots[busySlotIndex]?.PeriodIndex - 1)) {
          this.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = true;
        } else {
          this.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = false;
          ++busySlotIndex;
        }
      }
      this.scheduleSlotsValidity[indexes[0]][indexes[1]] = true;
    } else {
      for (let i = 0; i < this.scheduleSlots.length; ++i) {
        let element = scheduleSlots[i].element as ElementRef<HTMLElement>;
        if (i != (busySlots[busySlotIndex]?.Day - 1) * numberOfSlots + (busySlots[busySlotIndex]?.PeriodIndex - 1)) {
          this.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = true;
          connectedTo.push(element.nativeElement.id);
        } else {
          this.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = false;
          ++busySlotIndex;
        }
      }
    }

    dropContainer.connectedTo = connectedTo;
    
    if (!this.isReleased) {
      dropContainer._dropListRef.enter(
        event.source._dragRef, 0, 0
      );
    }
    
    if (this.isReleased) {
      try {
        const result = (!isScheduleSource) 
        ? await this.signalrService.UnlockCourseEdition(
          courseEdition.CourseId, courseEdition.CourseEditionId
        ).toPromise()
        : await this.signalrService.UnlockSchedulePositions(
          courseEdition.Room!.RoomId, indexes[1] + 1,
          indexes[0] + 1, this.weeks[this.currentTabIndex]
        ).toPromise();
        
        if (result.StatusCode >= 400) {
          throw result;
        }
        courseEdition.Locked = false;
      } catch (error) {
        
      }
      this.currentDragEvent = null;
      for (let i = 0; i < this.scheduleSlots.length; ++i) {
        this.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = false;
      }
    }
  }

  async OnReleaseDragging(event:CdkDragRelease<CourseEdition>) {
    this.isReleased = true;

    event.source.dropContainer.connectedTo = [];

    this.currentDragEvent = null;
    const numberOfSlots = this.settings.periods.length - 1;
    for (let i = 0; i < this.scheduleSlots.length; ++i) {
      this.scheduleSlotsValidity[Math.floor(i / numberOfSlots)][i % numberOfSlots] = false;
    }
  }

  Reset(dayIndex:number, slotIndex:number, event:CourseEdition) {
    transferArrayItem<CourseEdition>(
      this.schedule[dayIndex][slotIndex],
      this.myCourses,
      0,
      this.myCourses.length
    );
  }
}
