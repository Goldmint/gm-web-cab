import { Component, OnInit, HostBinding, EventEmitter, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef, ViewChild, isDevMode } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { NgForm } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';
import { RecaptchaComponent as reCaptcha } from 'ng-recaptcha';
import { JwtHelperService } from '@auth0/angular-jwt';
import 'rxjs/add/operator/finally';

import { APIService, UserService } from '../../services';
import { MessageBoxService } from '../../services/message-box.service';

@Component({
  selector: 'app-login-page',
  templateUrl: './login-page.component.html',
  styleUrls: ['./login-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoginPageComponent implements OnInit {
  @HostBinding('class') class = 'page page--auth';
  @ViewChild('captchaRef') captchaRef: reCaptcha;

  @ViewChild('userNameElement') userNameElement;
  @ViewChild('userPassElement') userPassElement;

  public loginModel: any = {};
  public tfaModel: any = {};
  public loading = false;
  public buttonBlur = new EventEmitter<boolean>();
  public errors = [];

  public tfaRequired: boolean;
  public autofill: boolean = false;
  private _returnUrl: string;

  constructor(
    private _router: Router,
    private _route: ActivatedRoute,
    private _apiService: APIService,
    private _userService: UserService,
    private _translate: TranslateService,
    private _cdRef: ChangeDetectorRef,
    private _messageBox: MessageBoxService) {

    this._route.params
      .subscribe(params => {
        if (params.token) {
          this._userService.processToken(params.token);
        }
      });
  }

  ngOnInit() {

    // if (isDevMode()) {
    //   this.loginModel.recaptcha = "devmode";
    // }

    this.matchesPolyfill();

    this._userService.currentUser.subscribe(currentUser => {
      if (currentUser && currentUser.hasOwnProperty('challenges')) {
        console.log('currentUser.challenges', currentUser, currentUser.challenges);

        if (currentUser.challenges.indexOf('2fa') > -1 && (!sessionStorage || !parseInt(sessionStorage.getItem('gmint_uc_2fa'), 0))) {
          sessionStorage && sessionStorage.setItem('gmint_uc_2fa', '1');

          this._router.navigate(['/signup/2fa']);
        }
      }
    });

    if (this._userService.isAuthenticated()) {
      this._router.navigate(['/home']);
    }
    else {
      this._returnUrl = this._route.snapshot.queryParams['returnUrl'] || '/';
      this.tfaRequired = !localStorage || !!parseInt(localStorage.getItem('gmint_2fa'), 0);
    }
  }

  resetAutofillFlag() {
    this.autofill = null;
  }

  public login() {
    this.loading = true;
    this.buttonBlur.emit();

    this._userService.login(this.loginModel.username, this.loginModel.password, this.loginModel.recaptcha)
      .finally(() => {
        this.loading = false;
        this._cdRef.detectChanges();
      })
      .subscribe(
        res => {
          if (res.data.tfaRequired) {
            this.tfaRequired = true;
          }
          else {
            this._router.navigate([this._returnUrl]);
          }
        },
        err => {
          this.captchaRef.reset();

          if (err.error && err.error.errorCode) {
            switch (err.error.errorCode) {
              case 100: // InvalidParameter
                for (let i = err.error.data.length - 1; i >= 0; i--) {
                  this.errors[err.error.data[i].field] = err.error.data[i].desc;
                }
                break;

              case 1000: // AccountNotFound
                this._translate.get('ERRORS.Login.AccountNotFound').subscribe(phrase => {
                    this.errors['Password'] = phrase;
                });
                break;

              case 1011: // AccountDpaNotSigned
                this._router.navigate(['/signin/dpa/required']);
                break;

              // case 1001: // AccountLocked
              //   this._translate.get('ERRORS.Login.AccountLocked').subscribe(phrase => {
              //     this._messageBox.alert(phrase);
              //     this.resetStorage();
              //   });
              //   break;
              //
              // case 1002: // AccountEmailNotConfirmed
              //   this._translate.get('ERRORS.Login.AccountEmailNotConfirmed').subscribe(phrase => {
              //     this._messageBox.alert(phrase);
              //   });
              //   break;
              //
              // default:
              //   this._messageBox.alert(err.error.errorDesc);
              //   break;
            }
          }
        });
  }

  public proceedTFA() {
    this.loading = true;
    this.buttonBlur.emit();

    this._userService.proceedTFA(this.tfaModel.code)
      .finally(() => {
        this.loading = false;
        this._cdRef.detectChanges();
      })
      .subscribe(
        res => {
          //if (this._userService.user.challenges && this._userService.user.challenges.indexOf('2fa') > -1) {
          //  this._router.navigate(['/signup/2fa']);
          //}
          //else {
            this._router.navigate([this._returnUrl]);
          //}
        },
        err => {
          if (err.error && err.error.errorCode) {
            switch (err.error.errorCode) {
              case 100: // InvalidParameter
                for (let i = err.error.data.length - 1; i >= 0; i--) {
                  this.errors[err.error.data[i].field] = err.error.data[i].desc;
                }
                break;

              default:
                this._messageBox.alert(err.error.errorDesc);
                break;
            }
          }
        });
  }

  public signInWithProvider(provider: string, e?: any) {
    if (e) e.preventDefault();

    this._userService.loginWithSocial(provider)
      .subscribe(redirect => {
        window.location.href = redirect.data.redirect;
      });
  }

  private matchesPolyfill() {
    if (Element && !Element.prototype.matches) {
      var proto = Element.prototype;
      proto.matches = proto['matchesSelector'] ||
        proto['mozMatchesSelector'] || proto['msMatchesSelector'] ||
        proto['oMatchesSelector'] || proto['webkitMatchesSelector'];
    }
  }

  public captchaResolved(captchaResponse: string, loginForm: NgForm) {
    console.log(`Resolved captcha with response ${captchaResponse}:`);

    this.errors['Captcha'] = false;
    this.loginModel.recaptcha = captchaResponse;

    const name = this.userNameElement.nativeElement;
    const pass = this.userPassElement.nativeElement;

    if (name.matches(":-webkit-autofill") && pass.matches(":-webkit-autofill") && this.autofill !== null) {
      this.autofill = true;
      this._cdRef.markForCheck();
    }
  }

  private resetStorage() {
    try { // Safari in incognito mode doesn't have storage objects
      localStorage.removeItem('gmint_token');
      localStorage.removeItem('gmint_2fa');
      sessionStorage.removeItem('gmint_uc_2fa');
    } catch(e) {}
  }

  public back() {
    this.resetStorage();
    this.tfaRequired = false;
    this._cdRef.detectChanges();
  }

}
