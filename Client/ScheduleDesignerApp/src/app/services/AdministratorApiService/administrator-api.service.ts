import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AccessToken } from 'src/app/others/AccessToken';

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
