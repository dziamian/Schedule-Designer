import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map, min } from 'rxjs/operators';
import { AccessToken } from 'src/app/others/AccessToken';
import { Account, Coordinator, Titles } from 'src/app/others/Accounts';
import { CourseEdition } from 'src/app/others/CourseEdition';
import { CourseType, RoomType } from 'src/app/others/Types';
import { Group } from 'src/app/others/Group';
import { Room } from 'src/app/others/Room';
import { ScheduleSlot } from 'src/app/others/ScheduleSlot';
import { Settings } from 'src/app/others/Settings';
import { CourseEditionInfo } from 'src/app/others/CourseEditionInfo';
import { ScheduledMove, ScheduledMoveDetails } from 'src/app/others/ScheduledMove';

@Injectable({
  providedIn: 'root'
})
export class ScheduleDesignerApiService {

  readonly baseUrl:string = 'http://localhost:5000/api';

  constructor(private http:HttpClient) { }

  private GetAuthorizationHeaders(token:any) {
    return {
      "AccessToken": token.key,
      "AccessTokenSecret": token.secret
    };
  }

  public GetMyAccount():Observable<Account> {
    const request = {
      url: this.baseUrl + '/users/Service.GetMyAccount()?$expand=Student,Coordinator,Staff',
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(map((response : any) => new Account(
      response.UserId,
      response.FirstName,
      response.LastName,
      response.Student != null,
      response.Coordinator != null,
      (response.Coordinator != null) ? new Titles(response.Coordinator.TitleBefore, response.Coordinator.TitleAfter) : null,
      response.Staff != null,
      response.Staff?.IsAdmin ?? false
    )));
  }

  public CreateMyAccount():Observable<any> {
    const request = {
      url: this.baseUrl + '/users/Service.CreateMyAccount()',
      method: 'POST'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    );
  }

  public GetSettings():Observable<Settings> {
    const request = {
      url: this.baseUrl + '/settings',
      method: 'GET'
    };
    
    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(map((response : any) => new Settings(
      response.CourseDurationMinutes,
      response.StartTime,
      response.EndTime,
      response.TermDurationWeeks
    )));
  }

  public GetCourseTypes():Observable<Map<number,CourseType>> {
    const request = {
      url: this.baseUrl + '/courseTypes',
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => {
        let map = new Map<number,CourseType>();
        
        response.value.forEach((element : any) => {
          map.set(element.CourseTypeId, new CourseType(element.CourseTypeId, element.Name, element.Color));
        });
        
        return map;
      })
    );
  }

