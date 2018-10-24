import {ChangeDetectionStrategy, ChangeDetectorRef, Component, HostBinding, OnDestroy, OnInit} from '@angular/core';
import {MessageBoxService} from "../../../services/message-box.service";
import {APIService} from "../../../services";
import {ActivatedRoute} from "@angular/router";
import {Subscription} from "rxjs/Subscription";
import {TransactionInfo} from "../../../interfaces/transaction-info";

@Component({
  selector: 'app-tx-info-page',
  templateUrl: './tx-info-page.component.html',
  styleUrls: ['./tx-info-page.component.sass'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TxInfoPageComponent implements OnInit, OnDestroy {

  @HostBinding('class') class = 'page';

  public loading: boolean = false;
  public transactionInfo: TransactionInfo;
  public isNotFound: boolean = false;
  public isPending: boolean = false;
  public txHash: string;
  private sub1: Subscription;
  private interval;

  constructor(
    private apiService: APIService,
    private cdRef: ChangeDetectorRef,
    private messageBox: MessageBoxService,
    private route: ActivatedRoute
  ) { }

  ngOnInit() {
    this.sub1 = this.route.params.subscribe(params => {
      this.loading = true;
      this.txHash = params.id;
      this.getTransactionInfo();
    });
  }

  getTransactionInfo() {
    this.apiService.checkTransactionStatus(this.txHash).subscribe(data => {
      this.transactionInfo = data['data'];
      if (this.transactionInfo.status === 3 && this.transactionInfo.tx === null) {
        this.isPending = true;
        clearInterval(this.interval);
        this.interval = setInterval(() => {
          this.getTransactionInfo();
        }, 30000);
        this.cdRef.markForCheck();
      } else {
        this.isPending = false;
        clearInterval(this.interval);
        this.transactionInfo.tx && (this.transactionInfo.tx.timeStamp = new Date(this.transactionInfo.tx.timeStamp.toString() + 'Z'));
        this.loading = false;
        this.cdRef.markForCheck();
      }
    }, (error) => {
      error.error.errorCode === 4 && (this.isNotFound = true);
      this.loading = false;
      this.cdRef.markForCheck();
    });
  }

  ngOnDestroy() {
    this.sub1 && this.sub1.unsubscribe();
    clearInterval(this.interval);
  }

}
