import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';

import { Observable } from 'rxjs';
import { map } from 'rxjs/operators'; 
import { AccessToken } from 'src/app/others/AccessToken';
import * as OAuth from 'oauth-1.0a';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})

export class UsosApiService {

  readonly baseUsosUrl:string = environment.baseUsosUrl;
  readonly baseApiUrl:string = environment.baseApiUrl;
  readonly oauth = new OAuth({
    consumer: { key: environment.consumerKey, secret: environment.consumerSecret },
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
      url: this.baseUsosUrl + '/services/oauth/request_token',
      method: 'POST'
    };
    
    const request_data = new FormData();
    request_data.append('oauth_callback', `${document.location.origin}/authenticated`);
    request_data.append('scopes', 'studies|staff_perspective|offline_access');
    
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

  public Authorize(oauth_token:string):Observable<any> {
    const request = {
      url: this.baseApiUrl + '/proxy/authorize',
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
      url: this.baseUsosUrl + '/services/oauth/access_token',
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

  public Logout(document:Document):void {
    document.location.href = this.baseUsosUrl + '/apps/logout';
  }

  public Deauthorize():void {
    AccessToken.Remove();
  }
}
