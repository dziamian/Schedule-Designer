import { Injectable } from '@angular/core';
import * as signalr from '@microsoft/signalr';

@Injectable({
  providedIn: 'root'
})
export class SignalrService {
  readonly connectionUrl = 'http://localhost:5000/scheduleHub';
  connection:signalr.HubConnection;

  constructor() { }

  public initConnection(): Promise<void> {
    return new Promise((resolve, reject) => {
      this.connection = new signalr.HubConnectionBuilder()
        .withUrl(this.connectionUrl)
        .build();

      this.connection
        .start()
        .then(() => {
          console.log(`Connection ID: ${this.connection.connectionId}`);
          resolve();
        })
        .catch((error) => {
          console.log(`Connection Error: ${error}`);
          reject();
        });
    });
  }
}
