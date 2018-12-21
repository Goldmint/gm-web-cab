import {
  AfterViewInit,
  ChangeDetectorRef,
  Component,
  HostBinding,
  OnInit,
  ViewChild,
  ViewEncapsulation
} from '@angular/core';
import {TranslateService} from "@ngx-translate/core";
import {APIService, EthereumService, GoldrateService, MessageBoxService, UserService} from "../../../services";
import {Observable} from "rxjs/Observable";
import {TFAInfo} from "../../../interfaces";
import {User} from "../../../interfaces/user";
import {BigNumber} from "bignumber.js";
import {Subject} from "rxjs/Subject";
import {Router} from "@angular/router";
import {Subscription} from "rxjs/Subscription";
import {environment} from "../../../../environments/environment";
import * as Web3 from "web3";
import {TradingStatus} from "../../../interfaces/trading-status";


@Component({
  selector: 'app-buy-cryptocurrency-page',
  templateUrl: './buy-cryptocurrency-page.component.html',
  styleUrls: ['./buy-cryptocurrency-page.component.sass'],
  encapsulation: ViewEncapsulation.None
})
export class BuyCryptocurrencyPageComponent implements OnInit, AfterViewInit {
  @HostBinding('class') class = 'page';

  @ViewChild('goldAmountInput') goldAmountInput;
  @ViewChild('coinAmountInput') coinAmountInput;

  public loading = false;
  public processing = false;
  public isFirstLoad = true;
  public isTradingError = false;
  public isTradingLimit: object | boolean = false;
  public showCryptoCurrencyBlock: boolean = false;
  public progress = false;
  public locale: string;

  public ethAddress: string = '';
  public goldRate: number = 0;
  public invalidBalance = false;

  public user: User;
  public tfaInfo: TFAInfo;

  public coinList = ['BTC', 'ETH']
  public currentCoin = this.coinList[1];
  public isReversed: boolean = false;
  public goldAmount: number = 0;
  public coinAmount: number = 0;
  public goldAmountToUSD: number = 0;
  public estimatedAmount: BigNumber;
  public currentValue: number;
  public transferData: object;
  private Web3 = new Web3();

  public ethBalance: BigNumber | null = null;
  public etherscanUrl = environment.etherscanUrl;
  public interval: Subscription;
  public MMNetwork = environment.MMNetwork;
  public isInvalidNetwork: boolean = true;

  public promoCode: string = null;
  public discount: number = 0;
  public isInvalidPromoCode: boolean = false;
  public promoCodeErrorCode: string = null;
  public isAuthenticated: boolean = false;
  public promoCodeModel: string;
  public tradingStatus: TradingStatus;

  private promoCodeLength: number = 11;
  private timeoutPopUp;
  private destroy$: Subject<boolean> = new Subject<boolean>();

  constructor(
    private _userService: UserService,
    private _apiService: APIService,
    private _messageBox: MessageBoxService,
    private _ethService: EthereumService,
    private _goldrateService: GoldrateService,
    private _cdRef: ChangeDetectorRef,
    private _translate: TranslateService,
    private router: Router
  ) { }

