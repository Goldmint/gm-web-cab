import { Injectable, Injector } from '@angular/core';
import { HttpEvent, HttpInterceptor, HttpHandler, HttpRequest } from '@angular/common/http';
import { Observable } from 'rxjs/Rx';
import 'rxjs/add/observable/throw'
import 'rxjs/add/operator/catch';

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
      return next.handle(req.clone({
        setHeaders: {
          'GM-LOCALE': localStorage.getItem('gmint_language')
        }
      })).catch((error, caught) => {
          if (error.status === 404) {
            this._messageBox.alert('Goldmint server does not respond. Please try again in few minutes.', 'Connection error');
          }

          return Observable.throw(error);
        }) as any;
    }
  }

}
