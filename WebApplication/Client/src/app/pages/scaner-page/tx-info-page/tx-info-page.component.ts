import {ChangeDetectionStrategy, ChangeDetectorRef, Component, HostBinding, OnDestroy, OnInit} from '@angular/core';
import {MessageBoxService} from "../../../services/message-box.service";
import {APIService} from "../../../services";
import {ActivatedRoute} from "@angular/router";
import {Subscription} from "rxjs/Subscription";
import {TransactionInfo} from "../../../interfaces/transaction-info";
import {Base64} from 'js-base64';
import {TranslateService} from "@ngx-translate/core";

@Component({
  selector: 'app-tx-info-page',
  templateUrl: './tx-info-page.component.html',
  styleUrls: ['./tx-info-page.component.sass'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TxInfoPageComponent implements OnInit, OnDestroy {

  @HostBinding('class') class = 'page';

  public loading: boolean = false;
  public tx: TransactionInfo = null;
  public isPending: boolean = false;
  public isTextDataPiece: boolean = true;
  public digest: string;
  public network: string = null;
  public dataPiece = {
    text: null,
    hex: null,
    size: null
  };
  public switchModel: {
    type: 'text'|'hex'
  };

  private sub1: Subscription;
  private interval;
  private txStatus = {
    1: "pending",
    2: "approved",
    3: "failed",
    4: "stale",
    5: "notfound",
  };
  public isInvalidDigest: boolean = false;

  constructor(
    private apiService: APIService,
    private cdRef: ChangeDetectorRef,
    private messageBox: MessageBoxService,
    private route: ActivatedRoute,
    private translate: TranslateService,
  ) { }

  ngOnInit() {
    this.switchModel = {
      type: 'text'
    };
    this.sub1 = this.route.params.subscribe(params => {
      this.loading = true;
      this.digest = params.id;
      this.network = params.network;
      this.getTransactionInfo();
    });
  }

  getTransactionInfo() {
    this.apiService.checkTransactionStatus(this.digest, this.network).subscribe((data: any) => {
      this.tx = data.res;

      if (this.tx.transaction && this.tx.transaction.data_piece) {
        this.dataPiece.text = Base64.decode(this.tx.transaction.data_piece);
        this.dataPiece.hex = this.base64toHEX(this.tx.transaction.data_piece);

        this.dataPiece.hex.length / 2 > this.tx.transaction.data_size &&
        (this.dataPiece.size = this.dataPiece.hex.length / 2 - this.tx.transaction.data_size);
      }

      if (!this.tx.transaction || this.tx.status === this.txStatus[5]) {
        this.isPending = true;
        clearInterval(this.interval);
        this.interval = setInterval(() => {
          this.getTransactionInfo();
        }, 30000);
        this.cdRef.markForCheck();
      } else {
        this.isPending = this.loading = false;
        clearInterval(this.interval);
        this.cdRef.markForCheck();
      }
    }, (error) => {
      this.catchError(error);
    });
  }

  changeViewDataPiece(isText: boolean) {
    this.isTextDataPiece = isText;
  }

  base64toHEX(base64) {
    let raw = atob(base64);
    let HEX = '';

    for (let i = 0; i < raw.length; i++ ) {
      let _hex = raw.charCodeAt(i).toString(16);
      HEX += (_hex.length==2?_hex:'0'+_hex);
    }
    return HEX.toUpperCase();
  }

  catchError(error) {
    if (error.status === 400) {
      this.isInvalidDigest = true;
    } else {
      this.translate.get('APIErrors.wrong').subscribe(phrase => {
        this.messageBox.alert(phrase);
      });
    }
    this.loading = false;
    this.cdRef.markForCheck();
  }

  ngOnDestroy() {
    this.sub1 && this.sub1.unsubscribe();
    clearInterval(this.interval);
  }
}
