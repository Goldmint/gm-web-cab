import {AfterViewInit, ChangeDetectorRef, Component, HostBinding, OnInit, ViewChild} from '@angular/core';
import {Subject} from "rxjs/Subject";
import {APIService, EthereumService, GoldrateService, MessageBoxService, UserService} from "../../../services";
import {TranslateService} from "@ngx-translate/core";
import {BigNumber} from "bignumber.js";
import * as Web3 from "web3";
import {Router} from "@angular/router";
import {CardsList} from "../../../interfaces";
import {Observable} from "rxjs/Observable";
import {Subscription} from "rxjs/Subscription";

@Component({
  selector: 'app-buy-card-page',
  templateUrl: './buy-card-page.component.html',
  styleUrls: ['./buy-card-page.component.sass']
})
export class BuyCardPageComponent implements OnInit {
  @HostBinding('class') class = 'page';

  @ViewChild('goldAmountInput') goldAmountInput;
  @ViewChild('usdAmountInput') usdAmountInput;

  public loading: boolean = false;
  public processing = false;
  public locale: string;
  public invalidBalance: boolean = false;
  public showPaymentCardBlock: boolean = false;
  public isTradingError = false;
  public isTradingLimit: object | boolean = false;
  public isBuyLimit: boolean = false;
  public buyLimit = {};

  public isReversed: boolean = false;
  public isDataLoaded: boolean = false;
  public goldAmount: number = 0;
  public usdAmount: number = 0;
  public ethAddress: string = '';
  public currentValue: number;
  public estimatedAmount: BigNumber;
  public cards: CardsList = {
    list: []
  };
  public selectedCard: number;
  public transferData: object;
  public allowValue: number;

  public promoCode: string = null;
  public discount: number = 0;
  public isInvalidPromoCode: boolean = false;
  public promoCodeErrorCode: string = null;

  private promoCodeLength: number = 11;
  private timeoutPopUp;
  private Web3 = new Web3();
  private destroy$: Subject<boolean> = new Subject<boolean>();
  private interval: Subscription;

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
      if (this.isReversed) {
        this.usdAmount = this.isTradingLimit['cur'];
      }

      this.isReversed && this.checkBuyLimit(limit['cur']);
      this._cdRef.markForCheck();
    });

    if (window.hasOwnProperty('web3')) {
      this.timeoutPopUp = setTimeout(() => {
        !this.ethAddress && this._userService.showLoginToMMBox('HeadingBuy');
      }, 3000);
    }

    Observable.combineLatest(
      this._apiService.getFiatCards(),
      this._apiService.getTradingStatus(),
      this._apiService.getProfile()
    ).subscribe(data => {
        !data[2].data.verifiedL1 && this.router.navigate(['/buy']);

        data[0].data.list.forEach(card => {
          card.status === 'verified' && this.cards.list.push(card);
        });
        this.buyLimit = data[1].data.limits.creditCardUsd.deposit;
        this.isDataLoaded = true;

        if (this.cards.list && this.cards.list.length) {
          this.interval = Observable.interval(100).subscribe(() => {
            if (this.goldAmountInput) {
              this.selectedCard = this.cards.list[0].cardId;
              this.initInputValueChanges();

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

    this._ethService.getObservableEthAddress().takeUntil(this.destroy$).subscribe(ethAddr => {
      ethAddr !== null && (this.ethAddress = ethAddr);
      this.ethAddress && ethAddr === null && this.router.navigate(['buy']);
      this._cdRef.markForCheck();
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

  onAmountChanged(value: number) {
    this.loading = true;

    if (!this.isReversed) {
      if (value > 0 && value.toString().length <= 15) {

        this.checkBuyLimit(value);
        if (this.isBuyLimit ) {
          this.loading = false;
          return;
        }

        this.estimatedAmount = new BigNumber(value).decimalPlaces(6, BigNumber.ROUND_DOWN);
        const usd = (value * 100).toFixed();

        this._apiService.goldBuyEstimate('USD', usd, false, this.promoCode)
          .finally(() => {
            this.loading = false;
            this._cdRef.markForCheck();
          }).subscribe(data => {
          this.goldAmount = +this.substrValue(data.data.amount / Math.pow(10, 18));
          this.checkDiscount(data.data.discount);
          this.invalidBalance = this.isTradingError = this.isTradingLimit = this.processing = this.isInvalidPromoCode = false;
        }, error => {
          this.setPromoCodeError(error.error.errorCode);
        });
      } else {
        this.goldAmount = 0;
        this.setError();
      }
    }
    if (this.isReversed) {
      if (value > 0 && value.toString().length <= 15) {

        const wei = this.Web3.toWei(value);
        this.estimatedAmount = new BigNumber(value).decimalPlaces(6, BigNumber.ROUND_DOWN);

        this._apiService.goldBuyEstimate('USD', wei, true, this.promoCode)
          .finally(() => {
            this.loading = false;
            this._cdRef.markForCheck();
          }).subscribe(data => {
            this.usdAmount = data.data.amount;
            this.checkDiscount(data.data.discount);
            this.isTradingError = this.isTradingLimit = this.processing = this.isInvalidPromoCode = false;
            this.invalidBalance = (this.usdAmount <= 1) ? true : false;

            this.checkBuyLimit(this.usdAmount);
        }, error => {
          this.setPromoCodeError(error.error.errorCode);
        });
      } else {
        this.setError();
      }
    }
  }

  setError() {
    this.invalidBalance = true;
    this.loading = false;
    this._cdRef.markForCheck();
  }

  checkBuyLimit(value: number) {
    this.allowValue = this.buyLimit['accountMax'] - this.buyLimit['accountUsed'];
    this.isBuyLimit = value > this.allowValue;
    this.isBuyLimit && (this.isTradingLimit = false);
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

    if (status !== this.isReversed) {
      this.isReversed = status;
      this.invalidBalance = false;
      this.currentValue && (this.loading = true);
    }
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
      type: 'buy',
      cardId: +this.selectedCard,
      ethAddress: this.ethAddress,
      currency: 'USD',
      amount: this.estimatedAmount,
      reversed: this.isReversed,
      promoCode: this.promoCode
    };

    this.showPaymentCardBlock = true;
    this._cdRef.markForCheck();
  }

  ngOnDestroy() {
    this.destroy$.next(true);
    clearTimeout(this.timeoutPopUp);
  }

}
