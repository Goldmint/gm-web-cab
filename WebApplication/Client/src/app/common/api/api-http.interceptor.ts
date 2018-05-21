import { Injectable, Injector } from '@angular/core';
import { HttpEvent, HttpInterceptor, HttpHandler, HttpRequest } from '@angular/common/http';
import { Observable } from 'rxjs/Rx';
import 'rxjs/add/observable/throw'
import 'rxjs/add/operator/catch';
import { environment } from '../../../environments/environment';

import { MessageBoxService } from '../../services/message-box.service';
import {TranslateService} from "@ngx-translate/core";
import {Router} from "@angular/router";

@Injectable()
export class APIHttpInterceptor implements HttpInterceptor {

  private _isOnline: boolean;

  constructor(
    private _messageBox: MessageBoxService,
    private _translate: TranslateService,
    private _router: Router
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
      this._translate.get('MessageBox.ConnectionError').subscribe(phrase => {
        this._messageBox.alert(phrase.Message, phrase.Title);
      });
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
          let translateKey  = null,
              ignoredErrors = [ // ignore auto translation for these codes
        50,   // Unauthorized
				100, 	// InvalidParameter
				1000,	// AccountNotFound
				1011,	// AccountDpaNotSigned
        1004 /// AccountEmailTaken
			];

          if (error.status === 404 && req.url.indexOf(environment.apiUrl) >= 0) {
            translateKey = 'notFound';
          } else if (error.error.errorCode === 50) {
            try { // Safari in incognito mode doesn't have storage objects
              localStorage.removeItem('gmint_token');
              localStorage.removeItem('gmint_2fa');
              sessionStorage.removeItem('gmint_uc_2fa');
            } catch(e) {}
            this._router.navigate(['/signin']);
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
