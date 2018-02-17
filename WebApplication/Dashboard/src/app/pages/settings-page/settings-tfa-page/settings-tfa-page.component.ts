import { Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef, EventEmitter } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import 'rxjs/add/operator/finally';

import { TFAInfo } from '../../../interfaces';
import { APIService } from '../../../services';
import { MessageBoxService } from '../../../services/message-box.service';

@Component({
  selector: 'app-settings-tfa-page',
  templateUrl: './settings-tfa-page.component.html',
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SettingsTFAPageComponent implements OnInit {

  public tfaModel: any = {};
  public tfaInfo: TFAInfo;

  public loading = true;
  public processing = false;
  public buttonBlur = new EventEmitter<boolean>();
  public errors = [];

  constructor(
    private _apiService: APIService,
    private _cdRef: ChangeDetectorRef,
    private _translate: TranslateService,
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

  enableTFA() {
    this.buttonBlur.emit();
    this.processing = true;

    this._apiService.verifyTFACode(this.tfaModel.code, !this.tfaInfo.enabled)
      .finally(() => {
        this.processing = false;
        this._cdRef.detectChanges();
      })
      .subscribe(
        res => {
          this.tfaInfo = res.data;
        },
        err => {
          if (err.error && err.error.errorCode) {
            switch (err.error.errorCode) {
              case 100:
                this._translate.get('ERRORS.TFA.InvalidCode').subscribe(phrase => {
                  this.errors['Code'] = phrase;
                });
                break;

              default:
                this._messageBox.alert(err.error.errorDesc);
                break;
            }
          }
        });
  }

}
