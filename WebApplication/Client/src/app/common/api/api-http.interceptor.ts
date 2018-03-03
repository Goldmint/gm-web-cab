import { Injectable, Injector } from '@angular/core';
import { HttpEvent, HttpInterceptor, HttpHandler, HttpRequest } from '@angular/common/http';
import { Observable } from 'rxjs/Rx';
import 'rxjs/add/observable/throw'
import 'rxjs/add/operator/catch';
import { environment } from '../../../environments/environment';

import { MessageBoxService } from '../../services/message-box.service';

@Injectable()
export class APIHttpInterceptor implements HttpInterceptor {

  private _isOnline: boolean;

  constructor(private _messageBox: MessageBoxService) {
    Observable.merge(
      Observable.of(navigator.onLine),
      Observable.fromEvent(window, 'online').map(()  => true),
      Observable.fromEvent(window, 'offline').map(() => false)
    ).subscribe(connected => this._isOnline = connected);
  }

  // @todo: move alerts to the api errors catcher
  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    if (!this._isOnline) {
      console.info('Browser is offline!');

      this._messageBox.alert('Seems like your internet connection is lost.<br>Please check it and try again.', 'Connection error');

      return Observable.throw('ohO_offline');
    }
    else {
      return next.handle(req)
        .catch((error, caught) => {
          if (error.status === 404 && req.url.indexOf(environment.apiUrl) >= 0) {
            this._messageBox.alert('Goldmint server does not respond. Please try again in few minutes.', 'Connection error');
          }
          if (error.error.errorCode === 1010) {
            this._messageBox.alert('You have exceeded request frequency (One request for 30 minutes). Please try later');
          }
          else if (error.error.errorCode === 1012) {
            this._messageBox.alert('Your previously blockchain operation is still pending');
          } else {
            this._messageBox.alert(`Sorry, somethings went wrong (error code ${error.error.errorCode})`);
          }
          return Observable.throw(error);
        }) as any;
    }
  }

}
