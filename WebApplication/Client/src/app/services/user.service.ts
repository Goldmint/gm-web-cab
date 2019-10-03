import { Injectable/*, ChangeDetectorRef*/ } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { JwtHelperService } from '@auth0/angular-jwt';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';
import { Observable } from 'rxjs/Observable';
import { zip } from 'rxjs/observable/zip';
import { tap, shareReplay } from 'rxjs/operators';
import { interval } from "rxjs/observable/interval";
import { User, OAuthRedirectResponse } from '../interfaces';
import { APIResponse, AuthResponse, RegistrationResponse } from '../interfaces/api-response';
import { MessageBoxService } from './message-box.service';
import { APIService } from './api.service';
import { AppDefaultLanguage } from '../app.languages';
import { ReplaySubject } from "rxjs/ReplaySubject";
import {Subject} from "rxjs/Subject";
import {TranslateService} from "@ngx-translate/core";
import {environment} from "../../environments/environment";

@Injectable()
export class UserService {

  private _user = new ReplaySubject<User>(1);
  private _locale = new BehaviorSubject<string>(AppDefaultLanguage || 'en');

  public currentUser: Observable<User> = this._user.asObservable();
  public currentLocale: Observable<string> = this._locale.asObservable();
  public getLiteWalletLink;
  public windowSize$ = new Subject();

  private token;

  constructor(
    private _router: Router,
    private _apiService: APIService,
    private _jwtHelper: JwtHelperService,
    private _messageBox: MessageBoxService,
    private _translate: TranslateService,
    private http: HttpClient
  ) {
    this.token = localStorage.getItem('gmint_token');
    if (this.token) {
      this.processToken(this.token);
    }

    let isFirefox = typeof window['InstallTrigger'] !== 'undefined';
    this.getLiteWalletLink = isFirefox ? environment.getLiteWalletLink.firefox : environment.getLiteWalletLink.chrome;
  }

  public getIPInfo() {
    return this.http.get('https://ipinfo.io');
  }

  public processToken(token: string) {
    this._processLoginResponse({ data: { token: token } });
  }

  private _processLoginResponse(response: APIResponse<AuthResponse>) {

    if (!response.errorCode && response.data.token) {
      localStorage.setItem('gmint_token', response.data.token);

      const jwt = this._jwtHelper.decodeToken(response.data.token);

      if (jwt.gm_area === 'authorized') {
        if (jwt.gm_role === 'user' && jwt.gm_id) this._user.next({ name: jwt.gm_id });

        zip(
          this._apiService.getProfile(),
          shareReplay(),
          (profile: APIResponse<User>) => {
            let user = profile.data;
            if (user && user.hasOwnProperty('verifiedL0') && !user.verifiedL0) this.redirectToTosVerifPage();
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
  }

  redirectToTosVerifPage() {
    if (this._router.url.indexOf('legal-security') >= 0 ) {
      return;
    }
    this._router.navigate(['/tos-verification']);
  }

  login(username: string, password: string, recaptcha: string) {
    return this._apiService.userLogin(username, password, recaptcha)
      .pipe(
      tap(
        (res: APIResponse<AuthResponse>) => {
          this._processLoginResponse(res);
        },
        err => {
          console.warn('User login error', err);
        }
      )
      );
  }

  loginWithSocial(provider: string) {
    switch (provider) {
      case 'google':
        return this._apiService.getGoogleOAuthUrl();
    }
    throw new Error("Unknown provider");
  }

  proceedTFA(code: string) {
    return this._apiService.exchangeTFAToken(code)
      .pipe(
      tap(
        (res: APIResponse<AuthResponse>) => {
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

    this._user.next({} as User);
    this._apiService.userLogout().subscribe(() => {
      localStorage.removeItem('gmint_token');
      localStorage.removeItem('gmint_uc_2fa');

      this._router.navigate(['/signin']);
    });
  }

  register(username: string, password: string, recaptcha: string, agreed: boolean) {
    return this._apiService.userRegister(username, password, recaptcha, agreed)
      .pipe(
      tap(
        (res: APIResponse<RegistrationResponse>) => {
          if (res.errorCode) {
            this._messageBox.alert(res.errorDesc);
          }
        },
        err => {
          console.warn('User register error', err);
        }
      )
      );
  }

  showLoginToMMBox(heading: string) {
    this._translate.get('MessageBox.LoginToMM').subscribe(phrase => {
      this._messageBox.alert(`
        <div class="text-center">${phrase.Text}</div>
        <div class="metamask-icon"></div>
        <div class="text-center mt-2 mb-2">MetaMask</div>
      `, phrase[heading]);
    });
  }

  showGetMetamaskModal() {
    this._translate.get('MessageBox.MetaMask').subscribe(phrase => {
      this._messageBox.alert(phrase.Text, phrase.Heading);
    });
  }

  showLoginToLiteWalletModal() {
    this._translate.get('MessageBox.LoginToLiteWallet').subscribe(phrase => {
      this._messageBox.alert(`
        <div class="text-center">${phrase.Text}</div>
        <div class="gold-circle-icon"></div>
        <div class="text-center mt-2 mb-2">Lite Wallet</div>
      `, phrase.Heading);
    });
  }

  showGetLiteWalletModal() {
    this._translate.get('MessageBox.LiteWallet').subscribe(phrase => {
      this._messageBox.alert(`
            <div>${phrase.Text} <a href="${this.getLiteWalletLink}" target="_blank">Lite Wallet</a></div>
      `, phrase.Heading);
    });
  }

  invalidNetworkModal(network) {
    this._translate.get('MessageBox.InvalidNetwork', {network}).subscribe(phrase => {
      setTimeout(() => {
        this._messageBox.alert(phrase);
      }, 0);
    });
  }

  showInvalidNetworkModal(translateKey, network) {
    this._translate.get('MessageBox.' + translateKey, {network}).subscribe(phrase => {
      setTimeout(() => {
        this._messageBox.alert(phrase);
      }, 0);
    });
  }

  /*
  public updateUser(newUser: User) {
    this._user.next(Object.assign(this._user.getValue(), newUser));
  }

  public setBalance(balance: Balance) {
    let user = this._user.getValue();
        user.balance = balance;

    this._user.next(user);
  }*/

  public setLocale(locale: string) {
    this._locale.next(locale);
  }

  public isAuthenticated(): boolean {
    if (!this.token) {
      return false;
    }

    const tokenExpired: boolean = this._jwtHelper.isTokenExpired(this.token);
    if (tokenExpired) {
      localStorage.removeItem('gmint_token');
      this._router.navigate(['/signin'], { queryParams: { returnUrl: this._router.url } });
      return false;
    }

    const jwt: any = this._jwtHelper.decodeToken(this.token);
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
		if (!this.token) {
			console.log("[JWT Refresher]", "/ EMPTY TOKEN");
			return;
		}

		const jwt:any = this._jwtHelper.decodeToken(this.token);
		if (!jwt || !jwt.hasOwnProperty('exp') || !jwt.hasOwnProperty('iat') || !jwt.hasOwnProperty('gm_area') || jwt.gm_area !== 'authorized') {
			console.log("[JWT Refresher]", "/ INVALID TOKEN");
			return;
		}

		var fullTtlSeconds = jwt.exp - jwt.iat;
		if (!this._jwtHelper.isTokenExpired(this.token, 3)) {
			var remainSeconds = (this._jwtHelper.getTokenExpirationDate(this.token).getTime() - new Date().getTime()) / 1000;
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
