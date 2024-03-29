import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AccessToken } from 'src/app/others/AccessToken';
import { Group } from 'src/app/others/Group';
import { map } from 'rxjs/operators';
import { Account, Coordinator, SearchUser, Staff, Student, StudentBasic, Titles, User } from 'src/app/others/Accounts';
import { ICoordinator, ICourse, ICourseEdition, ICourseType, IGroup, IRoom, IRoomType, ISettings, IStaff, IStudent, IUser } from 'src/app/others/Interfaces';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AdministratorApiService {

  readonly baseUrl:string = environment.baseApiUrl;

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

  public GetOtherUsers(): Observable<User[]> {
    const request = {
      url: this.baseUrl + `/users/Service.GetOtherUsers()`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => response.value.map((element : any) => new User(
        element.UserId, 
        element.FirstName, 
        element.LastName
      )))
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

  public CreateRoomType(roomType: IRoomType): Observable<any> {
    const request = {
      url: this.baseUrl + `/roomTypes`,
      method: 'POST'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        body: roomType,
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    );
  }

  public UpdateRoomType(roomType: IRoomType): Observable<any> {
    const request = {
      url: this.baseUrl + `/roomTypes(${roomType.RoomTypeId})`,
      method: 'PATCH'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        body: roomType,
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    );
  }

  public RemoveRoomType(roomTypeId: number): Observable<any> {
    const request = {
      url: this.baseUrl + `/roomTypes(${roomTypeId})`,
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

  public CreateRoom(room: IRoom): Observable<any> {
    const request = {
      url: this.baseUrl + `/rooms`,
      method: 'POST'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        body: room,
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    );
  }

  public UpdateRoom(room: IRoom): Observable<any> {
    const request = {
      url: this.baseUrl + `/rooms(${room.RoomId})`,
      method: 'PATCH'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        body: room,
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    );
  }

  public RemoveRoom(roomId: number): Observable<any> {
    const request = {
      url: this.baseUrl + `/rooms(${roomId})`,
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

  public CreateAccountFromUsos(userId: number): Observable<any> {
    const request = {
      url: this.baseUrl + `/users/Service.CreateAccountFromUsos`,
      method: 'POST'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        body: {
          UserId: userId
        },
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    );
  }

  public GetUserAccount(userId: number):Observable<Account> {
    const request = {
      url: this.baseUrl + `/users(${userId})?$expand=Student($expand=Groups),Coordinator,Staff`,
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

  public UpdateUser(user: IUser) {
    const request = {
      url: this.baseUrl + `/users(${user.UserId})`,
      method: 'PATCH'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        body: user,
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    );
  }

  public RemoveUser(userId: number) {
    const request = {
      url: this.baseUrl + `/users(${userId})`,
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

  public CreateStaff(staff: IStaff) {
    const request = {
      url: this.baseUrl + `/staffs`,
      method: 'POST'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        body: staff,
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    );
  }

  public UpdateStaff(staff: IStaff) {
    const request = {
      url: this.baseUrl + `/staffs(${staff.UserId})`,
      method: 'PATCH'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        body: staff,
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    );
  }

  public RemoveStaff(userId: number) {
    const request = {
      url: this.baseUrl + `/staffs(${userId})`,
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

  public CreateCoordinator(coordinator: ICoordinator) {
    const request = {
      url: this.baseUrl + `/coordinators`,
      method: 'POST'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        body: coordinator,
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    );
  }

  public UpdateCoordinator(coordinator: ICoordinator) {
    const request = {
      url: this.baseUrl + `/coordinators(${coordinator.UserId})`,
      method: 'PATCH'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        body: coordinator,
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    );
  }

  public RemoveCoordinator(userId: number) {
    const request = {
      url: this.baseUrl + `/coordinators(${userId})`,
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

  public CreateStudent(student: IStudent) {
    const request = {
      url: this.baseUrl + `/students`,
      method: 'POST'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        body: student,
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    );
  }

  public UpdateStudent(student: IStudent) {
    const request = {
      url: this.baseUrl + `/students(${student.UserId})`,
      method: 'PATCH'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        body: student,
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    );
  }

  public RemoveStudent(userId: number) {
    const request = {
      url: this.baseUrl + `/students(${userId})`,
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

  public UpdateSettings(settings: ISettings, connectionId: string):Observable<any> {
    const request = {
      url: this.baseUrl + `/settings?connectionId=${connectionId}`,
      method: 'PATCH'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        body: settings,
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    );
  }

  public Import(files: {name: string, file: File}[], resource: string, connectionId?: string | null):Observable<any> {
    const request = {
      url: this.baseUrl + `/import/${resource}`,
      method: 'POST'
    };

    if (connectionId != null || connectionId != undefined) {
      request.url += `?connectionId=${connectionId}`;
    }

    const formData = new FormData();

    for (var file in files) {
      formData.append(files[file].name, files[file].file);
    }

    return this.http.request(
      request.method,
      request.url,
      {
        body: formData,
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    );
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

  public Export(resource: string):Observable<any> {
    const request = {
      url: this.baseUrl + `/export/${resource}`,
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

  public Clear(resource: string, action: string):Observable<any> {
    const request = {
      url: this.baseUrl + `/${resource}/${action}`,
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
}
