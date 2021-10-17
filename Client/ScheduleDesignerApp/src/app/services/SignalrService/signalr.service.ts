import { Injectable } from '@angular/core';
import * as signalr from '@microsoft/signalr';
import { BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class SignalrService {
  readonly connectionUrl = 'http://localhost:5000/scheduleHub';
  connection:signalr.HubConnection;

  testMessage:BehaviorSubject<string>

  constructor() {
    this.testMessage = new BehaviorSubject<string>('');
  }

  public initConnection(): Promise<void> {
    return new Promise((resolve, reject) => {
      this.connection = new signalr.HubConnectionBuilder()
        .withUrl(this.connectionUrl)
        .build();

      this.setClientMethods();

      this.connection
        .start()
        .then(() => {
          resolve();
        })
        .catch(() => {
          reject();
        });
    });
  }

  private setClientMethods(): void {
    this.connection.on('Test', (message: string) => {
      this.testMessage.next(message);
    });
  }
}
