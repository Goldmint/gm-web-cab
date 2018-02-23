import { Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';

import { User } from '../../interfaces';
import { UserService, MessageBoxService } from '../../services';

@Component({
  selector: 'app-header',
  templateUrl: './header-block.component.html',
  styleUrls: ['./header-block.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HeaderBlockComponent implements OnInit {

  public user: User;
  public locale: string;

  constructor(
    private _userService: UserService,
    private _cdRef: ChangeDetectorRef,
    private _messageBox: MessageBoxService
  ) {
  }

  ngOnInit() {

    this._userService.currentUser.subscribe(currentUser => {
      this.user = currentUser;
      this._cdRef.detectChanges();
    });

    this._userService.currentLocale.subscribe(currentLocale => {
      this.locale = currentLocale;
      this._cdRef.detectChanges();
    });
  }

  public logout(e) {
    e.preventDefault();

    this._messageBox.confirm('Are you sure you want to log out?')
      .subscribe(confirmed => {
        if (confirmed) {
          this._userService.logout(e);
          this._cdRef.detectChanges();
        }
      });
  }

  public isLoggedIn() {
    return this._userService.isAuthenticated();
  }

}
