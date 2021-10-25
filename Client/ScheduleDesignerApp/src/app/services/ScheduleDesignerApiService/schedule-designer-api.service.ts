import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import * as OAuth from 'oauth-1.0a';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { AccessToken } from 'src/app/others/AccessToken';
import { Settings } from 'src/app/others/Settings';

@Injectable({
  providedIn: 'root'
})
export class ScheduleDesignerApiService {

  readonly baseUrl:string = 'http://localhost:5000/api';

  constructor(private http:HttpClient) { }

  private GetAuthorizationHeader(token:any) {
    return {
      "AccessToken": token.key,
      "AccessTokenSecret": token.secret
    };
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
        headers: this.GetAuthorizationHeader(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(map((response : any) => new Settings(
      response.CourseDurationMinutes,
      response.StartTime,
      response.EndTime,
      response.TermDurationWeeks
    )));
  }

  public GetPeriods():Observable<string[]> {
    const request = {
      url: this.baseUrl + '/settings/ScheduleDesignerService.GetPeriods',
      method: 'GET'
    };
    
    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeader(AccessToken.Retrieve()?.ToJson())
      }
    ).pipe(
      map((response : any) => response.value)
    );
  }

  public GetFreePeriods():Observable<number[]> {
    const request = {
      url: this.baseUrl + '/schedulePositions/ScheduleDesignerService.GetFreePeriods',
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url
    ).pipe(
      map((response : any) => response.value)
    );
  }

  public Test1():Observable<string> {
    const request = {
      url: this.baseUrl + '/test/test1',
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        responseType: 'text'
      }
    );
  }

  public Test2():Observable<string> {
    const request = {
      url: this.baseUrl + '/test/test2',
      method: 'GET'
    };

    return this.http.request(
      request.method,
      request.url,
      {
        headers: this.GetAuthorizationHeader(AccessToken.Retrieve()?.ToJson()),
        responseType: 'text'
      }
    );
  }
}
