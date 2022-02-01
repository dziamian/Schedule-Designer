import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { AccessToken } from 'src/app/others/AccessToken';
import { Account, Coordinator, CoordinatorBasic, Staff, Student, Titles, User } from 'src/app/others/Accounts';
import { CourseEdition } from 'src/app/others/CourseEdition';
import { CourseType, RoomType } from 'src/app/others/Types';
import { Group, GroupInfo } from 'src/app/others/Group';
import { Room } from 'src/app/others/Room';
import { ScheduleSlot } from 'src/app/others/ScheduleSlot';
import { Settings } from 'src/app/others/Settings';
import { CourseEditionInfo, CourseInfo } from 'src/app/others/CourseInfo';
import { ScheduledMove, ScheduledMoveDetails, ScheduledMoveInfo } from 'src/app/others/ScheduledMove';
import { Filter } from 'src/app/others/Filter';
import { Course } from 'src/app/others/Course';

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
      url: this.baseUrl + '/users/Service.GetMyAccount()?$expand=Student($expand=Groups),Coordinator,Staff',
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(map((response : any) => {
      const user = new User(
        response.UserId,
        response.FirstName,
        response.LastName
      );

      return new Account(
      user,
      response.Student != null ? new Student(
        user, 
        response.Student.StudentNumber, 
        response.Student.Groups.filter((group : any) => group.IsRepresentative).map((group : any) => group.GroupId) ?? []
      ) : null,
      response.Coordinator != null ? new Coordinator(
        user, 
        new Titles(response.Coordinator.TitleBefore, response.Coordinator.TitleAfter)
      ) : null,

      response.Staff != null ? new Staff(
        user,
        response.Staff.IsAdmin
      ) : null);
    }));
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

  public GetCourseType(id: number): Observable<CourseType> {
    const request = {
      url: this.baseUrl + `/courseTypes(${id})`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => new CourseType(response.CourseTypeId, response.Name, response.Color))
    );
  }

  public GetCourse(id: number): Observable<Course> {
    const request = {
      url: this.baseUrl + `/courses(${id})?$expand=CourseType`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => new Course(response.CourseId, new CourseType(
        response.CourseType.CourseTypeId, 
        response.CourseType.Name,
        response.CourseType.Color), response.Name, response.UnitsMinutes))
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

  public IsCourseEditionLocked(courseId:number, courseEditionId:number):Observable<{value: boolean, byAdmin: boolean}> {
    const request = {
      url: this.baseUrl + `/courseEditions(${courseId},${courseEditionId})?$expand=LockUser($select=Staff;$expand=Staff($select=IsAdmin))&$select=LockUserId`,
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
        return {
          value: response.LockUserId != null, byAdmin: response.LockUser?.Staff?.IsAdmin
        };
      })
    );
  }

  public GetCoordinators(): Observable<Coordinator[]> {
    const request = {
      url: this.baseUrl + `/coordinators?$expand=User`,
      method: 'GET'
    }

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => response.value.map((value : any) => 
        new Coordinator(
          new User(
            value.UserId,
            value.User.FirstName,
            value.User.LastName,
          ),
          new Titles(
            value.TitleBefore,
            value.TitleAfter
          )
        ))
      )
    );
  }

  public GetCoordinatorsFromUsers(usersIds:number[]):Observable<Coordinator[]> {
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
          new User(
            value.UserId,
            value.FirstName,
            value.LastName
          ),
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
    courseEditionId:number, 
    settings:Settings
  ):Observable<CourseEditionInfo> {
    const request = {
      url: this.baseUrl + `/courseEditions(${courseId},${courseEditionId})?`
      + `$expand=Course,LockUser($expand=Staff),SchedulePositions($count=true;$top=0)`,
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
          courseId, courseEditionId, response.Course.CourseTypeId, response.Course.Name, response.Name
        );
        courseEditionInfo.UnitsMinutes = response.Course.UnitsMinutes;
        courseEditionInfo.ScheduleAmount = response['SchedulePositions@odata.count'];
        courseEditionInfo.FullAmount = courseEditionInfo.UnitsMinutes / settings.CourseDurationMinutes;
        courseEditionInfo.IsLocked = response.LockUserId != null;
        courseEditionInfo.IsLockedByAdmin = response.LockUser?.Staff?.IsAdmin;
        return courseEditionInfo;
      })
    );
  }

  public GetCourseEditionBasicInfo(
    courseId: number,
    courseEditionId: number
  ):Observable<CourseEditionInfo> {
    const request = {
      url: this.baseUrl + `/courseEditions(${courseId},${courseEditionId})?$expand=Course`,
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
          response.CourseId, response.CourseEditionId, response.Course.CourseTypeId, response.Course.Name, response.Name
        );
        return courseEditionInfo;
      })
    );
  }

  public GetCoursesInfo():Observable<CourseInfo[]> {
    const request = {
      url: this.baseUrl + `/courses`,
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
        const courseInfo = new CourseInfo(
          element.CourseId, element.CourseTypeId, element.Name
        );
        return courseInfo;
      }))
    );
  }

  public GetCourseEditionsInfo(settings:Settings):Observable<CourseEditionInfo[]> {
    const request = {
      url: this.baseUrl + `/courseEditions`,
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
        const courseEditionInfo = new CourseEditionInfo(
          element.CourseId, element.CourseEditionId, -1, '', element.Name
        );
        return courseEditionInfo;
      }))
    );
  }

  public GetFilteredCourseEdition(
    courseId:number,
    courseEditionId:number,
    frequency:number, 
    filter:Filter,
    courseTypes:Map<number,CourseType>, 
    settings:Settings
  ):Observable<CourseEdition[]> {
    const FREQUENCY = Math.floor(frequency);
    const request = {
      url: this.baseUrl + `/courseEditions(${courseId},${courseEditionId})/Service.GetMyCourseEdition(${filter.toString()},Frequency=${FREQUENCY})?` +
        '$expand=Course,Groups,Coordinators($expand=Coordinator($expand=User)),LockUser($expand=Staff),' +
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
            new User(
              element.Coordinator.UserId,
              element.Coordinator.User.FirstName,
              element.Coordinator.User.LastName
            ),
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
          courseEdition.IsLocked = response.LockUserId;
          courseEdition.IsLockedByAdmin = response.LockUser?.Staff?.IsAdmin;
          courseEdition.ScheduleAmount = scheduleAmount;
          courseEdition.FullAmount = fullAmount;
          myCourseEditions.push(courseEdition);
        }

        return myCourseEditions;
      })
    );
  }

  public GetFilteredCourseEditions(
    frequency:number,
    filter:Filter,
    courseTypes:Map<number,CourseType>, 
    settings:Settings
  ):Observable<CourseEdition[]> {
    const FREQUENCY = Math.floor(frequency);
    const request = {
      url: this.baseUrl + `/courseEditions/Service.GetFilteredCourseEditions(${filter.toString()},Frequency=${FREQUENCY})?` +
        '$expand=Course,LockUser($expand=Staff),' +
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
          const scheduleAmount = value['SchedulePositions@odata.count'];
          const fullAmount = value.Course.UnitsMinutes / settings.CourseDurationMinutes;
          const fullAmountInteger = Math.ceil(fullAmount);
          const coursesAmount = Math.floor((fullAmountInteger - scheduleAmount) / FREQUENCY);
          for (let i = 0; i < coursesAmount; ++i) {
            let courseEdition = new CourseEdition(
              value.CourseId, value.CourseEditionId,
              value.Course.Name, 
              courseTypes.get(value.Course.CourseTypeId) ?? new CourseType(0, "", ""),
              FREQUENCY,
              [], 
              []
            );
            courseEdition.IsLocked = value.LockUserId;
            courseEdition.IsLockedByAdmin = value.LockUser?.Staff?.IsAdmin;
            courseEdition.ScheduleAmount = scheduleAmount;
            courseEdition.FullAmount = fullAmount;
            myCourseEditions.push(courseEdition);
          }
        });

        return myCourseEditions;
      })
    );
  }

  public GetCourseEditionGroupsSize(courseId:number, courseEditionId:number):Observable<number> {
    const request = {
      url: this.baseUrl + `/courseEditions(${courseId},${courseEditionId})/Service.GetCourseEditionGroupsSize()`,
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

  public AreSchedulePositionsLocked(
    roomId:number, periodIndex:number,
    day:number, weeks:number[]
  ):Observable<{value: boolean, byAdmin: boolean}> {
    const request = {
      url: this.baseUrl + `/schedulePositions/Service.GetSchedulePositions(`
      + `RoomId=${roomId},PeriodIndex=${periodIndex},Day=${day},Weeks=[${weeks.toString()}])?$expand=LockUser($select=Staff;$expand=Staff($select=IsAdmin))&$select=LockUserId`,
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
        return {
          value: response.value.find((x:any) => x.LockUserId != null) != undefined,
          byAdmin: response.value.find((x:any) => x.LockUser?.Staff?.IsAdmin) != undefined
        }
      })
    );
  }

  public GetCoursesForSchedule(
    courseEditions: CourseEdition[], //sorted by courseId
    courseTypes:Map<number,CourseType>,
    settings:Settings
  ):Observable<CourseEdition[]> {
    const request = {
      url: this.baseUrl + `/courses?$filter=CourseId in (${[...new Set(courseEditions.map(c => c.CourseId))]})&$orderby=CourseId`,
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
        var j = 0;

        const courseEditionsLength = courseEditions.length;
        for (var i = 0; i < courseEditionsLength; ++i) {
          if (response.value[j].CourseId != courseEditions[i].CourseId) {
            ++j;
          }

          if (response.value[j].CourseId == courseEditions[i].CourseId) {
            courseEditions[i].Name = response.value[j].Name;
            courseEditions[i].Type = courseTypes.get(response.value[j].CourseTypeId) ?? new CourseType(0, "", "");
            courseEditions[i].FullAmount = response.value[j].UnitsMinutes / settings.CourseDurationMinutes;
          }
        }

        return courseEditions;
      })
    );
  }

  public GetScheduleAmount(
    courseEditions: CourseEdition[] //sorted by courseEditionId
  ):Observable<CourseEdition[]> {
    const request = {
      url: this.baseUrl + `/schedulePositions/Service.GetScheduleAmount(CourseEditionIds=[${[...new Set(courseEditions.map(c => c.CourseEditionId))]}])?$orderby=CourseEditionId`,
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
        var j = 0;

        const courseEditionsLength = courseEditions.length;
        for (var i = 0; i < courseEditionsLength; ++i) {
          if (response.value[j].CourseEditionId != courseEditions[i].CourseEditionId) {
            ++j;
          }

          if (response.value[j].CourseEditionId == courseEditions[i].CourseEditionId) {
            courseEditions[i].ScheduleAmount = response.value[j].Count;
          }
        }

        return courseEditions;
      })
    );
  }

  public GetCourseEditionCoordinatorsBasic(courseEditionId: number):Observable<CoordinatorBasic[]> {
    const request = {
      url: this.baseUrl + `/coordinatorCourseEditions?$expand=Coordinator($expand=User)&$filter=CourseEditionId eq ${courseEditionId}`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => response.value.map((element : any) => new CoordinatorBasic(
        element.Coordinator.UserId,
        `${element.Coordinator.TitleBefore ?? ''} ${element.Coordinator.User.LastName.toUpperCase()} ${element.Coordinator.User.FirstName} ${element.Coordinator.TitleAfter ?? ''} (${element.Coordinator.UserId})`
      )))
    );
  }

  public GetCoordinatorsForCourses(
    courseEditions: CourseEdition[] //sorted by courseEditionId
  ):Observable<CourseEdition[]> {
    const request = {
      url: this.baseUrl + `/coordinatorCourseEditions?$expand=Coordinator($expand=User)&$filter=CourseEditionId in (${[...new Set(courseEditions.map(c => c.CourseEditionId))]})&$orderby=CourseEditionId`,
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
        var j = 0;
        var nextElement = j + 1;

        const courseEditionsLength = courseEditions.length;
        for (var i = 0; i < courseEditionsLength; ++i) {
          if (response.value[j].CourseEditionId != courseEditions[i].CourseEditionId) {
            j = nextElement;
          }

          var k = j;
          while (response.value[k]?.CourseEditionId == courseEditions[i].CourseEditionId) {
            courseEditions[i].Coordinators.push(new Coordinator(
              new User(
                response.value[k].Coordinator.UserId,
                response.value[k].Coordinator.User.FirstName,
                response.value[k].Coordinator.User.LastName
              ),
              new Titles(
                response.value[k].Coordinator.TitleBefore,
                response.value[k].Coordinator.TitleAfter
              )));

            ++k;
          }
          nextElement = k;
        }

        return courseEditions;
      })
    );
  }

  public GetCourseEditionGroupsBasic(courseEditionId: number):Observable<Group[]> {
    const request = {
      url: this.baseUrl + `/groupCourseEditions?$filter=CourseEditionId eq ${courseEditionId}`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => response.value.map((element : any) => new Group(
        element.GroupId
      )))
    );
  }

  public GetGroupsForCourses(
    courseEditions: CourseEdition[] //sorted by courseEditionId
  ):Observable<CourseEdition[]> {
    const request = {
      url: this.baseUrl + `/groupCourseEditions?$filter=CourseEditionId in (${[...new Set(courseEditions.map(c => c.CourseEditionId))]})&$orderby=CourseEditionId`,
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
        var j = 0;
        var nextElement = j + 1;

        const courseEditionsLength = courseEditions.length;
        for (var i = 0; i < courseEditionsLength; ++i) {
          if (response.value[j].CourseEditionId != courseEditions[i].CourseEditionId) {
            j = nextElement;
          }

          var k = j;
          while (response.value[k]?.CourseEditionId == courseEditions[i].CourseEditionId) {
            courseEditions[i].Groups.push(new Group(response.value[k].GroupId));

            ++k;
          }
          nextElement = k;
        }

        return courseEditions;
      })
    );
  }

  public GetFilteredSchedule(
    weeks:number[],
    filter: Filter,
    settings:Settings
  ):Observable<{schedule: CourseEdition[][][], courseEditions: CourseEdition[]}> {
    const request = {
      url: this.baseUrl + `/schedulePositions/Service.GetFilteredSchedule(${filter.toString()},Weeks=[${weeks.toString()}])?
        $expand=Timestamp,LockUser($expand=Staff),ScheduledMovePositions($expand=ScheduledMove($select=MoveId,UserId,IsConfirmed))`,
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
        const numberOfSlots = settings.Periods.length - 1;
        
        let schedule:CourseEdition[][][] = [];
        for (let j:number = 0; j < 5; ++j) {
          schedule.push([]);
          for (let i:number = 0; i < numberOfSlots; ++i) {
            schedule[j].push([]);
          }
        }

        let courseEditions: CourseEdition[] = [];

        response.value.forEach((value : any) => {
          
          const courseId = value.CourseId;
          const courseEditionId = value.CourseEditionId;
          const roomId = value.RoomId;
          const dayIndex = value.Timestamp.Day - 1;
          const periodIndex = value.Timestamp.PeriodIndex - 1;
          const week = value.Timestamp.Week;
          const locked = {value: value.LockUserId != null, byAdmin: value.LockUser?.Staff?.IsAdmin};
          const scheduledMoves = value.ScheduledMovePositions.map((value : any) => new ScheduledMove(value.ScheduledMove.MoveId, value.ScheduledMove.UserId, value.ScheduledMove.IsConfirmed));

          let scheduleSlot = schedule[dayIndex][periodIndex];
          let found = false;
          for (let i = 0; i < scheduleSlot.length; ++i) {
            let courseEdition = scheduleSlot[i];
            if (courseEdition.CourseId == courseId && courseEdition.CourseEditionId == courseEditionId
              && courseEdition.Room!.RoomId == roomId) {
                courseEdition.Weeks?.push(week);
                if (locked) {
                  courseEdition.IsLocked = locked.value;
                  courseEdition.IsLockedByAdmin = locked.byAdmin;
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
              "",
              new CourseType(0, "", ""),
              0,
              [],
              []
            );
            courseEdition.Room = new Room(roomId);
            courseEdition.IsLocked = locked.value;
            courseEdition.IsLockedByAdmin = locked.byAdmin;
            courseEdition.Weeks = [week];
            courseEdition.ScheduledMoves.push(...scheduledMoves);
            
            scheduleSlot.push(courseEdition);
            courseEditions.push(courseEdition);
          }
        });

        return {schedule: schedule, courseEditions: courseEditions};
      })
    );
  }

  public GetRooms(roomsTypes:Map<number,RoomType> = new Map<number, RoomType>()):Observable<Room[]> {
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
          room.Capacity = element.Capacity;
          room.RoomType = roomsTypes.get(element.RoomTypeId) ?? new RoomType(element.RoomTypeId, "");
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
      url: this.baseUrl + `/courseRooms?$expand=Room,User&$filter=CourseId eq ${courseId}`,
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
          room.Capacity = element.Room.Capacity;
          room.RoomType = roomsTypes.get(element.Room.RoomTypeId) ?? new RoomType(element.Room.RoomTypeId, "");
          if (element.User != null) {
            room.User = new User(element.User.UserId, element.User.FirstName, element.User.LastName);
          }
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

  public AddCourseRoom(courseId:number, roomId:number):Observable<any> {
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
          'RoomId': roomId
        },
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    );
  }

  public GetGroups(): Observable<Group[]> {
    const request = {
      url: this.baseUrl + `/groups`,
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
        const group = new Group(element.GroupId);
        group.ParentGroupId = element.ParentGroupId;
        group.FullName = element.Name;
        return group;
      }))
    );
  }

  public GetMyGroups(): Observable<Group[]> {
    const request = {
      url: this.baseUrl + `/studentGroups/Service.GetMyGroups()`,
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
        const group = new Group(element.GroupId);
        group.ParentGroupId = element.ParentGroupId;
        group.FullName = element.Name;
        return group;
      }))
    );
  }

  public GetGroupInfo(groupId: number): Observable<GroupInfo> {
    const request = {
      url: this.baseUrl + `/groups(${groupId})/Service.GetGroupFullInfo()`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => new GroupInfo(
        response.GroupId, response.BasicName, response.FullName, response.ParentIds, response.ChildIds
      ))
    );
  }

  public GetGroupsInfo(groupsIds: number[]): Observable<GroupInfo[]> {
    const request = {
      url: this.baseUrl + `/groups/Service.GetGroupsFullInfo(GroupsIds=[${groupsIds.toString()}])`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => response.value.map((element : any) => new GroupInfo(
        element.GroupId, element.BasicName, element.FullName, element.ParentIds, element.ChildIds
      )))
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
          move.UserId,
          move.SourceWeeks,
          room,
          move.DestPeriodIndex,
          move.DestDay,
          move.DestWeeks);
      }))
    );
  }

  public GetScheduledMoveInfo(moveId: number):Observable<ScheduledMoveInfo> {
    const request = {
      url: this.baseUrl + `/scheduledMoves(${moveId})?$expand=User($expand=Coordinator),Message&$select=User`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => new ScheduledMoveInfo(
        response.User.FirstName,
        response.User.LastName,
        response.User?.Coordinator ? new Titles(response.User.Coordinator.TitleBefore, response.User.Coordinator.TitleAfter) : null,
        response.Message?.Content
      ))
    );
  }
}
