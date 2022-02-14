import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AccessToken } from 'src/app/others/AccessToken';
import { Group } from 'src/app/others/Group';
import { map } from 'rxjs/operators';
import { UserInfo, SearchUser, Staff, Student, StudentBasic, User, Titles, UserBasic } from 'src/app/others/Accounts';
import { ICourse, ICourseEdition, ICourseType, IGroup, IRoom, IRoomType, ISettings, IUserInfo } from 'src/app/others/Interfaces';
import { environment } from 'src/environments/environment';

/**
 * Serwis odpowiadający za wysyłanie żądań dostępnych tylko dla administratora do API serwera.
 */
@Injectable({
  providedIn: 'root'
})
export class AdministratorApiService {

  /** Bazowy adres URL API serwera. */
  readonly baseUrl:string = environment.baseApiUrl;

  constructor(private http:HttpClient) { }

  /**
   * Pobranie nagłówka autoryzującego.
   * @param token Token dostępu
   * @returns Nagłówek autoryzujący
   */
  private GetAuthorizationHeaders(token:any) {
    return {
      "AccessToken": token.key,
      "AccessTokenSecret": token.secret
    };
  }

  /**
   * Metoda wysyłająca żądanie pobrania informacji o istniejących studentach do API systemu.
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie (tablicę studentów)
   */
  public GetStudents(): Observable<Student[]> {
    const request = {
      url: this.baseUrl + `/users?$filter=IsStudent eq true`,
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
            element.FirstName, 
            element.LastName
          ),
          element.AcademicNumber,
          [],
          new Titles(element.TitleBefore, element.TitleAfter)
        )
      ))
    );
  }

  /**
   * Metoda wysyłająca żądanie pobrania informacji o użytkownikach nieposiadających żadnych ról do API systemu.
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie (tablicę najbardziej podstawowych informacji o użytkownikach)
   */
  public GetOtherUsers(): Observable<UserBasic[]> {
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
      map((response : any) => response.value.map((element : any) => new UserBasic(
        element.UserId, 
        (element.TitleBefore != null ? `${element.TitleBefore} ` : '') + 
          `${element.FirstName} ${element.LastName}` + 
          (element.TitleAfter != null ? ` ${element.TitleAfter}` : '')
      )))
    );
  }

  /**
   * Metoda wysyłająca żądanie pobrania informacji o grupach, do których jest przypisany dany student do API systemu.
   * @param userId Identyfikator użytkownika
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie (tablicę grup)
   */
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

  /**
   * Metoda wysyłająca żądanie pobrania informacji o istniejących pracownikach do API systemu.
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie (tablicę pracowników)
   */
  public GetStaffs(): Observable<Staff[]> {
    const request = {
      url: this.baseUrl + `/users?$filter=IsStaff eq true`,
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
            element.FirstName, 
            element.LastName
          ),
          element.IsAdmin,
          new Titles(element.TitleBefore, element.TitleAfter)
        )
      ))
    );
  }

  /**
   * Metoda wysyłająca żądanie utworzenia nowego typu przedmiotu do API systemu.
   * @param courseType Obiekt zawierający dane tworzonego typu przedmiotu
   * @returns Strumień emitujący odpowiedź serwera na wysłane żądanie
   */
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

  /**
   * Metoda wysyłająca żądanie nadpisania typu przedmiotu do API systemu.
   * @param courseType Obiekt zawierający nadpisane dane typu przedmiotu
   * @returns Strumień emitujący odpowiedź serwera na wysłane żądanie
   */
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

  /**
   * Metoda wysyłająca żądanie usunięcia typu przedmiotu do API systemu.
   * @param courseTypeId Identyfikator typu przedmiotu
   * @returns Strumień emitujący odpowiedź serwera na wysłane żądanie
   */
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

  /**
   * Metoda wysyłająca żądanie utworzenia nowego przedmiotu do API systemu.
   * @param course Obiekt zawierający dane tworzonego przedmiotu
   * @returns Strumień emitujący odpowiedź serwera na wysłane żądanie
   */
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

  /**
   * Metoda wysyłająca żądanie nadpisania przedmiotu do API systemu.
   * @param course Obiekt zawierający nadpisane dane przedmiotu
   * @param connectionId Identyfikator połączenia z centrum SignalR
   * @returns Strumień emitujący odpowiedź serwera na wysłane żądanie
   */
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

  /**
   * Metoda wysyłająca żądanie usunięcia przedmiotu do API systemu.
   * @param courseId ID przedmiotu
   * @returns Strumień emitujący odpowiedź serwera na wysłane żądanie
   */
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

  /**
   * Metoda wysyłająca żądanie usunięcia przypisania pokoju do przedmiotu do API systemu.
   * @param courseId ID przedmiotu
   * @param roomId ID pokoju
   * @returns Strumień emitujący odpowiedź serwera na wysłane żądanie
   */
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

  /**
   * Metoda wysyłająca żądanie utworzenia nowej edycji zajęć do API systemu.
   * @param courseEdition Obiekt zawierający dane tworzonej edycji zajęć
   * @returns Strumień emitujący odpowiedź serwera na wysłane żądanie
   */
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

  /**
   * Metoda wysyłająca żądanie nadpisania edycji zajęć do API systemu.
   * @param courseEdition Obiekt zawierający nadpisane dane edycji zajęć
   * @returns Strumień emitujący odpowiedź serwera na wysłane żądanie
   */
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

  /**
   * Metoda wysyłająca żądanie usunięcia edycji zajęć do API systemu.
   * @param courseId ID przedmiotu
   * @param courseEditionId ID edycji zajęć
   * @returns Strumień emitujący odpowiedź serwera na wysłane żądanie
   */
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

  /**
   * Metoda wysyłająca żądanie przypisania prowadzącego do edycji zajęć do API systemu.
   * @param courseId ID przedmiotu
   * @param courseEditionId ID edycji zajęć
   * @param coordinatorId Identyfikator prowadzącego
   * @param connectionId Identyfikator połączenia z centrum SignalR
   * @returns Strumień emitujący odpowiedź serwera na wysłane żądanie
   */
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

  /**
   * Metoda wysyłająca żądanie usunięcia przypisania prowadzącego do edycji zajęć do API systemu.
   * @param courseId ID przedmiotu
   * @param courseEditionId ID edycji zajęć
   * @param coordinatorId Identyfikator prowadzącego
   * @returns Strumień emitujący odpowiedź serwera na wysłane żądanie
   */
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

  /**
   * Metoda wysyłająca żądanie przypisania grupy do edycji zajęć do API systemu.
   * @param courseId ID przedmiotu
   * @param courseEditionId ID edycji zajęć
   * @param groupId Identyfikator grupy
   * @param connectionId Identyfikator połączenia z centrum SignalR
   * @returns Strumień emitujący odpowiedź serwera na wysłane żądanie
   */
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

  /**
   * Metoda wysyłająca żądanie usunięcia przypisania grupy do edycji zajęć do API systemu.
   * @param courseId ID przedmiotu
   * @param courseEditionId ID edycji zajęć
   * @param groupId Identyfikator grupy
   * @returns Strumień emitujący odpowiedź serwera na wysłane żądanie
   */
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

  /**
   * Metoda wysyłająca żądanie pobrania listy studentów należących do podanych grup do API systemu.
   * @param groupIds Identyfikatory grup
   * @returns Strumień emitujący odpowiedź serwera na wysłane żądanie (tablicę podstawowych informacji o studentach)
   */
  public GetGroupsStudents(groupIds: number[]): Observable<StudentBasic[]> {
    const request = {
      url: this.baseUrl + `/studentGroups/Service.GetGroupsStudents(GroupsIds=[${groupIds.toString()}])`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(map((response : any) => response.value.map((element : any) => new StudentBasic(
      element.UserId, 
      (element.TitleBefore != null ? `${element.TitleBefore} ` : '') + 
        `${element.FirstName} ${element.LastName}` + 
        (element.TitleAfter != null ? ` ${element.TitleAfter}` : '')
    ))));
  }

  /**
   * Metoda wysyłająca żądanie nadania lub odebrania roli starosty grupy studentowi do API systemu.
   * @param groupId Identyfikator grupy
   * @param userId Identyfikator studenta
   * @param role Prawda - nadanie roli starosty, Fałsz - odebranie roli starosty
   * @returns Strumień emitujący odpowiedź serwera na wysłane żądanie
   */
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

  /**
   * Metoda wysyłająca żądanie pobrania identyfikatorów studentów będących starostami danej grupy do API systemu.
   * @param groupId Identyfikator grupy
   * @returns Strumień emitujący odpowiedź serwera na wysłane żądanie (tablicę identyfikatorów użytkowników)
   */
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

  /**
   * Metoda wysyłająca żądanie utworzenia nowej grupy do API systemu.
   * @param group Obiekt zawierający dane tworzonej grupy
   * @returns Strumień emitujący odpowiedź serwera na wysłane żądanie
   */
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

  /**
   * Metoda wysyłająca żądanie nadpisania grupy do API systemu.
   * @param group Obiekt zawierający nadpisane dane grupy
   * @param connectionId Identyfikator połączenia z centrum SignalR
   * @returns Strumień emitujący odpowiedź serwera na wysłane żądanie
   */
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

  /**
   * Metoda wysyłająca żądanie usunięcia grupy do API systemu.
   * @param groupId Identyfikator grupy
   * @returns Strumień emitujący odpowiedź serwera na wysłane żądanie
   */
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

  /**
   * Metoda wysyłająca żądanie przypisania studenta do grupy do API systemu.
   * @param groupId Identyfikator grupy
   * @param userId Identyfikator studenta
   * @returns Strumień emitujący odpowiedź serwera na wysłane żądanie
   */
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

  /**
   * Metoda wysyłająca żądanie usunięcia przypisania studenta do grupy do API systemu.
   * @param groupId Identyfikator grupy
   * @param userId Identyfikator studenta
   * @returns Strumień emitujący odpowiedź serwera na wysłane żądanie
   */
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

  /**
   * Metoda wysyłająca żądanie utworzenia nowego typu pokoju do API systemu.
   * @param roomType Obiekt zawierający dane tworzonego typu pokoju
   * @returns Strumień emitujący odpowiedź serwera na wysłane żądanie
   */
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

  /**
   * Metoda wysyłająca żądanie nadpisania typu pokoju do API systemu.
   * @param roomType Obiekt zawierający nadpisane dane typu pokoju
   * @returns Strumień emitujący odpowiedź serwera na wysłane żądanie
   */
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

  /**
   * Metoda wysyłająca żądanie usunięcia typu pokoju do API systemu.
   * @param roomTypeId Identyfikator typu pokoju
   * @returns Strumień emitujący odpowiedź serwera na wysłane żądanie
   */
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

  /**
   * Metoda wysyłająca żądanie utworzenia nowego pokoju do API systemu.
   * @param room Obiekt zawierający dane tworzonego pokoju
   * @returns Strumień emitujący odpowiedź serwera na wysłane żądanie
   */
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

  /**
   * Metoda wysyłająca żądanie nadpisania pokoju do API systemu.
   * @param room Obiekt zawierający nadpisane dane pokoju
   * @returns Strumień emitujący odpowiedź serwera na wysłane żądanie
   */
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

  /**
   * Metoda wysyłająca żądanie usunięcia pokoju do API systemu.
   * @param roomId Identyfikator pokoju
   * @returns Strumień emitujący odpowiedź serwera na wysłane żądanie
   */
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

  /**
   * Metoda wysyłająca żądanie utworzenia konta użytkownikowi na podstawie identyfikatora pochodzącego z systemu USOS do API systemu.
   * @param userId Identyfikator użytkownika w systemie USOS
   * @returns Strumień emitujący odpowiedź serwera na wysłane żądanie
   */
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

  /**
   * Metoda wysyłająca żądanie pobrania informacji o użytkowniku do API systemu.
   * @param userId Identyfikator użytkownika
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie
   */
  public GetUserAccount(userId: number):Observable<UserInfo> {
    const request = {
      url: this.baseUrl + `/users(${userId})?$expand=Groups`,
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeaders(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(map((response : any) => new UserInfo(
      response.UserId,
      response.FirstName,
      response.LastName,
      response.AcademicNumber,
      response.TitleBefore,
      response.TitleAfter,
      response.IsStudent,
      response.IsStaff,
      response.IsCoordinator,
      response.IsAdmin,
      response.Groups.filter((group : any) => group.IsRepresentative).map((group : any) => group.GroupId) ?? []
    )));
  }

  /**
   * Metoda wysyłająca żądanie wyszukania użytkowników spełniających kryteria w systemie USOS do API systemu.
   * @param query Kryteria wyszukiwania
   * @param perPage Liczba użytkowników na stronie
   * @param start Liczba użytkowników do pominięcia
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie (wynik wyszukiwania)
   */
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
        return new UserInfo(
          element.User.Id,
          element.User.FirstName,
          element.User.LastName,
          element.User.StudentNumber != null ? element.User.StudentNumber : '',
          element.User.Titles.Before, 
          element.User.Titles.After,
          !!(element.User.StudentStatus != null && element.User.StudentStatus != 0),
          !!(element.User.StaffStatus == 1 || element.User.StaffStatus == 2),
          !!(element.User.StaffStatus == 2),
          false,
          []
        );
      }),
      response.NextPage
    )));
  }

  /**
   * Metoda wysyłająca żądanie nadpisania danych użytkownika do API systemu.
   * @param user Obiekt zawierający nadpisane dane użytkownika
   * @returns Strumień emitujący przetworzoną odpowiedź serwera na wysłane żądanie
   */
  public UpdateUser(user: IUserInfo) {
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

  /**
   * Metoda wysyłająca żądanie usunięcia użytkownika do API systemu.
   * @param userId Identyfikator użytkownika
   * @returns Strumień emitujący odpowiedź serwera na wysłane żądanie
   */
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

  /**
   * Metoda wysyłająca żądanie nadpisania ustawień aplikacji do API systemu.
   * @param settings Obiekt zawierający nadpisane dane ustawień
   * @param connectionId Identyfikator połączenia z centrum SignalR
   * @returns Strumień emitujący odpowiedź serwera na wysłane żądanie
   */
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

  /**
   * Metoda wysyłająca żądanie zimportowania danych zasobu do API systemu.
   * @param files Pliki CSV z danymi
   * @param resource Nazwa zasobu, który ma zostać zimportowany
   * @param connectionId Identyfikator połączenia z centrum SignalR
   * @returns Strumień emitujący odpowiedź serwera na wysłane żądanie
   */
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

  /**
   * Metoda wysyłająca żądanie wyeksportowania danych zasobu do API systemu.
   * @param resource Nazwa zasobu, który ma zostać wyeksportowany
   * @returns Strumień emitujący odpowiedź serwera na wysłane żądanie
   */
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

  /**
   * Metoda wysyłająca żądanie wyczyszczenia danych zasobu do API systemu.
   * @param resource Nazwa zasobu, który ma zostać wyczyszczony
   * @param action Nazwa akcji OData (np. ClearSchedule)
   * @returns Strumień emitujący odpowiedź serwera na wysłane żądanie
   */
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
