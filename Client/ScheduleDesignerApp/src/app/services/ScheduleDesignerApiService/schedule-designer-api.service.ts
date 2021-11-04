import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import * as OAuth from 'oauth-1.0a';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { AccessToken } from 'src/app/others/AccessToken';
import { Account, Coordinator, Titles } from 'src/app/others/Accounts';
import { CourseEdition } from 'src/app/others/CourseEdition';
import { CourseType } from 'src/app/others/CourseType';
import { Group } from 'src/app/others/Group';
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

  public GetMyCourseEditions(frequency:number, courseTypes:Map<number,CourseType>, settings:Settings, roundUp:boolean = true):Observable<CourseEdition[]> {
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

  public LockCourseEdition(courseId:number, courseEditionId:number):Observable<any> {
    const request = {
      url: this.baseUrl + `/courseEditions(${courseId},${courseEditionId})/Service.Lock`,
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

  public UnlockCourseEdition(courseId:number, courseEditionId:number):Observable<any> {
    const request = {
      url: this.baseUrl + `/courseEditions(${courseId},${courseEditionId})/Service.Unlock`,
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

  public GetFreePeriods():Observable<number[]> {
    const request = {
      url: this.baseUrl + '/schedulePositions/Service.GetFreePeriods()',
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
}