  ngOnInit() {
    this.isAuthenticated = this._userService.isAuthenticated();

    this._apiService.transferTradingError$.takeUntil(this.destroy$).subscribe(status => {
      this.isTradingError = !!status;
      this._cdRef.markForCheck();
    });

    this._apiService.transferTradingLimit$.takeUntil(this.destroy$).subscribe(limit => {
      this.isTradingLimit = limit;
      this.isTradingLimit['min'] = this.substrValue(limit['min'] / Math.pow(10, 18));
      this.isTradingLimit['max'] = this.substrValue(limit['max'] / Math.pow(10, 18));

      if (this.isReversed) {
        this.coinAmount = +this.substrValue(limit['cur'] / Math.pow(10, 18));
      }

      this._cdRef.markForCheck();
    });

    this.iniTransactionHashModal();

    if (window.hasOwnProperty('web3') || window.hasOwnProperty('ethereum')) {
      this.timeoutPopUp = setTimeout(() => {
        !this.ethAddress && this._userService.showLoginToMMBox('HeadingBuy');
      }, 3000);
    }

    this._userService.isAuthenticated() && Observable.combineLatest(
      this._apiService.getTFAInfo(),
      this._apiService.getProfile(),
      this._apiService.getTradingStatus()
    )
      .subscribe((res) => {
        this.tfaInfo = res[0].data;
        this.user = res[1].data;
        this.tradingStatus = res[2].data.trading;

        !this.user.verifiedL0 && this.router.navigate(['/buy']);
        this._cdRef.markForCheck();
      });

    this._userService.currentLocale.takeUntil(this.destroy$).subscribe(currentLocale => {
      this.locale = currentLocale;
    });

    this._goldrateService.getObservableRate().takeUntil(this.destroy$).subscribe(data => {
      data && (this.goldRate = data.gold);
      this._cdRef.markForCheck();
    });

    this._ethService.getObservableEthBalance().takeUntil(this.destroy$).subscribe(balance => {
     if (balance !== null && (this.ethBalance === null || !this.ethBalance.eq(balance))) {
        this.ethBalance = balance;
        if (this.ethBalance !== null && this.isFirstLoad) {
          this.setCoinBalance(1);
          this.isFirstLoad = false;
        }
      }
    });

    this._ethService.getObservableEthAddress().takeUntil(this.destroy$).subscribe(ethAddr => {
      this.ethAddress = ethAddr;
      if (!this.ethAddress && this.ethBalance !== null) {
        this.ethBalance = null;
        this.router.navigate(['buy']);
      }
      this._cdRef.markForCheck();
    });

    this._ethService.getObservableNetwork().takeUntil(this.destroy$).subscribe(network => {
      if (network !== null) {
        if (network != this.MMNetwork.index) {
          this._userService.invalidNetworkModal(this.MMNetwork.name);
          this.isInvalidNetwork = true;
        } else {
          this.isInvalidNetwork = false;
        }
        this._cdRef.markForCheck();
      }
    });
  }

  initInputValueChanges() {
    this.goldAmountInput.valueChanges
      .debounceTime(500)
      .distinctUntilChanged()
      .takeUntil(this.destroy$)
      .subscribe(value => {
        if (value && !this.isReversed) {
          this.onAmountChanged(this.currentValue);
          this._cdRef.markForCheck();
        }
      });

    this.coinAmountInput.valueChanges
      .debounceTime(500)
      .distinctUntilChanged()
      .takeUntil(this.destroy$)
      .subscribe(value => {
        if (value && this.isReversed) {
          this.onAmountChanged(this.currentValue);
          this._cdRef.markForCheck();
        }
      });
  }

  iniTransactionHashModal() {
    this._ethService.getSuccessBuyRequestLink$.takeUntil(this.destroy$).subscribe(hash => {
      if (hash) {
        this.hideCryptoCurrencyForm(true);
        this._translate.get('PAGES.Buy.CtyptoCurrency.SuccessModal').subscribe(phrases => {
          this._messageBox.alert(`
            <div class="text-center">
              <div class="font-weight-500 mb-2">${phrases.Heading}</div>
              <div class="color-red">${phrases.Steps}</div>
              <div>${phrases.Hash}</div>
              <div class="mb-2 buy-hash">${hash}</div>
              <a href="${this.etherscanUrl}${hash}" target="_blank">${phrases.Link}</a>
            </div>
          `).subscribe(ok => {
            ok && this.router.navigate(['/finance/history']);
          });
        });
      }
    });
  }

  chooseCurrentCoin(coin) {
    if (this.currentCoin !== coin) {
      this.currentCoin = coin;
    }
  }

