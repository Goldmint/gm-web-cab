import { Component, OnInit, HostBinding, EventEmitter, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef, ViewChild, isDevMode } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { RecaptchaComponent as reCaptcha } from 'ng-recaptcha';
import 'rxjs/add/operator/finally';

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

  public signupModel: any = {};
  public loading = false;
  public submitButtonBlur = new EventEmitter<boolean>();
  public errors: any = [];

  private returnUrl: string;

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
    if (isDevMode()) {
      this.signupModel.recaptcha = "devmode";
    }

    if (this.userService.isAuthenticated()) {
      this.router.navigate(['/home']);
    }
    else {
      this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/';
    }
  }

  public register() {
    this.loading = true;
    this.submitButtonBlur.emit();

    this.userService.register(this.signupModel.email, this.signupModel.password, this.signupModel.recaptcha)
      .finally(() => {
        this.loading = false;
        this.cdRef.detectChanges();
      })
      .subscribe(
        res => {
          this.router.navigate(['/signup/success']);
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
