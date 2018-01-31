import { Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef, EventEmitter } from '@angular/core';
import { TabsetComponent } from 'ngx-bootstrap';

import { TFAInfo } from '../../interfaces';
import { APIService, MessageBoxService } from '../../services';

@Component({
  selector: 'app-withdraw-page',
  templateUrl: './withdraw-page.component.html',
  styleUrls: ['./withdraw-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class WithdrawPageComponent implements OnInit {

  public loading: boolean = true;
  public tfaInfo: TFAInfo;

  public withdrawAsset = 'USD';
  public limitsIncrease: boolean = true; //@todo: dev

  constructor(
    private _apiService: APIService,
    private _cdRef: ChangeDetectorRef,
    private _messageBox: MessageBoxService) {

    this.tfaInfo = {enabled: false} as TFAInfo;

    this._apiService.getTFAInfo()
      .finally(() => {
        this.loading = false;
        this._cdRef.detectChanges();
      })
      .subscribe(
        res => {
          this.tfaInfo = res.data;
        },
        err => {});
  }

  ngOnInit() {
  }

}
