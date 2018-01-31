import { Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef, EventEmitter } from '@angular/core';

import { User } from '../../../interfaces';
import { UserService, APIService } from '../../../services';

@Component({
  selector: 'app-settings-profile-page',
  templateUrl: './settings-profile-page.component.html',
  styleUrls: ['./settings-profile-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SettingsProfilePageComponent implements OnInit {

  public loading = true;
  public user: User;
  public passwordModel: any = {};
  public submitButtonBlur = new EventEmitter<boolean>();

  constructor(
    private userService: UserService,
    private apiService: APIService,
    private cdRef: ChangeDetectorRef) {
  }

  ngOnInit() {
    this.userService.currentUser.subscribe(user => {
      this.user = user;
      this.onLoading();
    });
  }

  onLoading() {
    if (this.user && this.user.id) {
      this.loading = false;
      this.cdRef.detectChanges();
    }
  }

  changeEmail() {
    //this.loading = true;

    setTimeout(() => {
      //this.loading = false;
      this.cdRef.detectChanges();

      alert('Method changeEmail(): \n' + this.user.email);
    }, 3000);
  }

  changePassword() {
    //this.loading = true;
    this.submitButtonBlur.emit();

    setTimeout(() => {
      //this.loading = false;
      this.cdRef.detectChanges();
    }, 3000);
  }

}
