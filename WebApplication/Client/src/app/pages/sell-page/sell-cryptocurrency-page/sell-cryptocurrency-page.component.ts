import {
  AfterViewInit,
  ChangeDetectorRef,
  Component,
  HostBinding,
  OnDestroy,
  OnInit,
  ViewChild,
  ViewEncapsulation
} from '@angular/core';
import {APIService, EthereumService, GoldrateService, MessageBoxService, UserService} from "../../../services";
import {TFAInfo} from "../../../interfaces";
import {User} from "../../../interfaces/user";
import {Subject} from "rxjs/Subject";
import {BigNumber} from "bignumber.js";
import {Observable} from "rxjs/Observable";
import {TranslateService} from "@ngx-translate/core";
import {Router} from "@angular/router";
import {environment} from "../../../../environments/environment";
import {Subscription} from "rxjs/Subscription";
import * as Web3 from "web3";
import {LimitErrors} from "../../../models/limitErrors";

@Component({
  selector: 'app-sell-cryptocurrency-page',
  templateUrl: './sell-cryptocurrency-page.component.html',
  styleUrls: ['./sell-cryptocurrency-page.component.sass'],
  encapsulation: ViewEncapsulation.None
})
export class SellCryptocurrencyPageComponent implements OnInit, OnDestroy, AfterViewInit {
  @HostBinding('class') class = 'page';

  @ViewChild('sellForm') sellForm;
  @ViewChild('goldAmountInput') goldAmountInput;
  @ViewChild('coinAmountInput') coinAmountInput;

  public loading = false;
  public invalidBalance = false;
  public isModalShow = false;
  public isTradingError = false;
  public isTradingLimit: any = false;
  public showCryptoCurrencyBlock: boolean = false;
  public locale: string;

  public user: User;
  public tfaInfo: TFAInfo;

  public goldBalance: string | number = null;
  public goldRate: number = 0;
  public ethRate: number = 0;
  public ethAddress: string = '';
  public goldLimit: number | null = null;
  public limitError: LimitErrors = new LimitErrors();
  public coinList = ['BTC', 'ETH'];
  public currentCoin = this.coinList[1];
  public isReversed: boolean = false;
  public goldAmount: number = 0;
  public coinAmount: number = 0;
  public currentBalance: number;
  public coinAmountToUSD: number = 0;
  public estimatedAmount: BigNumber;
  public currentValue: number;
  public transferData: object;
  private Web3 = new Web3();

  public etherscanUrl = environment.etherscanUrl;
  public sub1: Subscription;
  public subGetGas: Subscription;
  public interval: Subscription;
  public MMNetwork = environment.MMNetwork;
  public isInvalidNetwork: boolean = true;
  public isAuthenticated: boolean = false;
  public isEthLimitError: boolean = false;
  public sumusAddress: string = '';
  public noMetamask: boolean = false;

  private destroy$: Subject<boolean> = new Subject<boolean>();
  private liteWallet = window['GoldMint'];
  private isFirstLoad: boolean = true;

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
    this._apiService.transferTradingError$.takeUntil(this.destroy$).subscribe(status => {
      this.isTradingError = !!status;
      this._cdRef.markForCheck();
    });

    this._apiService.transferTradingLimit$.takeUntil(this.destroy$).subscribe(limit => {
      this.isTradingLimit = limit;
      this.isTradingLimit['min'] = this.substrValue(limit['min'] / Math.pow(10, 18));
      this.isTradingLimit['max'] = this.substrValue(limit['max'] / Math.pow(10, 18));

      if (!this.isReversed) {
        this.coinAmount = +this.substrValue(limit['cur'] / Math.pow(10, 18));
      }
      this._cdRef.markForCheck();
    });

    this.iniTransactionHashModal();

    if (!window.hasOwnProperty('web3') && !window.hasOwnProperty('ethereum')) {
      this.noMetamask = true;
      this._cdRef.markForCheck();
    }

    Observable.combineLatest(
      this._apiService.getTFAInfo(),
      this._apiService.getProfile()
    )
      .subscribe((res) => {
        this.tfaInfo = res[0].data;
        this.user = res[1].data;

        !this.user.verifiedL1 && this.router.navigate(['/sell']);
        this.loading = false;
        this._cdRef.markForCheck();
      });