  public GetRoomTypes():Observable<Map<number,RoomType>> {
    const request = {
      url: this.baseUrl + '/roomTypes',
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => {
        let map = new Map<number,RoomType>();
        
        response.value.forEach((element : any) => {
          map.set(element.RoomTypeId, new RoomType(element.RoomTypeId, element.Name));
        });
        
        return map;
      })
    );
  }

  public GetPeriods():Observable<string[]> {
    const request = {
      url: this.baseUrl + '/settings/Service.GetPeriods()',
      method: 'GET'
    };
    
    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => response.value)
    );
  }

  public IsCourseEditionLocked(courseId:number, courseEditionId:number):Observable<boolean> {
    const request = {
      url: this.baseUrl + `/courseEditions(${courseId},${courseEditionId})?$select=LockUserId`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => response.LockUserId != null)
    );
  }

  public GetCoordinators(usersIds:number[]):Observable<Coordinator[]> {
    const request = {
      url: this.baseUrl + `/users?$expand=Coordinator&$filter=UserId in [${usersIds.toString()}]`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => response.value.map((value : any) => 
        new Coordinator(
          value.UserId,
          value.FirstName,
          value.LastName,
          new Titles(
            value.Coordinator.TitleBefore,
            value.Coordinator.TitleAfter
          )
        ))
      )
    );
  }

  public GetCourseEditionInfo(
    courseId:number, 
    courseEdition:number, 
    settings:Settings
  ):Observable<CourseEditionInfo> {
    const request = {
      url: this.baseUrl + `/courseEditions(${courseId},${courseEdition})?`
      + `$expand=Course,SchedulePositions($count=true;$top=0)`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => {
        const courseEditionInfo = new CourseEditionInfo(
          courseId, response.Course.CourseTypeId, response.Course.Name
        );
        courseEditionInfo.UnitsMinutes = response.Course.UnitsMinutes;
        courseEditionInfo.ScheduleAmount = response['SchedulePositions@odata.count'];
        courseEditionInfo.FullAmount = courseEditionInfo.UnitsMinutes / settings.CourseDurationMinutes;
        courseEditionInfo.Locked = response.LockUserId != null;
        return courseEditionInfo;
      })
    );
  }

  public GetMyCourseEdition(
    courseId:number,
    courseEditionId:number,
    frequency:number, 
    courseTypes:Map<number,CourseType>, 
    settings:Settings
  ):Observable<CourseEdition[]> {
    const FREQUENCY = Math.floor(frequency);
    const request = {
      url: this.baseUrl + `/courseEditions(${courseId},${courseEditionId})/Service.GetMyCourseEdition(Frequency=${FREQUENCY})?` +
        '$expand=Course,Groups,Coordinators($expand=Coordinator($expand=User)),' +
        'SchedulePositions($count=true;$top=0)',
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => {
        let myCourseEditions = new Array<CourseEdition>();
        
        let groups = new Array<Group>();
        response.Groups.forEach((element : any) => {
          groups.push(new Group(
            element.GroupId
          ));
        });
        let coordinators = new Array<Coordinator>();
        response.Coordinators.forEach((element : any) => {
          coordinators.push(new Coordinator(
            element.Coordinator.UserId,
            element.Coordinator.User.FirstName,
            element.Coordinator.User.LastName,
            new Titles(
              element.Coordinator.TitleBefore,
              element.Coordinator.TitleAfter
            )
          ));
        });
        const scheduleAmount = response['SchedulePositions@odata.count'];
        const fullAmount = response.Course.UnitsMinutes / settings.CourseDurationMinutes;
        const fullAmountInteger = Math.ceil(fullAmount);
        const coursesAmount = Math.floor((fullAmountInteger - scheduleAmount) / FREQUENCY);
        for (let i = 0; i < coursesAmount; ++i) {
          let courseEdition = new CourseEdition(
            response.CourseId, response.CourseEditionId,
            response.Course.Name, courseTypes.get(response.Course.CourseTypeId) ?? new CourseType(0, "", ""),
            FREQUENCY,
            groups, coordinators
          );
          courseEdition.Locked = response.LockUserId;
          courseEdition.ScheduleAmount = scheduleAmount;
          courseEdition.FullAmount = fullAmount;
          myCourseEditions.push(courseEdition);
        }

        return myCourseEditions;
      })
    );
  }

  public GetMyCourseEditions(
    frequency:number, 
    courseTypes:Map<number,CourseType>, 
    settings:Settings
  ):Observable<CourseEdition[]> {
    const FREQUENCY = Math.floor(frequency);
    const request = {
      url: this.baseUrl + `/courseEditions/Service.GetMyCourseEditions(Frequency=${FREQUENCY})?` +
        '$expand=Course,Groups,Coordinators($expand=Coordinator($expand=User)),' +
        'SchedulePositions($count=true;$top=0)',
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => {
        let myCourseEditions = new Array<CourseEdition>();

        response.value.forEach((value : any) => {
          let groups = new Array<Group>();
          value.Groups.forEach((element : any) => {
            groups.push(new Group(
              element.GroupId
            ));
          });
          let coordinators = new Array<Coordinator>();
          value.Coordinators.forEach((element : any) => {
            coordinators.push(new Coordinator(
              element.Coordinator.UserId,
              element.Coordinator.User.FirstName,
              element.Coordinator.User.LastName,
              new Titles(
                element.Coordinator.TitleBefore,
                element.Coordinator.TitleAfter
              )
            ));
          });
          const scheduleAmount = value['SchedulePositions@odata.count'];
          const fullAmount = value.Course.UnitsMinutes / settings.CourseDurationMinutes;
          const fullAmountInteger = Math.ceil(fullAmount);
          const coursesAmount = Math.floor((fullAmountInteger - scheduleAmount) / FREQUENCY);
          for (let i = 0; i < coursesAmount; ++i) {
            let courseEdition = new CourseEdition(
              value.CourseId, value.CourseEditionId,
              value.Course.Name, courseTypes.get(value.Course.CourseTypeId) ?? new CourseType(0, "", ""),
              FREQUENCY,
              groups, coordinators
            );
            courseEdition.Locked = value.LockUserId;
            courseEdition.ScheduleAmount = scheduleAmount;
            courseEdition.FullAmount = fullAmount;
            myCourseEditions.push(courseEdition);
          }
        });

        return myCourseEditions;
      })
    );
  }

  public AreSchedulePositionsLocked(
    roomId:number, periodIndex:number,
    day:number, weeks:number[]
  ):Observable<boolean> {
    const request = {
      url: this.baseUrl + `/schedulePositions/Service.GetSchedulePositions(`
      + `RoomId=${roomId},PeriodIndex=${periodIndex},Day=${day},Weeks=[${weeks.toString()}])?$select=LockUserId`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => response.value.find((x:any) => x.LockUserId != null) != undefined)
    );
  }

  public GetScheduleAsCoordinator(
    weeks:number[], 
    courseTypes:Map<number,CourseType>,
    settings:Settings
  ):Observable<CourseEdition[][][]> {
    const request = {
      url: this.baseUrl + `/schedulePositions/Service.GetScheduleAsCoordinator(Weeks=[${weeks.toString()}])?` +
        '$expand=CourseEdition($expand=Course,Coordinators($expand=Coordinator($expand=User)),Groups,SchedulePositions($count=true;$top=0)),' +
        'CourseRoomTimestamp($expand=Timestamp),ScheduledMoves($select=MoveId,IsConfirmed)',
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => {
        const numberOfSlots = settings.periods.length - 1;
        let schedule:CourseEdition[][][] = [];
        for (let j:number = 0; j < 5; ++j) {
          schedule.push([]);
          for (let i:number = 0; i < numberOfSlots; ++i) {
            schedule[j].push([]);
          }
        }

        response.value.forEach((value : any) => {
          let groups = new Array<Group>();
          value.CourseEdition.Groups.forEach((element : any) => {
            groups.push(new Group(
              element.GroupId
            ));
          });
          let coordinators = new Array<Coordinator>();
          value.CourseEdition.Coordinators.forEach((element : any) => {
            coordinators.push(new Coordinator(
              element.Coordinator.UserId,
              element.Coordinator.User.FirstName,
              element.Coordinator.User.LastName,
              new Titles(
                element.Coordinator.TitleBefore,
                element.Coordinator.TitleAfter
              )
            ));
          });

          const courseId = value.CourseId;
          const courseEditionId = value.CourseEditionId;
          const roomId = value.RoomId;
          const dayIndex = value.CourseRoomTimestamp.Timestamp.Day - 1;
          const periodIndex = value.CourseRoomTimestamp.Timestamp.PeriodIndex - 1;
          const week = value.CourseRoomTimestamp.Timestamp.Week;
          const locked = value.LockUserId != null;
          const scheduleAmount = value.CourseEdition['SchedulePositions@odata.count'];
          const fullAmount = value.CourseEdition.Course.UnitsMinutes / settings.CourseDurationMinutes;
          const scheduledMoves = value.ScheduledMoves.map((value : ScheduledMove) => new ScheduledMove(value.MoveId, value.IsConfirmed));
          let scheduleSlot = schedule[dayIndex][periodIndex];
          let found = false;
          for (let i = 0; i < scheduleSlot.length; ++i) {
            let courseEdition = scheduleSlot[i];
            if (courseEdition.CourseId == courseId && courseEdition.CourseEditionId == courseEditionId
              && courseEdition.Room!.RoomId == roomId) {
                courseEdition.Weeks?.push(week);
                if (locked) {
                  courseEdition.Locked = true;
                }
                
                const currentMovesIds = courseEdition.ScheduledMoves.map(scheduledMove => scheduledMove.MoveId);
                const notAddedScheduledMoves = scheduledMoves.filter((scheduledMove : ScheduledMove) => !currentMovesIds.includes(scheduledMove.MoveId));
                courseEdition.ScheduledMoves.push(...notAddedScheduledMoves);
                
                found = true;
            }
          }

          if (!found) {
            const courseEdition = new CourseEdition(
              value.CourseId, 
              value.CourseEditionId,
              value.CourseEdition.Course.Name,
              courseTypes.get(value.CourseEdition.Course.CourseTypeId) ?? new CourseType(0, "", ""),
              0,
              groups,
              coordinators
            );
            courseEdition.Room = new Room(roomId);
            courseEdition.Locked = locked;
            courseEdition.Weeks = [week];
            courseEdition.ScheduleAmount = scheduleAmount;
            courseEdition.FullAmount = fullAmount;
            courseEdition.ScheduledMoves.push(...scheduledMoves);
            
            scheduleSlot.push(courseEdition);
          }
        });

        return schedule;
      })
    );
  }

  public GetRooms(roomsTypes:Map<number,RoomType>):Observable<Room[]> {
    const request = {
      url: this.baseUrl + `/rooms`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) =>
        response.value.map((element : any) => {
          const room = new Room(element.RoomId);
          room.Name = element.Name;
          room.RoomType = roomsTypes.get(element.RoomTypeId) ?? new RoomType(0, "");
          return room;
        })
      )
    );
  }

  public GetRoomsNames(roomsIds:number[]):Observable<string[]> {
    const request = {
      url: this.baseUrl + `/rooms/Service.GetRoomsNames(RoomsIds=[${roomsIds.toString()}])`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => response.value.map((element : any) => element.Name))
    );
  }

  public GetCourseRooms(courseId:number, roomsTypes:Map<number,RoomType>):Observable<Room[]> {
    const request = {
      url: this.baseUrl + `/courseRooms?$expand=Room&$filter=CourseId eq ${courseId}`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) =>
        response.value.map((element : any) => {
          const room = new Room(element.RoomId);
          room.Name = element.Room.Name;
          room.RoomType = roomsTypes.get(element.Room.RoomTypeId) ?? new RoomType(0, "");
          return room;
        })
      )
    );
  }

  public GetRoomsAvailability(
    roomsIds:number[], 
    periodIndex:number, 
    day:number, 
    weeks:number[]):Observable<Room[]> {
      const request = {
        url: this.baseUrl + `/schedulePositions/Service.GetRoomsAvailability(RoomsIds=[${roomsIds.toString()}],` +
        `PeriodIndex=${periodIndex},Day=${day},Weeks=[${weeks.toString()}])`,
        method: 'GET'
      };
  
      return this.http.request(
        request.method,
        request.url,
        {
          headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
        }
      ).pipe(
        map((response : any) => response.value.map((element : any) => {
          const room = new Room(element.RoomId);
          room.IsBusy = element.IsBusy;
          return room;
        }))
      );
  }

  public AddCourseRoom(courseId:number, roomId:number, userId:number):Observable<any> {
    const request = {
      url: this.baseUrl + `/courseRooms`,
      method: 'POST'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        body: {
          'CourseId': courseId,
          'RoomId': roomId,
          'UserId': userId
        },
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    );
  }

  public GetGroupsFullNames(groupsIds:number[]):Observable<string[]> {
    const request = {
      url: this.baseUrl + `/groups/Service.GetGroupsFullNames(GroupsIds=[${groupsIds.toString()}])`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => response.value.map((element : any) => element.FullName))
    );
  }

  public GetBusyPeriods(courseId:number, courseEditionId:number, weeks:number[]):Observable<ScheduleSlot[]> {
    const request = {
      url: this.baseUrl + `/courseEditions(${courseId},${courseEditionId})/Service.GetBusyPeriods(Weeks=[${weeks.toString()}])?` +
      `$apply=groupby((PeriodIndex,Day))&$orderby=Day asc, PeriodIndex asc`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => response.map(
        (element : any) => new ScheduleSlot(element.PeriodIndex, element.Day)
      ))
    );
  }

  public IsPeriodBusy(
    courseId:number, courseEditionId:number,
    periodIndex:number, day:number,
    weeks:number[]
  ):Observable<boolean> {
    const request = {
      url: this.baseUrl + `/courseEditions(${courseId},${courseEditionId})/Service.IsPeriodBusy(`
      + `PeriodIndex=${periodIndex},Day=${day},Weeks=[${weeks.toString()}])`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => response.value)
    );
  }

  public GetConcreteScheduledMoves(movesIds:number[], roomsTypes:Map<number,RoomType>):Observable<ScheduledMoveDetails[]> {
    const request = {
      url: this.baseUrl + `/scheduledMoves/Service.GetConcreteScheduledMoves(MovesIds=[${movesIds.toString()}])`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => response.value.map((move : any) => {
        const room = new Room(move.DestRoomId);
        room.Name = move.DestRoomName;
        room.RoomType = roomsTypes.get(move.DestRoomTypeId) ?? new RoomType(0, "");
        return new ScheduledMoveDetails(
          move.MoveId,
          move.IsConfirmed,
          move.SourceWeeks,
          room,
          move.DestPeriodIndex,
          move.DestDay,
          move.DestWeeks);
      }))
    );
  }

  //TESTING
  /*public AddSchedulePositions(
    courseId:number,
    courseEditionId:number,
    roomId:number,
    periodIndex:number,
    day:number,
    weeks:number[]
  ):Observable<any> {
    const request = {
      url: this.baseUrl + `/schedulePositions/Service.AddSchedulePositions()`,
      method: 'POST'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        body: {
          'CourseId': courseId,
          'CourseEditionId': courseEditionId,
          'RoomId': roomId,
          'PeriodIndex': periodIndex,
          'Day': day,
          'Weeks': weeks
        },
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    );
  }*/
}
