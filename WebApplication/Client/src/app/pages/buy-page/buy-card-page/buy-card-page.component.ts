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
  public locale: string;
  public invalidBalance: boolean = false;
  public showPaymentCardBlock: boolean = false;

  public isReversed: boolean = false;
  public isDataLoaded: boolean = false;
  public goldAmount: number = 0;
  public usdAmount: number = 0;
  public ethAddress: string = '';
  public currentValue: number;
  public estimatedAmount: BigNumber;
  public cards: CardsList;
  public selectedCard: number;
  public transferAmount: object;

  private Web3 = new Web3();
  private destroy$: Subject<boolean> = new Subject<boolean>();
  private interval: Subscription;
  private isFormSubmitted = false;

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
    this._apiService.getFiatCards()
      .subscribe(cards => {
        this.cards = cards.data;
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
      this.ethAddress && ethAddr === null && this.router.navigate(['sell']);
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
      if (value > 1 && value.toString().length <= 15) {

        this.estimatedAmount = new BigNumber(value).decimalPlaces(6, BigNumber.ROUND_DOWN);
        const usd = (value * 100).toFixed();

        this._apiService.goldBuyEstimate('USD', usd, false)
          .finally(() => {
            this.loading = false;
            this._cdRef.markForCheck();
          }).subscribe(data => {
          this.goldAmount = +this.substrValue(data.data.amount / Math.pow(10, 18));

          this.fixedAmount();
          this.invalidBalance = false;
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

        this._apiService.goldBuyEstimate('USD', wei, true)
          .finally(() => {
            this.loading = false;
            this._cdRef.markForCheck();
          }).subscribe(data => {
          this.usdAmount = data.data.amount;

          this.fixedAmount();
          this.invalidBalance = (this.usdAmount <= 1) ? true : false;
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

  changeCard() {

  }

  changeValue(status: boolean, event) {
    event.target.value = this.substrValue(event.target.value);
    this.currentValue = +event.target.value;
    event.target.setSelectionRange(event.target.value.length, event.target.value.length);

    if (status !== this.isReversed) {
      this.isReversed = status;
      this.invalidBalance = false;
      this.loading = true;
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

  onSubmit() {
    this.isFormSubmitted = true;
    this.isReversed ? this.onAmountChanged(this.goldAmount) : this.onAmountChanged(this.usdAmount);
  }

  fixedAmount() {
    if (this.isFormSubmitted) {
      this.transferAmount = {
        amount: this.usdAmount + ' USD',
        estimated: this.goldAmount + ' GOLD'
      };

      this.isFormSubmitted = false;
      this.showPaymentCardBlock = true;
      this._cdRef.markForCheck();
    }
  }

  ngOnDestroy() {
    this.destroy$.next(true);
  }

}
