import {ChangeDetectorRef, Component, HostBinding, OnDestroy, OnInit, ViewChild} from '@angular/core';
import {APIService, EthereumService, MessageBoxService, UserService} from "../../../services";
import {Subject} from "rxjs/Subject";
import {BigNumber} from "bignumber.js";
import {TranslateService} from "@ngx-translate/core";
import * as Web3 from "web3";
import {Router} from "@angular/router";
import {Observable} from "rxjs/Observable";
import {CardsList, User} from "../../../interfaces";
import {Subscription} from "rxjs/Subscription";
import {environment} from "../../../../environments/environment";

@Component({
  selector: 'app-sell-card-page',
  templateUrl: './sell-card-page.component.html',
  styleUrls: ['./sell-card-page.component.sass']
})
export class SellCardPageComponent implements OnInit, OnDestroy {
  @HostBinding('class') class = 'page';

  @ViewChild('goldAmountInput') goldAmountInput;
  @ViewChild('usdAmountInput') usdAmountInput;

  public loading = false;
  public locale: string;
  public invalidBalance = false;
  public isDataLoaded: boolean = false;
  public isReversed: boolean = false;
  public showPaymentCardBlock: boolean = false;
  public isTradingError = false;
  public isTradingLimit: object | boolean = false;

  public goldAmount: number = 0;
  public usdAmount: number = 0;
  public currentValue: number;
  public estimatedAmount: BigNumber;
  public cards: CardsList;
  public user: User;
  public selectedCard: number;
  public transferData: object;

  public selectedWallet = 0;
  public ethAddress: string = '';
  public goldBalance: BigNumber = null;
  public mntpBalance: BigNumber = null;
  public etherscanUrl = environment.etherscanUrl;

  private Web3 = new Web3();
  private destroy$: Subject<boolean> = new Subject<boolean>();
  private interval: Subscription;

  constructor(
    private _userService: UserService,
    private _apiService: APIService,
    private _messageBox: MessageBoxService,
    private _ethService: EthereumService,
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
      if (!this.isReversed) {
        this.usdAmount = this.isTradingLimit['cur'];
      }
      this._cdRef.markForCheck();
    });

    this.iniTransactionHashModal();

    Observable.combineLatest(
      this._apiService.getFiatCards(),
      this._apiService.getProfile()
    ).subscribe((res) => {
      this.cards = res[0].data;
      this.user = res[1].data;

      this.isDataLoaded = true;
      if (this.cards.list && this.cards.list.length) {
        this.interval = Observable.interval(100).subscribe(() => {
          if (this.goldAmountInput) {
            this.selectedCard = this.cards.list[0].cardId;

            this.initInputValueChanges();
            // this.setGoldBalance(1);

            this.interval && this.interval.unsubscribe();
            this._cdRef.markForCheck();
          }
        });
      }
      this._cdRef.markForCheck();
    });

    this._userService.currentLocale.takeUntil(this.destroy$).subscribe(currentLocale => {
      this.locale = currentLocale;
    });

    Observable.combineLatest(
      this._ethService.getObservableGoldBalance(),
      this._ethService.getObservableMntpBalance()
    )
      .takeUntil(this.destroy$).subscribe((data) => {
      if(
        (data[0] !== null && data[1] !== null) && (
          (this.goldBalance === null || !this.goldBalance.eq(data[0]))
          ||
          (this.mntpBalance === null || !this.mntpBalance.eq(data[1]))
        )
      ) {
        this.goldBalance = data[0];
        this.mntpBalance = data[1];
      }
    });

    this._ethService.getObservableEthAddress().takeUntil(this.destroy$).subscribe(ethAddr => {
      this.ethAddress = ethAddr;
      if (!this.ethAddress && this.goldBalance !== null) {
        this.router.navigate(['sell']);
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

    this.usdAmountInput.valueChanges
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
    this._ethService.getSuccessSellRequestLink$.takeUntil(this.destroy$).subscribe(hash => {
      if (hash) {
        this.hidePaymentCardForm(true);
        this._translate.get('PAGES.Sell.CtyptoCurrency.SuccessModal').subscribe(phrases => {
          this._messageBox.alert(`
                <div class="text-center">
                  <div class="font-weight-500 mb-2">${phrases.Heading}</div>
                  <div>${phrases.Steps}</div>
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

  onAmountChanged(value: number) {
    this.loading = true;

    if (!this.isReversed) {
      if (value > 0 && value.toString().length <= 15 && +this.goldBalance) {

        const wei = this.Web3.toWei(value);
        this.estimatedAmount = new BigNumber(value).decimalPlaces(6, BigNumber.ROUND_DOWN);

        this._apiService.goldSellEstimate(this.ethAddress, 'USD', wei, false)
          .finally(() => {
            this.loading = false;
            this._cdRef.markForCheck();
          }).subscribe(data => {
          this.usdAmount = data.data.amount;

          this.invalidBalance = this.isTradingError = this.isTradingLimit = false;
        });
      } else {
        this.usdAmount = 0;
        this.setError();
      }
    }
    if (this.isReversed) {
      if (value > 1 && value.toString().length <= 15 && +this.goldBalance) {

        const usd = (value * 100).toFixed();
        this.estimatedAmount = new BigNumber(value).decimalPlaces(6, BigNumber.ROUND_DOWN);

        this._apiService.goldSellEstimate(this.ethAddress, 'USD', usd, true)
          .finally(() => {
            this.loading = false;
            this._cdRef.markForCheck();
          }).subscribe(data => {
          this.goldAmount = +this.substrValue(data.data.amount / Math.pow(10, 18));
          this.isTradingError = this.isTradingLimit = false;

          if (this.goldAmount > +this.goldBalance) {
            this.invalidBalance = true;
            this.goldAmount = 0;
          } else {
            this.invalidBalance = false;
          }
        });
      } else {
        this.goldAmount = 0;
        this.setError();
      }
    }
  }

  setError() {
    this.invalidBalance = true;
    this.loading = false;
    this._cdRef.markForCheck();
  }

  changeValue(status: boolean, event) {
    event.target.value = this.substrValue(event.target.value);
    this.currentValue = +event.target.value;
    event.target.setSelectionRange(event.target.value.length, event.target.value.length);

    if (status !== this.isReversed) {
      this.isReversed = status;
      this.invalidBalance = false;
      this.currentValue && (this.loading = true);
    }
    this._cdRef.markForCheck();
  }

  setGoldBalance(percent) {
    this.isReversed = this.invalidBalance = false;
    const value = this.substrValue(+this.goldBalance * percent);
    this.currentValue = this.goldAmount = +value;
    !this.currentValue && (this.invalidBalance = true);
    this._cdRef.markForCheck();
  }

  substrValue(value: number|string) {
    return value.toString()
      .replace(',', '.')
      .replace(/([^\d.])|(^\.)/g, '')
      .replace(/^(\d+)(?:(\.\d{0,6})[\d.]*)?/, '$1$2')
      .replace(/^0+(\d)/, '$1');
  }

  hidePaymentCardForm(status) {
    this.showPaymentCardBlock = !status;
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
    this.showPaymentCardBlock = false;
    this._cdRef.markForCheck();
  }

  onSubmit() {
    this.transferData = {
      goldAmount: this.goldAmount,
      type: 'sell',
      userId:  this.user.id,
      cardId: this.selectedCard,
      ethAddress: this.ethAddress,
      currency: 'USD',
      amount: this.estimatedAmount,
      reversed: this.isReversed
    };

    this.showPaymentCardBlock = true;
    this._cdRef.markForCheck();
  }

  ngOnDestroy() {
    this.destroy$.next(true);
  }

}
