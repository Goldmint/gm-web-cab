import { Injectable } from '@angular/core';
import { HttpEvent, HttpInterceptor, HttpHandler, HttpRequest } from '@angular/common/http';
import { Observable } from 'rxjs/Rx';
import 'rxjs/add/observable/throw'
import 'rxjs/add/operator/catch';

import { MessageBoxService } from '../../services/message-box.service';
import {TranslateService} from "@ngx-translate/core";
import {Router} from "@angular/router";
import {APIService} from "../../services";

@Injectable()
export class APIHttpInterceptor implements HttpInterceptor {

  private _isOnline: boolean;

  constructor(
    private _messageBox: MessageBoxService,
    private _translate: TranslateService,
    private _router: Router,
    private _apiService: APIService
  ) {
    Observable.merge(
      Observable.of(navigator.onLine),
      Observable.fromEvent(window, 'online').map(()  => true),
      Observable.fromEvent(window, 'offline').map(() => false)
    ).subscribe(connected => this._isOnline = connected);
  }

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    if (!this._isOnline) {
      this._translate.get('MessageBox.ConnectionError').subscribe(phrase => {
        this._messageBox.alert(phrase.Message, phrase.Title);
      });
      return Observable.throw('ohO_offline');
    }
    else {
      let handle = next.handle(req);

      return handle.catch((error, caught) => {
          let translateKey  = null,
              ignoredErrors = [ // ignore auto translation for these codes
        50,   // Unauthorized
				100, 	// InvalidParameter
        103,  // TradingNotAllowed
        104,  // TradingExchangeLimit
        106,  // MigrationDuplicateRequest
        501,  // PromoCodeNotEnter
        502,  // PromoCodeNotFound
        503,  // PromoCodeExpired
        504,  // PromoCodeIsUsed
        505,  // PromoCodeLimitExceeded
				1000,	// AccountNotFound
        1001, // AccountLocked
				1011,	// AccountDpaNotSigned
        1004 /// AccountEmailTaken
			];

          if (error.error.errorCode === 103) {
            this._apiService.transferTradingError$.next(true);
          } else if (error.error.errorCode === 104) {
            this._apiService.transferTradingLimit$.next(error.error.data);
          } else if (error.error.errorCode === 50) {
            this._router.navigate(['/buy-sell-gold']);
          } else {
            if (error.error.hasOwnProperty('errorCode')) {
              let errorCode = parseInt(error.error.errorCode, 10);
              ignoredErrors.indexOf(errorCode) < 0 && (translateKey = errorCode);
            } else if (error.error.hasOwnProperty('msg')) {
              this._translate.get('APIErrors.defaultMsg', {msg: error.error.msg}).subscribe(phrase => {
                this._messageBox.alert(phrase);
              });
            }
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
