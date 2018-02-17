import { Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import 'rxjs/add/operator/finally';

import { User, FiatLimits } from '../../interfaces';
import { APIService, UserService } from '../../services';

@Component({
  selector: 'app-limits-page',
  templateUrl: './limits-page.component.html',
  styleUrls: ['./limits-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LimitsPageComponent implements OnInit {

  public loading: boolean = true;
  public user: User;
  public limits: FiatLimits;
  public limitsSwitchModel: {
    type: 'deposit'|'withdraw',
    currency: string
  };

  constructor(
    private _apiService: APIService,
    private _userService: UserService,
    private _cdRef: ChangeDetectorRef) {

    this.limitsSwitchModel = {
      type: 'deposit',
      currency: 'USD'
    };

    this._apiService.getLimits()
      .finally(() => {
        this.loading = false;
        this._cdRef.detectChanges();
      })
      .subscribe(
        res => {
          this.limits = res.data;
        },
        err => {});
  }

  ngOnInit() {
    this._userService.currentUser
      .subscribe(user => {
        this.user = user;
        this._cdRef.detectChanges();
      });
  }

}
