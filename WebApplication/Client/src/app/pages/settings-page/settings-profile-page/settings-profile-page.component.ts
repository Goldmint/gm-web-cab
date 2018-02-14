import { Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef, ViewChild } from '@angular/core';

import { User, TFAInfo } from '../../../interfaces';
import { UserService, APIService, MessageBoxService } from '../../../services';
import { Observable } from "rxjs/Observable";
import { NgForm } from "@angular/forms";

@Component({
  selector: 'app-settings-profile-page',
  templateUrl: './settings-profile-page.component.html',
  styleUrls: ['./settings-profile-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SettingsProfilePageComponent implements OnInit {

  @ViewChild('passwordForm') passwordForm: NgForm;

  private _loading = true;
  private _changingPassword: boolean;

  private _user: User;
  private _tfaInfo: TFAInfo;
  private passwordModel = { current: "", new: "", tfaCode: "" };
  private passwordErrors = [];

  constructor(
    private _userService: UserService,
    private _apiService: APIService,
    private _cdRef: ChangeDetectorRef,
    private messageBox: MessageBoxService
  ) {
  }

  ngOnInit() {
    Observable.combineLatest(
      this._userService.currentUser,
      this._apiService.getTFAInfo()
    ).subscribe(res => {
      this._user = res[0];
      this._tfaInfo = res[1].data;
      this._loading = false;
      this._cdRef.detectChanges();
    });
  }

  // ---

  changeEmail() {
    //this.loading = true;

    setTimeout(() => {
      //this.loading = false;
      this._cdRef.detectChanges();

      //alert('Method changeEmail(): \n' + this.user.email);
    }, 3000);
  }

  changePassword() {
    this._changingPassword = true;
    this.passwordErrors = [];
    this._cdRef.detectChanges();

    this._apiService.changePassword(this.passwordModel.current, this.passwordModel.new, this.passwordModel.tfaCode)
      .finally(() => {
        this._changingPassword = false;
        this._cdRef.detectChanges();
      })
      .subscribe(res => {
        this.passwordForm.resetForm();
        this.messageBox.alert("Password successfully changed");
      }, err => {
        if (err.error && err.error.errorCode) {
          switch (err.error.errorCode) {
            case 100: // InvalidParameter
              for (let i = err.error.data.length - 1; i >= 0; i--) {
                this.passwordErrors[err.error.data[i].field] = err.error.data[i].desc;
              }
              break;
          }
        }
      });
  }

}