    this._ethService.getObservableSumusAccount().takeUntil(this.destroy$).subscribe(data => {
      if (data) {
        let sumusGold = +data.sumusGold > 0 ? (+data.sumusGold / Math.pow(10, 18)) : 0;
        if (sumusGold !== this.currentBalance) {
          this.currentBalance = sumusGold;
          this.goldBalance = Math.floor(sumusGold * 1000000) / 1000000;
          this.isReversed = false;
          this.goldAmount = this.goldBalance;

          if (this.isFirstLoad) {
            this.onAmountChanged(this.goldBalance);
            this.isFirstLoad = false;
          } else {
            this.currentValue = this.goldBalance;
          }

          this.sumusAddress = data.sumusWallet;
          this._cdRef.markForCheck();
        }
      }
    });

    this._userService.currentLocale.takeUntil(this.destroy$).subscribe(currentLocale => {
      this.locale = currentLocale;
    });

    this._goldrateService.getObservableRate().takeUntil(this.destroy$).subscribe(data => {
      data && (this.goldRate = data.gold) && (this.ethRate = data.eth);
      this._cdRef.markForCheck();
    });

    this._ethService.getObservableEthAddress().takeUntil(this.destroy$).subscribe(ethAddr => {
      if (!this.ethAddress && ethAddr) {
        this._messageBox.closeModal();
      }
      this.ethAddress = ethAddr;
      if (!this.ethAddress && this.goldBalance !== null) {
        // this.router.navigate(['sell']);
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

  getMetamaskModal() {
    this._userService.showGetMetamaskModal();
  }

  enableMetamaskModal() {
    this._ethService.connectToMetaMask();
    this._userService.showLoginToMMBox('HeadingSell');
  }

  openLiteWallet() {
    if (window.hasOwnProperty('GoldMint')) {
      this.liteWallet.getAccount().then(res => {
        !res.length && this._userService.showLoginToLiteWalletModal();
        res.length && this.liteWallet.openSendTokenPage(this.sumusAddress, 'GOLD').then(() => {});
      });
    } else {
      this._userService.showGetLiteWalletModal();
    }
  }

  initInputValueChanges() {
    this.goldAmountInput.valueChanges
      .debounceTime(500)
      .distinctUntilChanged()
      .takeUntil(this.destroy$)
      .subscribe(value => {
        if (!this.isReversed && this.currentValue !== undefined) {
          this.onAmountChanged(this.currentValue);
          this._cdRef.markForCheck();
        }
      });

    this.coinAmountInput.valueChanges
      .debounceTime(500)
      .distinctUntilChanged()
      .takeUntil(this.destroy$)
      .subscribe(value => {
        if (this.isReversed && this.currentValue !== undefined) {
          this.onAmountChanged(this.currentValue);
          this._cdRef.markForCheck();
        }
      });
  }

  iniTransactionHashModal() {
    this._ethService.getSuccessSellRequestLink$.takeUntil(this.destroy$).subscribe(hash => {
      if (hash) {
        this.hideCryptoCurrencyForm(true);
        this._translate.get('PAGES.Sell.CtyptoCurrency.SuccessModal').subscribe(phrases => {
          this._messageBox.alert(`
            <div class="text-center">
              <div class="font-weight-500 mb-2">${phrases.Heading}</div>
              <div class="color-red">${phrases.Steps}</div>
              <div>${phrases.Hash}</div>
              <div class="mb-2 sell-hash">${hash}</div>
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
      this.limitError = new LimitErrors();
      this.invalidBalance = false;

      if (value > 0 && value <= this.currentBalance) {
        const wei = this.Web3.toWei(value);
        this.estimatedAmount = new BigNumber(value).decimalPlaces(6, BigNumber.ROUND_DOWN);

        this._apiService.goldSellEstimate(this.ethAddress, this.currentCoin, wei, false)
          .finally(() => {
            this.loading = false;
            this._cdRef.markForCheck();
          }).subscribe(data => {
            this.coinAmount = +this.substrValue(data.data.amount / Math.pow(10, 18));
            this.coinAmountToUSD = (this.coinAmount / this.ethRate) * this.goldRate;
            this.invalidBalance = this.isTradingError = this.isTradingLimit = false;
        }, (error) => {
            error.error.errorCode === 104 ? this.setLimitError() : this.setError();
        });
      } else {
        this.setError();
      }
    }
    if (this.isReversed) {
      this.limitError = new LimitErrors();
      this.invalidBalance = false;

      if (value > 0) {
        const wei = this.Web3.toWei(value);
        this.estimatedAmount = new BigNumber(value).decimalPlaces(6, BigNumber.ROUND_DOWN);

        this._apiService.goldSellEstimate(this.ethAddress, this.currentCoin, wei, true)
          .finally(() => {
            this.loading = false;
            this._cdRef.markForCheck();
          }).subscribe(data => {
            this.goldAmount = +this.substrValue(data.data.amount / Math.pow(10, 18));
            this.isTradingError = this.isTradingLimit = false;
            this.coinAmountToUSD = (this.coinAmount / this.ethRate) * this.goldRate;
            this.invalidBalance = (this.goldAmount > this.currentBalance) ? true : false;
        }, (error) => {
            this.setError();
        });
      } else {
        this.setError();
      }
    }
  }

  changeValue(status: boolean, event) {
    event.target.value = this.substrValue(event.target.value);
    this.currentValue = +event.target.value;
    // event.target.setSelectionRange(event.target.value.length, event.target.value.length);

    status !== this.isReversed && this.ethAddress && (this.isReversed = status);
  }

  substrValue(value: number|string) {
    return value.toString()
      .replace(',', '.')
      .replace(/([^\d.])|(^\.)/g, '')
      .replace(/^(\d{1,6})\d*(?:(\.\d{0,6})[\d.]*)?/, '$1$2')
      .replace(/^0+(\d)/, '$1');
  }

  setGoldBalance(percent) {
    this.ethAddress && (this.isReversed = false);
    const value = this.substrValue(this.currentBalance * percent);
    this.currentValue = this.goldAmount = +value;
    this._cdRef.markForCheck();
  }

  setCorrectValue() {
    this.isReversed = false;

    if (this.goldAmount === this.goldLimit) {
      this.onAmountChanged(this.goldLimit);
    } else {
      this.goldAmount = this.currentValue = this.goldLimit;
    }

    this.isModalShow = false;
    this._cdRef.markForCheck();
  }

  closeModal() {
    this.isModalShow = false;
    this.invalidBalance = true;
    this._cdRef.markForCheck();
  }

  hideCryptoCurrencyForm(status) {
    this.showCryptoCurrencyBlock = !status;
    this.interval = Observable.interval(100).subscribe(() => {
      if (this.goldAmountInput) {
        this.initInputValueChanges();

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

  setError() {
    this.invalidBalance = true;
    this.loading = false;
    this._cdRef.markForCheck();
  }

  setLimitError() {
    this.limitError.min = this.coinAmount < +this.isTradingLimit['min'];
    this.limitError.max = this.coinAmount > +this.isTradingLimit['max'];
    this._cdRef.markForCheck();
  }

  openDepositModal() {
    this._translate.get('MessageBox.MakeDeposit').subscribe(phrase => {
      setTimeout(() => {
        this._messageBox.alert(`
          <div>${phrase}</div>
          <b>${this.sumusAddress}</b>
        `);
      }, 0);
    });
  }

  onSubmit() {
    this.transferData = {
      type: 'sell',
      ethAddress: this.ethAddress,
      userId: this.user.id,
      currency: this.currentCoin,
      amount: this.estimatedAmount,
      coinAmount: this.goldAmount,
      reversed: this.isReversed
    };

    this.showCryptoCurrencyBlock = true;
    this._cdRef.markForCheck();
  }

  ngAfterViewInit() {
    this.initInputValueChanges();
  }

  ngOnDestroy() {
    this.destroy$.next(true);
    this.subGetGas && this.subGetGas.unsubscribe();
    this.sub1 && this.sub1.unsubscribe();
  }

}
