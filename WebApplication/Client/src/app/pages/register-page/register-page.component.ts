import { Component, OnInit, HostBinding, EventEmitter, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef, ViewChild, isDevMode } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { RecaptchaComponent as reCaptcha } from 'ng-recaptcha';
import 'rxjs/add/operator/finally';
import 'rxjs/add/operator/debounceTime';

import { APIService, UserService } from '../../services';
import { MessageBoxService } from '../../services/message-box.service';

@Component({
  selector: 'app-register-page',
  templateUrl: './register-page.component.html',
  styleUrls: ['./register-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RegisterPageComponent implements OnInit {
  @HostBinding('class') class = 'page page--auth';
  @ViewChild('captchaRef') captchaRef: reCaptcha;
  @ViewChild('signupForm') signupForm;

  public signupModel: any = {};
  public loading = false;
  public passwordChecking = false;
  public submitButtonBlur = new EventEmitter<boolean>();
  public errors: any = [];
  public passwordChanged = false;
  public isAgreementConfirmShown = false;
  public agreeCheck = false;
  public agreeTerms = false;
  public location = window.location.origin + window.location.pathname;

  private returnUrl: string;

  @ViewChild('password') password

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private userService: UserService,
    private cdRef: ChangeDetectorRef,
    private apiService: APIService,
    private _translate: TranslateService,
    private _messageBox: MessageBoxService) {
  }

  ngOnInit() {
    if (this.userService.isAuthenticated()) {
      this.router.navigate(['/master-node']);
    } else {
      this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/';
    }
  }

  agreementConfirmed(status) {
    this.isAgreementConfirmShown = false;
    this.agreeCheck = false;

    if (status) {
      this._register();
    } else {
      this.loading = false;
    }

    this.cdRef.detectChanges();
  }

  public onRegister() {
    this.submitButtonBlur.emit();
    this.isAgreementConfirmShown = true;
    this.cdRef.markForCheck();
  }

  private _register() {
    this.loading = true;
    this.cdRef.markForCheck();
    this.userService.register(this.signupModel.email, this.signupModel.password, this.signupModel.recaptcha, this.agreeCheck)
      .finally(() => {
        this.loading = false;
        this.cdRef.markForCheck();
      })
      .subscribe(
        res => {
          this.userService.processToken(res.data.token);
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

              case 1004: // AccountEmailTaken
                this._translate.get('ERRORS.Registration.AccountEmailTaken').subscribe(phrase => {
                  this.errors['Email'] = phrase;
                });
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

    this.userService.loginWithSocial(provider)
      .subscribe(redirect => {
        window.location.href = redirect.data.redirect;
      });
  }

  public captchaResolved(captchaResponse: string) {
    console.log(`Resolved captcha with response ${captchaResponse}:`);

    this.errors['Captcha'] = false;
    this.signupModel.recaptcha = captchaResponse;
  }

}
