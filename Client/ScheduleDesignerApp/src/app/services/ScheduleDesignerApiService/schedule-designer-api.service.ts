import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import * as OAuth from 'oauth-1.0a';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { AccessToken } from 'src/app/others/AccessToken';
import { Account, Coordinator, Titles } from 'src/app/others/Accounts';
import { CourseEdition } from 'src/app/others/CourseEdition';
import { CourseType, RoomType } from 'src/app/others/Types';
import { Group } from 'src/app/others/Group';
import { Room } from 'src/app/others/Room';
import { ScheduleSlot } from 'src/app/others/ScheduleSlot';
import { Settings } from 'src/app/others/Settings';

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

  public GetMyCourseEditions(
    frequency:number, 
    courseTypes:Map<number,CourseType>, 
    settings:Settings, 
    roundUp:boolean = true
  ):Observable<CourseEdition[]> {
    const request = {
      url: this.baseUrl + `/courseEditions/Service.GetMyCourseEditions(Frequency=${frequency})?` +
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

          let coursesAmount = value.Course.UnitsMinutes - value['SchedulePositions@odata.count'] * settings.CourseDurationMinutes
          coursesAmount /= frequency * settings.CourseDurationMinutes;
          coursesAmount = Math.floor(coursesAmount);
          for (let i = 0; i < coursesAmount; ++i) {
            let courseEdition = new CourseEdition(
              value.CourseId, 
              value.CourseEditionId,
              value.Course.Name,
              courseTypes.get(value.Course.CourseTypeId) ?? new CourseType(0, "", ""),
              (roundUp) ? Math.ceil(frequency) : Math.floor(frequency),
              groups,
              coordinators
            );
            courseEdition.Locked = value.LockUserId;
            myCourseEditions.push(courseEdition);
          }
        });

        return myCourseEditions;
      })
    );
  }

  public GetScheduleAsCoordinator(
    weeks:number[], 
    courseTypes:Map<number,CourseType>,
    settings:Settings
  ):Observable<CourseEdition[][][]> {
    const request = {
      url: this.baseUrl + `/schedulePositions/Service.GetScheduleAsCoordinator(Weeks=[${weeks.toString()}])?` +
        '$expand=CourseEdition($expand=Course,Coordinators($expand=Coordinator($expand=User)),Groups),' +
        'CourseRoomTimestamp($expand=Timestamp)',
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
          let scheduleSlot = schedule[dayIndex][periodIndex];
          let found = false;
          for (let i = 0; i < scheduleSlot.length; ++i) {
            let courseEdition = scheduleSlot[i];
            if (courseEdition.CourseId == courseId && courseEdition.CourseEditionId == courseEditionId
              && courseEdition.Room!.RoomId == roomId) {
              courseEdition.Weeks?.push(week);
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
            courseEdition.Locked = value.LockUserId;
            courseEdition.Weeks = [week];
            scheduleSlot.push(courseEdition);
          }
        });

        return schedule;
      })
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
      map((response : any) => //{
        //let rooms:Map<number,Room[]> = new Map<number,Room[]>();

        response.value.map((element : any) => {
          const room = new Room(element.RoomId);
          room.Name = element.Room.Name;
          room.RoomType = roomsTypes.get(element.Room.RoomTypeId) ?? new RoomType(0, "");
          return room;
          //let currentRooms:Room[]|undefined = rooms.get(room.RoomType.RoomTypeId);
          //if (currentRooms == undefined) {
            //rooms.set(room.RoomType.RoomTypeId, new Array<Room>(room));
          //} else {
            //currentRooms.push(room);
            //rooms.set(room.RoomType.RoomTypeId, currentRooms);
          //}
        })

        //return rooms;
      //})
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

  //TESTING
  public AddSchedulePositions(
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
  }
}
