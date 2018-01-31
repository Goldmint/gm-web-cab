import { Component, OnInit, EventEmitter, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef, ViewChild } from '@angular/core';
import { Router, ActivatedRoute } from "@angular/router";
import { JwtHelperService } from '@auth0/angular-jwt';
import { RecaptchaComponent as reCaptcha } from 'ng-recaptcha';
import 'rxjs/add/operator/finally';

import { APIService, MessageBoxService } from "../../../services";

import * as zxcvbn from 'zxcvbn';

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

  private _pages = Pages;
  private _token: string;

  public passwordResetModel: any = {};
  public newPasswordModel: any = {};
  public passwordStrength: number;
  public loading = false;
  public buttonBlur = new EventEmitter<boolean>();
  public errors = [];
  public page: Pages;

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

  onPasswordInput(someVar) {
    let strength = zxcvbn(someVar.value);

    console.log('strength result', strength);

    this.passwordStrength = strength.score;
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
