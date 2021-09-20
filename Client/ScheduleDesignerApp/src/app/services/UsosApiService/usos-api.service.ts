import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';

import { Observable } from 'rxjs';
import { map } from 'rxjs/operators'; 
import { AccessToken } from 'src/app/others/AccessToken';
import * as OAuth from 'oauth-1.0a';
import { MatSnackBar } from '@angular/material/snack-bar';

@Injectable({
  providedIn: 'root'
})

export class UsosApiService {

  readonly baseUrl:string = 'https://api.usos.tu.kielce.pl';
  readonly oauth = new OAuth({
    consumer: { key: 'Ru3DbQrGDhTTaWChm3ME', secret: 'LSMfqwmpvpvnhdK2GPRXDqzbx8uaPQLqUwWxuXuM' },
    signature_method: 'PLAINTEXT'
  });
  
  constructor(private http:HttpClient) { }

  private GetAuthorizationHeader(request:any,token?:any):HttpHeaders {
    return new HttpHeaders({
      Authorization: this.oauth.toHeader(this.oauth.authorize(request, token)).Authorization
    });
  }

  public RequestToken():Observable<AccessToken> {
    const request = {
      url: this.baseUrl + '/services/oauth/request_token',
      method: 'POST'
    };
    
    const request_data = new FormData();
    request_data.append('oauth_callback', 'http://localhost:4200/authenticated');
    request_data.append('scopes', 'studies');
    
    return this.http.request(
      request.method,
      request.url,
      {
        body: request_data,
        headers: this.GetAuthorizationHeader(request),
        responseType: 'text'
      }
    ).pipe(map(data => AccessToken.ParseToken(data.toString(), ['oauth_token=', 'oauth_token_secret='], '&')));
  }

  public Authorize(oauth_token:string):Observable<Object> {
    const request = {
      url: this.baseUrl + '/services/oauth/authorize',
      method: 'GET'
    };

    const request_data = new HttpParams()
      .set('oauth_token', oauth_token)
      .set('interactivity', 'confirm_user');

    return this.http.request(
      request.method,
      request.url,
      {
        params: request_data
      }
    );
  }

  public AccessToken(oauth_verifier:string):Observable<AccessToken> {
    const request = {
      url: this.baseUrl + '/services/oauth/access_token',
      method: 'POST'
    };

    const request_data = new FormData();
    request_data.append('oauth_verifier', oauth_verifier);

    return this.http.request(
      request.method,
      request.url,
      {
        body: request_data,
        headers: this.GetAuthorizationHeader(request, AccessToken.RetrieveRequest()?.ToJson()),
        responseType: 'text'
      }
    ).pipe(map(data => AccessToken.ParseToken(data.toString(), ['oauth_token=', 'oauth_token_secret='], '&')));
  }

  public Deauthorize(snackBar:MatSnackBar):void {
    AccessToken.Remove();
    snackBar.open('Session expired. Please log in again.', 'Close');
  }

  public GetUser(user_id?:string):Observable<any> {
    const request = {
      url: this.baseUrl + '/services/users/user',
      method: 'POST'
    };

    const request_data = new FormData();
    if (user_id != undefined) {
      request_data.append('user_id', user_id);
    }
    request_data.append('fields', 'id|first_name|last_name');
    request_data.append('format', 'json');

    return this.http.request(
      request.method,
      request.url,
      {
        body: request_data,
        headers: this.GetAuthorizationHeader(request, AccessToken.Retrieve()?.ToJson())
      }
    );
  }
}
