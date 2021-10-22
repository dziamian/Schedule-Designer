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
  readonly oauth = new OAuth({
    consumer: { key: 'Ru3DbQrGDhTTaWChm3ME', secret: 'LSMfqwmpvpvnhdK2GPRXDqzbx8uaPQLqUwWxuXuM' },
    signature_method: 'PLAINTEXT'
  })

  constructor(private http:HttpClient) { }

  private GetAuthorizationHeader(request:any, token?:any):HttpHeaders {
    return new HttpHeaders({
      Authorization: this.oauth.toHeader(this.oauth.authorize(request, token)).Authorization
    });
  }

  public GetSettings():Observable<Settings> {
    const request = {
      url: this.baseUrl + '/settings',
      method: 'GET'
    };
    
    return this.http.request(
      request.method,
      request.url
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
      request.url
    ).pipe(map((response : any) => response.value)
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
        headers: this.GetAuthorizationHeader(request, AccessToken.Retrieve()?.ToJson()),
        responseType: 'text'
      }
    );
  }
}
