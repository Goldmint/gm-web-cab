import { Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { ActivatedRoute } from "@angular/router";

import { APIService } from '../../../services';

@Component({
  selector: 'app-register-email-confirmed-page',
  templateUrl: './register-email-confirmed-page.component.html',
  styleUrls: ['./register-email-confirmed-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RegisterEmailConfirmedPageComponent implements OnInit {

  public processing = true;
  private failed = true;

  constructor(
    private _route: ActivatedRoute,
    private _apiService: APIService,
    private _cdRef: ChangeDetectorRef
  ) {

    this._route.params
      .subscribe(params => {
        if (params.token) {

          // try to confirm
          _apiService.userConfirmEmail(params.token)
            .finally(() => {
              this.processing = false;
              _cdRef.detectChanges();
            })
            .subscribe(res => {
              this.failed = false;
            });
        }
        else {
          this.processing = false;
          _cdRef.detectChanges();
        }
      });
  }

  ngOnInit() {
  }

}
