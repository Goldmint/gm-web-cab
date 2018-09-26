import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  EventEmitter,
  Input,
  OnDestroy,
  OnInit,
  Output
} from '@angular/core';
import {APIService, EthereumService, MessageBoxService, UserService} from "../../services";
import {Router} from "@angular/router";
import {Subscription} from "rxjs/Subscription";
import {Subject} from "rxjs/Subject";
import {TranslateService} from "@ngx-translate/core";
import * as Web3 from "web3";
import {BigNumber} from "bignumber.js";

@Component({
  selector: 'app-payment-card-block',
  templateUrl: './payment-card-block.component.html',
  styleUrls: ['./payment-card-block.component.sass'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PaymentCardBlockComponent implements OnInit, OnDestroy {

  public agreeCheck: boolean = false;
  public isMobile: boolean = false;
  public loading: boolean = false;
  public isDataLoaded: boolean = false;
  public isFirstTransaction: boolean = true;
  public showConfirmBlock: boolean = false;
  public isTradingError = false;

  public subGetGas: Subscription;
  private buyRequestId: number;
  private sellRequestId: number;
  private destroy$: Subject<boolean> = new Subject<boolean>();
  private Web3 = new Web3();
  private goldAmount: BigNumber;
  public expiresTime: number;

  @Input('amount') transferData;
  @Output() hideForm: EventEmitter<any> = new EventEmitter();
  @Output() tradingError: EventEmitter<any> = new EventEmitter();

  constructor(
    private _apiService: APIService,
    private _ethService: EthereumService,
    private _messageBox: MessageBoxService,
    private _cdRef: ChangeDetectorRef,
    private _translate: TranslateService,
    private userService: UserService,
    private router: Router
  ) { }

  ngOnInit() {
    this.isMobile = (window.innerWidth <= 767);

    this.userService.windowSize$.takeUntil(this.destroy$).subscribe(size => {
      this.isMobile = size <= 767 ? true : false;
      this._cdRef.markForCheck();
    });

    this._apiService.transferTradingError$.takeUntil(this.destroy$).subscribe(status => {
      this.isTradingError = !!status;
      this.transferTradingError();
      this._cdRef.markForCheck();
    });

    this._apiService.transferTradingLimit$.takeUntil(this.destroy$).subscribe(() => {
      this.hidePaymentCardForm();
      this._cdRef.markForCheck();
    });

    if (this.transferData.type === 'buy') {
      let amount = this.transferData.reversed ? this.Web3.toWei(+this.transferData.amount) : (+this.transferData.amount * 100);

      this._apiService.buyGoldFiat(this.transferData.cardId, this.transferData.ethAddress, this.transferData.currency, amount.toString(), this.transferData.reversed, this.transferData.promoCode)
        .subscribe((res) => {
          this.expiresTime = res.data.expires;

          if (this.transferData.reversed) {
            this.transferData.amountView = res.data.estimation.amount + ' USD';
            this.transferData.estimatedView = +this.transferData.amount + ' GOLD';
          } else {
            this.transferData.amountView = +this.transferData.amount + ' USD';
            this.transferData.estimatedView = this.substrValue(res.data.estimation.amount / Math.pow(10, 18)) + ' GOLD';
          }

          this.buyRequestId = res.data.requestId;
          this.isDataLoaded = true;
          this._cdRef.markForCheck();
        });
    }

    if (this.transferData.type === 'sell') {
      let amount = this.transferData.reversed ? (+this.transferData.amount * 100) : this.Web3.toWei(+this.transferData.amount);

      this._apiService.sellGoldFiat(this.transferData.cardId, this.transferData.ethAddress, this.transferData.currency, amount.toString(), this.transferData.reversed)
        .subscribe((res) => {
          this.expiresTime = res.data.expires;

          if (this.transferData.reversed) {
            this.transferData.amountView = this.substrValue(res.data.estimation.amount / Math.pow(10, 18)) + ' GOLD';
            this.transferData.estimatedView = +this.transferData.amount + ' USD';
            this.goldAmount = res.data.estimation.amount;
          } else {
            this.transferData.amountView = +this.transferData.amount + ' GOLD';
            this.transferData.estimatedView = res.data.estimation.amount + ' USD';
            this.goldAmount = this.Web3.toWei(+this.transferData.goldAmount);
          }

          this.sellRequestId = res.data.requestId;
          this.isDataLoaded = true;
          this._cdRef.markForCheck();
        });
    }

  }

  substrValue(value: number|string) {
    return value.toString()
      .replace(',', '.')
      .replace(/([^\d.])|(^\.)/g, '')
      .replace(/^(\d+)(?:(\.\d{0,6})[\d.]*)?/, '$1$2')
      .replace(/^0+(\d)/, '$1');
  }

  hidePaymentCardForm() {
    this.hideForm.emit(true);
  }

  transferTradingError() {
    this.tradingError.emit(true);
  }

  buyMethod() {
    this.loading = true;
    this._apiService.goldBuyConfirm(this.buyRequestId, this.transferData.promoCode)
      .finally(() => {
        this.loading = this.agreeCheck = false;
        this._cdRef.markForCheck();
      }).subscribe(() => {
      this.router.navigate(['/finance/history']);
    });
  }

  sellMethod() {
    this.isFirstTransaction = this.loading = true;
    this.subGetGas && this.subGetGas.unsubscribe();

    this._apiService.goldSellConfirm(this.sellRequestId)
      .finally(() => {
        this.loading = this.agreeCheck = false;
        this._cdRef.markForCheck();
      })
      .subscribe(() => {
        this.subGetGas = this._ethService.getObservableGasPrice().subscribe((price) => {
          if (price !== null && this.isFirstTransaction) {
            this.showConfirmBlock = true;
            this._ethService.sendSellRequest(this.transferData.ethAddress, this.transferData.userId, this.sellRequestId, this.goldAmount.toString(), +price * Math.pow(10, 9));
            this.isFirstTransaction = false;
            this._cdRef.markForCheck();
          }
        });
      });
  }

  hideConfirmBlock() {
    this.hidePaymentCardForm();
  }

  backAfterStopTimer() {
    this.hidePaymentCardForm();
  }

  onSubmit() {
    this.transferData.type === 'buy' ? this.buyMethod() : this.sellMethod();
  }

  ngOnDestroy() {
    this.destroy$.next(true);
    this.subGetGas && this.subGetGas.unsubscribe();
  }

}
