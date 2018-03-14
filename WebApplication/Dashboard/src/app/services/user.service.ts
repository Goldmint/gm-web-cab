import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { JwtHelperService } from '@auth0/angular-jwt';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';
import { Observable } from 'rxjs/Observable';
import { zip } from 'rxjs/observable/zip';
import { tap, shareReplay } from 'rxjs/operators';
import { interval } from "rxjs/observable/interval";
import { User } from '../interfaces';
import { APIResponse, AuthResponse } from '../interfaces/api-response';
import { MessageBoxService } from './message-box.service';
import { APIService } from './api.service';
import { AppDefaultLanguage } from '../app.languages';
import { ReplaySubject } from "rxjs/ReplaySubject";
import {Subject} from "rxjs/Subject";

@Injectable()
export class UserService {

  private _user = new ReplaySubject<User>(1);
  private _locale = new BehaviorSubject<string>(AppDefaultLanguage || 'en');

  public currentUser: Observable<User> = this._user.asObservable();
  public currentLocale: Observable<string> = this._locale.asObservable();
  public canShowNav$ = new Subject();

  constructor(
    private _router: Router,
    private _apiService: APIService,
    private _jwtHelper: JwtHelperService,
    private _messageBox: MessageBoxService,
    private http: HttpClient
  ) {
    const token = localStorage.getItem('gmint_token');
    if (token) {
      this.processToken(token);
    }
  }

  public processToken(token: string) {
    this._processLoginResponse({ data: { token: token } });
  }

  private _processLoginResponse(response: APIResponse<AuthResponse>) {
    console.group('_processLoginResponse');
    console.log('response', response);

    if (!response.errorCode && response.data.token) {
      localStorage.setItem('gmint_token', response.data.token);

      const jwt = this._jwtHelper.decodeToken(response.data.token);

      console.log('jwt', jwt);

      if (jwt.gm_area === 'authorized') {
        if (jwt.gm_role === 'user' && jwt.gm_id) this._user.next({ name: jwt.gm_id });

        zip(
          this._apiService.getProfile(),
          shareReplay(),
          (profile: APIResponse<User>) => {
            let user = profile.data;
            return user;
          }
        ).subscribe(user => {
          this._user.next(user);
        });
      }
      else if (jwt.gm_area === 'tfa') {
        // 2FA token received. Continue flow further to the LoginPageController...
        localStorage.setItem('gmint_2fa', '1');

        this._router.navigate(['/signin']);
      }
    }
    else {
      this._messageBox.alert(response.errorDesc);
    }

    console.groupEnd();
  }

  login(username: string, password: string, recaptcha: string) {
    return this._apiService.userLogin(username, password, recaptcha)
      .pipe(
      tap(
        (res: APIResponse<AuthResponse>) => {
          console.info('User login result', res);
          this.canShowNav(true);
          this._processLoginResponse(res);
        },
        err => {
          console.warn('User login error', err);
        }
      )
      );
  }

  canShowNav(flag: boolean) {
    this.canShowNav$.next(flag);
  }

  proceedTFA(code: string) {
    return this._apiService.exchangeTFAToken(code)
      .pipe(
      tap(
        (res: APIResponse<AuthResponse>) => {
          console.info('TFA code processing result', res);

          localStorage.removeItem('gmint_2fa');

          this._processLoginResponse(res);
        },
        err => {
          console.warn('TFA code processing error', err);
        }
      )
      );
  }

  logout(e?: any) {
    if (e) e.preventDefault();
    localStorage.removeItem('gmint_token');
    localStorage.removeItem('gmint_uc_2fa');

    this._user.next({} as User);
    this._apiService.userLogout();
    this.canShowNav(false);
    this._router.navigate(['/signin']);
  }

  public setLocale(locale: string) {
    this._locale.next(locale);
  }

  public isAuthenticated(): boolean {
    const token: string = this._jwtHelper.tokenGetter();
    if (!token) {
      return false;
    }

    const tokenExpired: boolean = this._jwtHelper.isTokenExpired(token);
    if (tokenExpired) {
      localStorage.removeItem('gmint_token');
      this._router.navigate(['/signin'], { queryParams: { returnUrl: this._router.url } });
      return false;
    }

    const jwt: any = this._jwtHelper.decodeToken(token);
    if (jwt.gm_area !== 'authorized') {
      return false;
    }

    return true;
  }

	public launchTokenRefresher() {
		this.refreshJwtToken(true);
		interval(10000).subscribe(time => { this.refreshJwtToken(false); });
	}
  
	private refreshJwtToken(forceNewToken: boolean) {
		const token = this._jwtHelper.tokenGetter();
		if (!token) {
			console.log("[JWT Refresher]", "/ EMPTY TOKEN");
			return;
		}

		const jwt:any = this._jwtHelper.decodeToken(token);
		if (!jwt || !jwt.hasOwnProperty('exp') || !jwt.hasOwnProperty('iat') || !jwt.hasOwnProperty('gm_area') || jwt.gm_area !== 'authorized') {
			console.log("[JWT Refresher]", "/ INVALID TOKEN");
			return;
		}

		var fullTtlSeconds = jwt.exp - jwt.iat;
		if (!this._jwtHelper.isTokenExpired(token, 3)) {
			var remainSeconds = (this._jwtHelper.getTokenExpirationDate(token).getTime() - new Date().getTime()) / 1000;
			var remainPerc = Math.round(remainSeconds / (fullTtlSeconds / 100));
			console.log("[JWT Refresher]", "/ VALID", "/ TTL", remainSeconds + " s.", remainPerc + "%", "/ FTTL", fullTtlSeconds);

			if (remainPerc <= 20 || forceNewToken) {
				console.log("[JWT Refresher]", "/ REFRESHING ATTEMPT");

				this._apiService.userRefreshToken()
				.subscribe(x => {
					console.log("[JWT Refresher]", "/ GOT FRESH TOKEN");
					localStorage.setItem('gmint_token', x);
				});
			}
		} else {
			console.log("[JWT Refresher]", "/ WILL EXPIRE within 3 s.", "/ FTTL", fullTtlSeconds);
		}
	}
}
