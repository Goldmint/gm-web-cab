import { Component, OnInit, ViewEncapsulation, ChangeDetectionStrategy, ChangeDetectorRef, EventEmitter } from '@angular/core';
import { Router } from '@angular/router';
import 'rxjs/add/operator/finally';

import { TFAInfo } from '../../../../interfaces';
import { APIService, MessageBoxService } from '../../../../services';

@Component({
  selector: 'app-tfa-enable-page',
  templateUrl: './tfa-enable-page.component.html',
  styleUrls: ['./tfa-enable-page.component.sass'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TfaEnablePageComponent implements OnInit {

  public tfaModel: any = {};
  public tfaInfo: TFAInfo;

  public loading = true;
  public processing = false;
  public buttonBlur = new EventEmitter<boolean>();

  constructor(
    private apiService: APIService,
    private cdRef: ChangeDetectorRef,
    private router: Router,
    private _messageBox: MessageBoxService) {

    this.tfaInfo = {enabled: false} as TFAInfo;

    this.apiService.getTFAInfo()
      .finally(() => {
        this.loading = false;
        this.cdRef.detectChanges();
      })
      .subscribe(
        res => {
          this.tfaInfo = res.data;
        },
        err => {
          if (err.error.errorCode === 50) {
            //@todo: handle error
            this._messageBox.alert(err.error.errorDesc);

            this.router.navigate(['/signin']);
          }
        }
      );
  }

  ngOnInit() {
  }

  enableTFA() {
    this.buttonBlur.emit();
    this.processing = true;

    //@todo: uncomment this block when the 2fa functionality would be implemented
    // this.apiService.verifyTFACode(this.tfaModel.code)
    //   .subscribe(
    //     res => {
    //       this.tfaInfo = res.data;
    //     },
    //     err => {},
    //     () => {
    //       this.processing = false;
    //       this.cdRef.detectChanges();
    //     });

    alert('Demo. Uncomment this block when the 2fa functionality would be implemented.');

    setTimeout(() => {
      this.tfaInfo.enabled = true;
      this.processing = false;
      this.cdRef.detectChanges();
    }, 1250);
  }

}
