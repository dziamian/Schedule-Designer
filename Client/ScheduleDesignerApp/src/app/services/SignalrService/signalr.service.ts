import { Injectable, OnDestroy } from '@angular/core';
import * as signalr from '@microsoft/signalr';
import { BehaviorSubject, Observable } from 'rxjs';
import { AccessToken } from 'src/app/others/AccessToken';

@Injectable({
  providedIn: 'root'
})
export class SignalrService implements OnDestroy {
  readonly connectionUrl = 'http://localhost:5000/scheduleHub';
  
  connection:signalr.HubConnection;
  isConnected:BehaviorSubject<boolean>

  testMessage:BehaviorSubject<string>

  constructor() {
    this.isConnected = new BehaviorSubject<boolean>(false);
    this.testMessage = new BehaviorSubject<string>('');
  }

  private GetAuthorizationHeader(token:any) {
    return {
      "AccessToken": token.key,
      "AccessTokenSecret": token.secret
    };
  }

  public InitConnection(): Observable<void> {
    return new Observable((observer) => {
      var accessToken = AccessToken.Retrieve();

      console.log(accessToken);

      this.connection = new signalr.HubConnectionBuilder()
        .withUrl(this.connectionUrl, {
          transport: signalr.HttpTransportType.LongPolling,
          headers: this.GetAuthorizationHeader(accessToken),
        }).build();

      this.SetClientMethods();

      this.connection
        .start()
        .then(() => {
          this.isConnected.next(true);
          observer.next();
          observer.complete();
        })
        .catch(() => {
          observer.error();
        });
    });
  }

  public Disconnect() {
    if (this.connection.connectionId) {
      this.connection.stop();
    }
  }

  private SetClientMethods(): void {
    this.connection.onclose((error) => {
      this.isConnected.next(false);
    })
    this.connection.on('Test', (message: string) => {
      this.testMessage.next(message);
    });
  }

  ngOnDestroy() {
    this.Disconnect();
  }
}
