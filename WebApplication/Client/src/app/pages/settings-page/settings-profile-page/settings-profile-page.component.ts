import { Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';

import { User, TFAInfo } from '../../../interfaces';
import { UserService, APIService } from '../../../services';
import { Observable } from "rxjs/Observable";

@Component({
  selector: 'app-settings-profile-page',
  templateUrl: './settings-profile-page.component.html',
  styleUrls: ['./settings-profile-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SettingsProfilePageComponent implements OnInit {
  
  private _loading = true;
  private _changingPassword: boolean;

  private _user: User;
  private _tfaInfo: TFAInfo;
  private passwordModel: any = {};
  
  constructor(
    private _userService: UserService,
    private _apiService: APIService,
    private _cdRef: ChangeDetectorRef
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
      this._cdRef.markForCheck();
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
    this._cdRef.markForCheck();

    setTimeout(() => {
      this._changingPassword = false;
      this._cdRef.markForCheck();
    }, 3000);
  }

}
