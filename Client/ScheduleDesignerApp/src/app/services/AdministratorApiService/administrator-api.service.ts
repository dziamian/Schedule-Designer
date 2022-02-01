import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AccessToken } from 'src/app/others/AccessToken';
import { Group } from 'src/app/others/Group';
import { map } from 'rxjs/operators';
import { Account, Coordinator, SearchUser, Staff, Student, StudentBasic, Titles, User } from 'src/app/others/Accounts';
import { ICourse, ICourseEdition, ICourseType, IGroup } from 'src/app/others/Interfaces';

@Injectable({
  providedIn: 'root'
})
export class AdministratorApiService {

  readonly baseUrl:string = 'http://localhost:5000/api';

  constructor(private http:HttpClient) { }

  private GetAuthorizationHeaders(token:any) {
    return {
      "AccessToken": token.key,
      "AccessTokenSecret": token.secret
    };
  }

  public GetStudents(): Observable<Student[]> {
    const request = {
      url: this.baseUrl + `/students?$expand=User`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => response.value.map((element : any) => new Student(
          new User(
            element.UserId, 
            element.User.FirstName, 
            element.User.LastName
          ),
          element.StudentNumber,
          []
        )
      ))
    );
  }

  public GetStudentGroups(userId: number): Observable<Group[]> {
    const request = {
      url: this.baseUrl + `/studentGroups?$expand=Group&$filter=StudentId eq ${userId}`,
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
        group.ParentGroupId = element.Group.ParentGroupId;
        group.FullName = element.Group.Name;
        return group;
      }))
    );
  }

  public GetStaffs(): Observable<Staff[]> {
    const request = {
      url: this.baseUrl + `/staffs?$expand=User`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => response.value.map((element : any) => new Staff(
          new User(
            element.UserId, 
            element.User.FirstName, 
            element.User.LastName
          ),
          element.IsAdmin
        )
      ))
    );
  }

  public CreateCourseType(courseType: ICourseType): Observable<any> {
    const request = {
      url: this.baseUrl + `/courseTypes`,
      method: 'POST'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        body: courseType,
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    );
  }

  public UpdateCourseType(courseType: ICourseType): Observable<any> {
    const request = {
      url: this.baseUrl + `/courseTypes(${courseType.CourseTypeId})`,
      method: 'PATCH'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        body: courseType,
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    );
  }

  public RemoveCourseType(courseTypeId: number): Observable<any> {
    const request = {
      url: this.baseUrl + `/courseTypes(${courseTypeId})`,
      method: 'DELETE'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    );
  }

  public CreateCourse(course: ICourse): Observable<any> {
    const request = {
      url: this.baseUrl + `/courses`,
      method: 'POST'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        body: course,
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    );
  }

  public UpdateCourse(course: ICourse, connectionId: string): Observable<any> {
    const request = {
      url: this.baseUrl + `/courses(${course.CourseId})?connectionId=${connectionId}`,
      method: 'PATCH'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        body: course,
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    );
  }

  public RemoveCourse(courseId: number): Observable<any> {
    const request = {
      url: this.baseUrl + `/courses(${courseId})`,
      method: 'DELETE'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    );
  }

  public RemoveCourseRoom(courseId: number, roomId: number) {
    const request = {
      url: this.baseUrl + `/courseRooms(${courseId},${roomId})`,
      method: 'DELETE'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    );
  }

  public CreateCourseEdition(courseEdition: ICourseEdition): Observable<any> {
    const request = {
      url: this.baseUrl + `/courseEditions`,
      method: 'POST'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        body: courseEdition,
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    );
  }

  public UpdateCourseEdition(courseEdition: ICourseEdition): Observable<any> {
    const request = {
      url: this.baseUrl + `/courseEditions(${courseEdition.CourseId},${courseEdition.CourseEditionId})`,
      method: 'PATCH'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        body: courseEdition,
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    );
  }

  public RemoveCourseEdition(courseId: number, courseEditionId: number): Observable<any> {
    const request = {
      url: this.baseUrl + `/courseEditions(${courseId},${courseEditionId})`,
      method: 'DELETE'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    );
  }

  public AddCoordinatorCourseEdition(
    courseId: number,
    courseEditionId: number,
    coordinatorId: number,
    connectionId: string
  ): Observable<any> {
    const request = {
      url: this.baseUrl + `/coordinatorCourseEditions?connectionId=${connectionId}`,
      method: 'POST'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        body: {
          CourseId: courseId,
          CourseEditionId: courseEditionId,
          CoordinatorId: coordinatorId
        },
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    );
  }

  public RemoveCoordinatorCourseEdition(
    courseId: number,
    courseEditionId: number,
    coordinatorId: number,
  ): Observable<any> {
    const request = {
      url: this.baseUrl + `/coordinatorCourseEditions(${courseId},${courseEditionId},${coordinatorId})`,
      method: 'DELETE'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    );
  }

  public AddGroupCourseEdition(
    courseId: number,
    courseEditionId: number,
    groupId: number,
    connectionId: string
  ): Observable<any> {
    const request = {
      url: this.baseUrl + `/groupCourseEditions?connectionId=${connectionId}`,
      method: 'POST'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        body: {
          CourseId: courseId,
          CourseEditionId: courseEditionId,
          GroupId: groupId
        },
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    );
  }

  public RemoveGroupCourseEdition(
    courseId: number,
    courseEditionId: number,
    groupId: number,
  ): Observable<any> {
    const request = {
      url: this.baseUrl + `/groupCourseEditions(${courseId},${courseEditionId},${groupId})`,
      method: 'DELETE'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    );
  }

  public GetGroupsStudents(groupIds: number[]): Observable<StudentBasic[]> {
    const request = {
      url: this.baseUrl + `/studentGroups/Service.GetGroupsStudents(GroupsIds=[${groupIds.toString()}])`
      + `?$expand=User`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(map((response : any) => response.value.map((element : any) => new StudentBasic(
      element.User.UserId, `${element.User.FirstName} ${element.User.LastName}`
    ))));
  }

  public GiveOrRemoveRepresentativeRole(groupId: number, userId: number, role: boolean): Observable<any> {
    const request = {
      url: this.baseUrl + `/studentGroups/Service.GiveOrRemoveRepresentativeRole`,
      method: 'POST'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        body: {
          UserId: userId,
          GroupId: groupId,
          Role: role
        },
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    );
  }

  public GetGroupRepresentativeRoles(groupId: number): Observable<number[]> {
    const request = {
      url: this.baseUrl + `/studentGroups?$filter=GroupId eq ${groupId} and IsRepresentative eq true&$select=StudentId`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(map((response : any) => response.value.map((element : any) => element.StudentId)));
  }

  public CreateGroup(group: IGroup): Observable<any> {
    const request = {
      url: this.baseUrl + `/groups`,
      method: 'POST'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        body: group,
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    );
  }

  public UpdateGroup(group: IGroup, connectionId: string): Observable<any> {
    const request = {
      url: this.baseUrl + `/groups(${group.GroupId})?connectionId=${connectionId}`,
      method: 'PATCH'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        body: group,
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    );
  }

  public RemoveGroup(groupId: number): Observable<any> {
    const request = {
      url: this.baseUrl + `/groups(${groupId})`,
      method: 'DELETE'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    );
  }

  public AddStudentToGroup(groupId: number, userId: number) {
    const request = {
      url: this.baseUrl + `/studentGroups`,
      method: 'POST'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        body: {
          GroupId: groupId,
          StudentId: userId,
          IsRepresentative: false
        },
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    );
  }

  public RemoveStudentFromGroup(groupId: number, userId: number) {
    const request = {
      url: this.baseUrl + `/studentGroups(${groupId},${userId})`,
      method: 'DELETE'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    );
  }

  public SearchForUserFromUsos(query: string, perPage: number, start: number): Observable<SearchUser> {
    const request = {
      url: this.baseUrl + `/users/Service.SearchForUserFromUsos(Query='${query}',PerPage=${perPage},Start=${start})`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(map((response : any) => new SearchUser(
      response.Items.map((element : any) => {
        const user = new User(element.User.Id, 
          element.User.FirstName, 
          element.User.LastName
        );

        return new Account(
          user,
          element.User.StudentStatus != null && element.User.StudentStatus != 0 
            ? new Student(user, element.User.StudentNumber, []) : null,
          element.User.StaffStatus == 2 
            ? new Coordinator(
              user, new Titles(
                element.User.Titles.Before, 
                element.User.Titles.After
              ) 
            ) : null,
          element.User.StaffStatus == 1 || element.User.StaffStatus == 2
            ? new Staff(
              user, false
            ) : null
        );
      }),
      response.NextPage
    )));
  }

  public UploadSchedule(file: File, connectionId: string):Observable<any> {
    const request = {
      url: this.baseUrl + `/import/schedulePositions?connectionId=${connectionId}`,
      method: 'POST'
    };

    const formData = new FormData();

    formData.append('file', file);

    return this.http.request(
      request.method,
      request.url,
      {
        body: formData,
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    );
  }

  public DownloadSchedule():Observable<any> {
    const request = {
      url: this.baseUrl + `/export/schedulePositions`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        responseType: 'blob',
        observe: 'response',
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    );
  }
}
