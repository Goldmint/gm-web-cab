import {
  AfterViewInit,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  EventEmitter,
  Input,
  OnInit,
  Output
} from '@angular/core';
import {APIService, EthereumService, MessageBoxService} from "../../services";
import {Subscription} from "rxjs/Subscription";
import {Subject} from "rxjs/Subject";
import * as Web3 from "web3";

@Component({
  selector: 'app-cryptocurrency-block',
  templateUrl: './cryptocurrency-block.component.html',
  styleUrls: ['./cryptocurrency-block.component.sass'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CryptocurrencyBlockComponent implements OnInit {

  public agreeCheck: boolean = false;
  public isMobile: boolean = false;
  public loading: boolean = false;
  public isDataLoaded: boolean = false;
  public isFirstTransaction: boolean = true;
  public showConfirmBlock: boolean = false;
  public isTradingError = false;
  public expiresTime: number;

  public subGetGas: Subscription;
  private requestId: number;
  private destroy$: Subject<boolean> = new Subject<boolean>();
  private Web3 = new Web3();
  private amount;

  @Input('amount') transferData;
  @Output() hideForm: EventEmitter<any> = new EventEmitter();
  @Output() tradingError: EventEmitter<any> = new EventEmitter();

  constructor(
    private _apiService: APIService,
    private _ethService: EthereumService,
    private _messageBox: MessageBoxService,
    private _cdRef: ChangeDetectorRef
  ) { }

  ngOnInit() {
    this.isMobile = (window.innerWidth <= 767);
    window.onresize = () => {
      this.isMobile = window.innerWidth <= 767 ? true : false;
      this._cdRef.markForCheck();
    };

    this._apiService.transferTradingError$.takeUntil(this.destroy$).subscribe(status => {
      this.isTradingError = !!status;
      this.transferTradingError();
      this._cdRef.markForCheck();
    });

    this._apiService.transferTradingLimit$.takeUntil(this.destroy$).subscribe(() => {
      this.hideCryptoCurrencyForm();
      this._cdRef.markForCheck();
    });

    if (this.transferData.type === 'buy') {
      this._apiService.goldBuyAsset(this.transferData.ethAddress, this.Web3.toWei(+this.transferData.amount), this.transferData.reversed, this.transferData.currency)
        .subscribe(res => {
          this.expiresTime = res.data.expires;

          if (this.transferData.reversed) {
            this.amount = res.data.estimation.amount;
            this.transferData.amountView = this.substrValue(this.amount / Math.pow(10, 18)) + ' Eth';
            this.transferData.estimatedView = +this.transferData.amount + ' GOLD';
          } else {
            this.amount = this.Web3.toWei(this.transferData.coinAmount);
            this.transferData.amountView = +this.transferData.amount + ' Eth';
            this.transferData.estimatedView = this.substrValue(res.data.estimation.amount / Math.pow(10, 18)) + ' GOLD';
          }

          this.requestId = res.data.requestId;
          this.isDataLoaded = true;
          this._cdRef.markForCheck();
        });
    }

    if (this.transferData.type === 'sell') {
      this._apiService.goldSellAsset(this.transferData.ethAddress, this.Web3.toWei(+this.transferData.amount), this.transferData.reversed, this.transferData.currency)
        .subscribe(res => {
          this.expiresTime = res.data.expires;

          if (this.transferData.reversed) {
            this.amount = res.data.estimation.amount;
            this.transferData.estimatedView =  +this.transferData.amount + ' Eth';
            this.transferData.amountView = this.substrValue(this.amount / Math.pow(10, 18)) + ' GOLD';
          } else {
            this.amount = this.Web3.toWei(this.transferData.coinAmount);
            this.transferData.amountView = +this.transferData.amount + ' GOLD';
            this.transferData.estimatedView = this.substrValue(res.data.estimation.amount / Math.pow(10, 18)) + ' Eth';
          }

          this.requestId = res.data.requestId;
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

  hideCryptoCurrencyForm() {
    this.hideForm.emit(true);
  }

  backAfterStopTimer() {
    this.hideCryptoCurrencyForm();
  }

  transferTradingError() {
    this.tradingError.emit(true);
  }

  hideConfirmBlock() {
    this.hideCryptoCurrencyForm();
  }

  buyMethod() {
    this.loading = this.isFirstTransaction = true;

    this._apiService.goldBuyConfirm(this.requestId)
      .finally(() => {
        this.loading = false;
        this._cdRef.markForCheck();
      }).subscribe(() => {
      this.subGetGas = this._ethService.getObservableGasPrice().subscribe((price) => {
        if (price !== null && this.isFirstTransaction) {
          this.showConfirmBlock = true;
          this._ethService.sendBuyRequest(this.transferData.ethAddress, this.transferData.userId, this.requestId, this.amount, +price);
          this.isFirstTransaction = false;
          this._cdRef.markForCheck();
        }
      });
    });
  }

  sellMethod() {
    this.loading = this.isFirstTransaction = true;

    this._apiService.goldSellConfirm(this.requestId)
      .finally(() => {
        this.loading = false;
        this._cdRef.markForCheck();
      }).subscribe(() => {
      this.subGetGas = this._ethService.getObservableGasPrice().subscribe((price) => {
        if (price !== null && this.isFirstTransaction) {
          this.showConfirmBlock = true;
          this._ethService.sendSellRequest(this.transferData.ethAddress, this.transferData.userId, this.requestId, this.amount, +price);
          this.isFirstTransaction = false;
          this._cdRef.markForCheck();
        }
      });
    });
  }

  onSubmit() {
    this.transferData.type === 'buy' ? this.buyMethod() : this.sellMethod();
  }

  ngOnDestroy() {
    this.destroy$.next(true);
    this.subGetGas && this.subGetGas.unsubscribe();
  }

}
