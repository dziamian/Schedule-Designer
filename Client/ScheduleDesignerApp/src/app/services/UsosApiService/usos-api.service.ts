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

/**
 * Serwis przeznaczony do komunikowania się z zewnętrznym systemem USOS.
 */
export class UsosApiService {

  /** Bazowy adres URL instalacji USOS API. */
  readonly baseUsosUrl:string = environment.baseUsosUrl;
  /** Bazowy adres URL API serwera. */
  readonly baseApiUrl:string = environment.baseApiUrl;
  readonly oauth = new OAuth({
    consumer: { key: environment.consumerKey, secret: environment.consumerSecret },
    signature_method: 'PLAINTEXT'
  });
  
  constructor(private http:HttpClient) { }

  /**
   * Pobranie nagłówka autoryzującego.
   * @param token Token dostępu
   * @returns Nagłówek autoryzujący
   */
  private GetAuthorizationHeader(request:any,token?:any):HttpHeaders {
    return new HttpHeaders({
      Authorization: this.oauth.toHeader(this.oauth.authorize(request, token)).Authorization
    });
  }

  /**
   * Metoda wysyłająca żądanie zwrócenia tokenu przeznaczonego do autoryzacji do USOS API.
   * @returns Strumień emitujący odpowiedź serwera na wysłane żądanie (niezautoryzowany token dostępu)
   */
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

  /**
   * Metoda wysyłająca żądanie przekierowania na stronę logowania do USOS API (używając systemowego proxy).
   * @param oauth_token Niezautoryzowany token dostępu
   * @returns Strumień emitujący odpowiedź serwera na wysłane żądanie
   */
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

  /**
   * Metoda wysyłająca żądanie zautoryzowania tokenu dostępu do USOS API.
   * @param oauth_verifier Kod autoryzacyjny
   * @returns Strumień emitujący odpowiedź serwera na wysłane żądanie (token dostępu)
   */
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