  onAmountChanged(value: number) {
    this.loading = true;

    if (!this.isReversed) {
      if (value > 0 && value <= +this.ethBalance) {

        const wei = this.Web3.toWei(value);
        this.estimatedAmount = new BigNumber(value).decimalPlaces(6, BigNumber.ROUND_DOWN);

        this._apiService.goldBuyEstimate(this.currentCoin, wei, false, this.promoCode)
          .finally(() => {
            this.loading = false;
            this._cdRef.markForCheck();
          }).subscribe(data => {
            this.goldAmount = +this.substrValue(data.data.amount / Math.pow(10, 18));
            this.goldAmountToUSD = this.goldAmount * this.goldRate;
            this.checkDiscount(data.data.discount);
            this.invalidBalance = this.isTradingError = this.isTradingLimit = this.processing = this.isInvalidPromoCode = false;
        }, error => {
            this.setPromoCodeError(error.error.errorCode);
        });
      } else {
        this.invalidBalance = true;
        this.loading = false;
        this._cdRef.markForCheck();
      }
    }
    if (this.isReversed) {
      if (value > 0) {

        const wei = this.Web3.toWei(value);
        this.estimatedAmount = new BigNumber(value).decimalPlaces(6, BigNumber.ROUND_DOWN);

        this._apiService.goldBuyEstimate(this.currentCoin, wei, true, this.promoCode)
          .finally(() => {
            this.loading = false;
            this._cdRef.markForCheck();
          }).subscribe(data => {
            this.coinAmount = +this.substrValue(data.data.amount / Math.pow(10, 18));
            this.goldAmountToUSD = this.goldAmount * this.goldRate;
            this.checkDiscount(data.data.discount);
            this.invalidBalance = (this.coinAmount > +this.ethBalance) ? true : false;
            this.isTradingError = this.isTradingLimit = this.processing = this.isInvalidPromoCode = false;
        }, error => {
            this.setPromoCodeError(error.error.errorCode);
        });
      } else {
        this.invalidBalance = true;
        this.loading = false;
        this._cdRef.markForCheck();
      }
    }
  }

  checkDiscount(dicsount: number) {
    this.discount = dicsount;
  }

  inputPromoCode(code: string) {
    this.isInvalidPromoCode = true;
    this.promoCode = this.promoCodeErrorCode = null;
    if (code === '') {
      this.isInvalidPromoCode = false;
      this.onAmountChanged(this.currentValue);
    }
    if (code.length === this.promoCodeLength) {
      this.promoCode = code;
      this.isInvalidPromoCode = false;
      this.onAmountChanged(this.currentValue);
    }
    this._cdRef.markForCheck();
  }

  setPromoCodeError(errorCode) {
    if (errorCode > 500 && errorCode < 510) {
      this.isInvalidPromoCode = true;
      this.promoCodeErrorCode = errorCode;
    }
    this._cdRef.markForCheck();
  }

  changeValue(status: boolean, event) {
    this.processing = true;

    event.target.value = this.substrValue(event.target.value);
    this.currentValue = +event.target.value;
    event.target.setSelectionRange(event.target.value.length, event.target.value.length);

    status !== this.isReversed && (this.isReversed = status);
  }

  substrValue(value: number|string) {
    return value.toString()
      .replace(',', '.')
      .replace(/([^\d.])|(^\.)/g, '')
      .replace(/^(\d{1,6})\d*(?:(\.\d{0,6})[\d.]*)?/, '$1$2')
      .replace(/^0+(\d)/, '$1');
  }

  setCoinBalance(percent) {
    this.isReversed = false;
    const value = this.substrValue(+this.ethBalance * percent);
    this.currentValue = this.coinAmount = +value;
    this._cdRef.markForCheck();
  }

  hideCryptoCurrencyForm(status) {
    this.showCryptoCurrencyBlock = !status;
    this.interval = Observable.interval(100).subscribe(() => {
      if (this.goldAmountInput) {
        this.initInputValueChanges();
        this.promoCodeModel = this.promoCode;

        this.interval && this.interval.unsubscribe();
        this._cdRef.markForCheck();
      }
    });
    this._cdRef.markForCheck();
  }

  transferTradingError(status) {
    this.isTradingError = status;
    this.showCryptoCurrencyBlock = false;
    this._cdRef.markForCheck();
  }

  onSubmit() {
    if (!this.isAuthenticated) {
      this._messageBox.authModal();
      return;
    }

    this.transferData = {
      type: 'buy',
      ethAddress: this.ethAddress,
      userId: this.user.id,
      currency: this.currentCoin,
      amount: this.estimatedAmount,
      coinAmount: this.coinAmount,
      reversed: this.isReversed,
      promoCode: this.promoCode
    };

    this.showCryptoCurrencyBlock = true;
    this._cdRef.markForCheck();
  }

  ngAfterViewInit() {
    this.initInputValueChanges();
  }

  ngOnDestroy() {
    this.destroy$.next(true)
    clearTimeout(this.timeoutPopUp);
  }

}
