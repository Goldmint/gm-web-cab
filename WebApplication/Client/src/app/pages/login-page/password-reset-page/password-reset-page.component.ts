import { Component, OnInit, EventEmitter, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef, ViewChild, isDevMode } from '@angular/core';
import { Router, ActivatedRoute } from "@angular/router";
import { JwtHelperService } from '@auth0/angular-jwt';
import { RecaptchaComponent as reCaptcha } from 'ng-recaptcha';
import 'rxjs/add/operator/finally';

import { APIService, MessageBoxService } from "../../../services";

enum Pages {Default, EmailSent, NewPassword}

@Component({
  selector: 'app-password-reset-page',
  templateUrl: './password-reset-page.component.html',
  styleUrls: ['./password-reset-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PasswordResetPageComponent implements OnInit {
  @ViewChild('captchaRef') captchaRef: reCaptcha;
  @ViewChild('newPassword') password

  public pages = Pages;
  private _token: string;

  public passwordResetModel: any = {};
  public newPasswordModel: any = {};
  public loading = false;
  public buttonBlur = new EventEmitter<boolean>();
  public errors = [];
  public page: Pages;
  public isPasswordWeak = false;

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private apiService: APIService,
    private cdRef: ChangeDetectorRef,
    private jwtHelper: JwtHelperService,
    private _messageBox: MessageBoxService) {

    this.page = Pages.Default;

    this.route.params
      .subscribe(
        params => {
          if (params.token) {
            this._token  = params.token;

            this.page    = Pages.NewPassword;
          }
        }
      );
  }

  ngOnInit() {
    if (isDevMode()) {
      this.passwordResetModel.recaptcha = "devmode";
    }

    this.password && this.password.valueChanges
      .debounceTime(500)
      .subscribe(() => {
        this.testPassword();
      });
  }

  captchaResolved(captchaResponse: string) {
    console.log(`Resolved captcha with response ${captchaResponse}:`);

    this.passwordResetModel.recaptcha = captchaResponse;
    this.cdRef.detectChanges();
  }

  sendConfirmationEmail() {
    this.loading = true;
    this.buttonBlur.emit();

    this.apiService.userRestorePassword(this.passwordResetModel.email, this.passwordResetModel.recaptcha)
      .finally(() => {
        this.loading = false;
        this.cdRef.detectChanges();
      })
      .subscribe(
        res => {
          this.page = Pages.EmailSent;
        },
        err => {
          this.captchaRef.reset();

          if (err.error.errorCode) {
            switch (err.error.errorCode) {
              case 100: // InvalidParameter
                for (let i = err.error.data.length - 1; i >= 0; i--) {
                  this.errors[err.error.data[i].field] = err.error.data[i].desc;
                }
                break;

              default:
                //@todo: handle 'new password request' error
                this._messageBox.alert('Something went wrong.');
                break;
            }
          }
        });
  }

  testPassword() {
    if (this.newPasswordModel.password) {
      this.apiService.testPassword(this.newPasswordModel.password)
        .subscribe((data) => {
            this.isPasswordWeak = true;
            this.cdRef.detectChanges();
          },
          (err) => {
            this.isPasswordWeak = false;
            this.cdRef.detectChanges();
          }
        );
    }
  }

  changePassword() {
    this.loading = true;
    this.buttonBlur.emit();

    this.apiService.userChangePassword(this._token, this.newPasswordModel.password)
      .finally(() => {
        this.loading = false;
        this.cdRef.detectChanges();
      })
      .subscribe(
        res => {
          this.router.navigate(['/signin']);

          this._messageBox.alert('Password successfully changed.');
        },
        err => {
          if (err.error.errorCode) {
            switch (err.error.errorCode) {
              case 100: // InvalidParameter
                for (let i = err.error.data.length - 1; i >= 0; i--) {
                  this.errors[err.error.data[i].field] = err.error.data[i].desc;
                }
                break;

              default:
                //@todo: handle 'change password' error
                this._messageBox.alert('Something went wrong.');
                break;
            }
          }
        });
  }

}
