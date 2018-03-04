import { Injectable, Injector } from '@angular/core';
import { HttpEvent, HttpInterceptor, HttpHandler, HttpRequest } from '@angular/common/http';
import { Observable } from 'rxjs/Rx';
import 'rxjs/add/observable/throw'
import 'rxjs/add/operator/catch';
import { environment } from '../../../environments/environment';

import { MessageBoxService } from '../../services/message-box.service';
import {TranslateService} from "@ngx-translate/core";

@Injectable()
export class APIHttpInterceptor implements HttpInterceptor {

  private _isOnline: boolean;

  constructor(
    private _messageBox: MessageBoxService,
    private _translate: TranslateService,
  ) {
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
          let translateKey  = null,
              ignoredErrors = [100, 1000];

          if (error.status === 404 && req.url.indexOf(environment.apiUrl) >= 0) {
            translateKey = 'notFound';
          } else {
            let errorCode = parseInt(error.error.errorCode, 10);
            ignoredErrors.indexOf(errorCode) < 0 && (translateKey = errorCode);
          }

          translateKey && this._translate.get('APIErrors.' + translateKey, error.error).subscribe(phrase => {
            this._messageBox.alert(phrase === 'APIErrors.' + translateKey
              ? this._translate.instant('APIErrors.default', error.error) : phrase
            );
          });

          return Observable.throw(error);
        }) as any;
    }
  }

}
