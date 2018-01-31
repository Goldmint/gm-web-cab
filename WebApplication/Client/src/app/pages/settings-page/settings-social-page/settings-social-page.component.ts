import { Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';

import { User } from '../../../interfaces';
import { UserService, APIService } from '../../../services';

@Component({
  selector: 'app-settings-social-page',
  templateUrl: './settings-social-page.component.html',
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SettingsSocialPageComponent implements OnInit {

  public user: User;
  public selected: string;
  public loading: boolean = false;

  constructor(
    private userService: UserService,
    private apiService: APIService,
    private cdRef: ChangeDetectorRef) {

    this.user = this.userService.user;
    this.selected = 'facebook';
  }

  ngOnInit() {
  }

  selectSocial(network) {
    this.selected = network;
  }

}
