import {ChangeDetectionStrategy, ChangeDetectorRef, Component, HostBinding, OnDestroy, OnInit} from '@angular/core';
import {MessageBoxService} from "../../../services/message-box.service";
import {APIService} from "../../../services";
import {ActivatedRoute} from "@angular/router";
import {Subscription} from "rxjs/Subscription";
import {TransactionInfo} from "../../../interfaces/transaction-info";
import {Base64} from 'js-base64';

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
  public isNotFound: boolean = false;
  public isPending: boolean = false;
  public digest: string;
  private sub1: Subscription;
  private interval;
  private txStatus = {
    1: "pending",
    2: "approved",
    3: "failed",
    4: "stale",
    5: "notfound",
  };

  constructor(
    private apiService: APIService,
    private cdRef: ChangeDetectorRef,
    private messageBox: MessageBoxService,
    private route: ActivatedRoute
  ) { }

  ngOnInit() {
    this.sub1 = this.route.params.subscribe(params => {
      this.loading = true;
      this.digest = params.id;
      this.getTransactionInfo();
    });
  }

  getTransactionInfo() {
    this.apiService.checkTransactionStatus(this.digest).subscribe((data: any) => {
      this.tx = data.res;

      if (this.tx.status === this.txStatus[1]) {
        this.isPending = true;
        clearInterval(this.interval);
        this.interval = setInterval(() => {
          this.getTransactionInfo();
        }, 30000);
        this.cdRef.markForCheck();
      } else {
        this.tx.status === this.txStatus[5] && (this.isNotFound = true);
        this.isPending = this.loading = false;
        clearInterval(this.interval);
        this.cdRef.markForCheck();
      }
    }, () => {
      this.loading = false;
      this.cdRef.markForCheck();
    });
  }

  ngOnDestroy() {
    this.sub1 && this.sub1.unsubscribe();
    clearInterval(this.interval);
  }
}
