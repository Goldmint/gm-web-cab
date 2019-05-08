import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  EventEmitter,
  Input,
  OnInit,
  Output
} from '@angular/core';
import {APIService, EthereumService, MessageBoxService, UserService} from "../../services";
import {Subject} from "rxjs/Subject";
import * as Web3 from "web3";
import {TranslateService} from "@ngx-translate/core";

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
  public isTradingError = false;
  public expiresTime: number;

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
    private _cdRef: ChangeDetectorRef,
    private userService: UserService,
    private _translate: TranslateService
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
      this.hideCryptoCurrencyForm();
      this._cdRef.markForCheck();
    });

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

  sellMethod() {
    this.loading = true;

    this._apiService.goldSellConfirm(this.requestId)
      .finally(() => {
        this.loading = false;
        this._cdRef.markForCheck();
      }).subscribe(() => {
        this._translate.get('MessageBox.RequestProgress').subscribe(phrase => {
          this._messageBox.alert(phrase);
        });
        this.hideCryptoCurrencyForm();
    });
  }

  onSubmit() {
    this.transferData.type === 'sell' ? this.sellMethod() : null;
  }

  ngOnDestroy() {
    this.destroy$.next(true);
  }

}
