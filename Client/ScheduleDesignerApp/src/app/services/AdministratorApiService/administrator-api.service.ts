import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AccessToken } from 'src/app/others/AccessToken';
import { Group } from 'src/app/others/Group';
import { map } from 'rxjs/operators';
import { Staff, Student, User } from 'src/app/others/Accounts';
import { ICourse, ICourseType } from 'src/app/others/Interfaces';

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
