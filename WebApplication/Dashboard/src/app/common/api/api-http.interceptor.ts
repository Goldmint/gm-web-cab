import { Injectable, Injector } from '@angular/core';
import { HttpEvent, HttpInterceptor, HttpHandler, HttpRequest } from '@angular/common/http';
import { Observable } from 'rxjs/Rx';
import 'rxjs/add/observable/throw'
import 'rxjs/add/operator/catch';

import { MessageBoxService } from '../../services/message-box.service';
import {Router} from "@angular/router";
import {environment} from '../../../environments/environment';

@Injectable()
export class APIHttpInterceptor implements HttpInterceptor {

  private _isOnline: boolean;

  constructor(
    private _messageBox: MessageBoxService,
    private _router: Router) {
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
      let handle;
      if (req.url.indexOf(environment.apiUrl) >= 0) {
        handle = next.handle(req.clone({
          setHeaders: {
            'GM-LOCALE': localStorage.getItem('gmint_language') || ''
          }
        }));
      } else {
        handle =  next.handle(req);
      }

      return handle.catch((error, caught) => {
          if (error.status === 404) {
            this._messageBox.alert('Goldmint server does not respond. Please try again in few minutes.', 'Connection error');
          } else if (error.error.errorCode === 50) {
            try { // Safari in incognito mode doesn't have storage objects
              localStorage.removeItem('gmint_token');
              localStorage.removeItem('gmint_2fa');
              sessionStorage.removeItem('gmint_uc_2fa');
            } catch (e) {
            }
            this._router.navigate(['/signin']);
          }

          return Observable.throw(error);
        }) as any;
    }
  }

}
