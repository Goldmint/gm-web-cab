import {ChangeDetectorRef, Component, HostBinding, OnInit, ViewChild, ViewEncapsulation} from '@angular/core';
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


@Component({
  selector: 'app-buy-cryptocurrency-page',
  templateUrl: './buy-cryptocurrency-page.component.html',
  styleUrls: ['./buy-cryptocurrency-page.component.sass'],
  encapsulation: ViewEncapsulation.None
})
export class BuyCryptocurrencyPageComponent implements OnInit {
  @HostBinding('class') class = 'page';

  @ViewChild('goldAmountInput') goldAmountInput;
  @ViewChild('coinAmountInput') coinAmountInput;

  public loading = false;
  public isFirstLoad = true;
  public isFirstTransaction = true;
  public progress = false;
  public locale: string;

  public ethAddress: string = '';
  public selectedWallet = 0;
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
  private Web3 = new Web3();

  public ethBalance: BigNumber | null = null;
  public etherscanUrl = environment.etherscanUrl;
  public sub1: Subscription;
  public subGetGas: Subscription;

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

    Observable.combineLatest(
      this._apiService.getTFAInfo(),
      this._userService.currentUser
    )
      .subscribe((res) => {
        this.tfaInfo = res[0].data;
        this.user = res[1];
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
     if (this.ethBalance === null || !this.ethBalance.eq(balance)) {
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
        this.selectedWallet = 0;
        this.router.navigate(['buy']);
      }
    });

    this.selectedWallet = this._userService.currentWallet.id === 'hot' ? 0 : 1;

    this._userService.onWalletSwitch$.takeUntil(this.destroy$).subscribe((wallet) => {
      if (wallet['id'] === 'hot') {
        this.selectedWallet = 0;
        this.router.navigate(['buy']);
      } else {
        this.selectedWallet = 1;
        this.setCoinBalance(1);
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

        this._apiService.goldBuyEstimate(this.currentCoin, wei, false)
          .finally(() => {
            this.loading = false;
            this._cdRef.markForCheck();
          }).subscribe(data => {
            this.goldAmount = +this.substrValue(data.data.amount / Math.pow(10, 18));
            this.goldAmountToUSD = this.goldAmount * this.goldRate;
            this.invalidBalance = false;
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

        this._apiService.goldBuyEstimate(this.currentCoin, wei, true)
          .finally(() => {
            this.loading = false;
            this._cdRef.markForCheck();
          }).subscribe(data => {
            this.coinAmount = +this.substrValue(data.data.amount / Math.pow(10, 18));
            this.goldAmountToUSD = this.goldAmount * this.goldRate;
            this.invalidBalance = (this.coinAmount > +this.ethBalance) ? true : false;
        });
      } else {
        this.invalidBalance = true;
        this.loading = false;
        this._cdRef.markForCheck();
      }
    }
  }

  changeValue(status: boolean, event) {
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

  onSubmit() {
    this.loading = this.isFirstTransaction = true;
    this.sub1 && this.sub1.unsubscribe();
    this.subGetGas && this.subGetGas.unsubscribe();
    let eth;

    this._apiService.goldBuyAsset(this.ethAddress, this.Web3.toWei(+this.estimatedAmount), this.isReversed, this.currentCoin)
      .subscribe(res => {
        let estimate, amount, toAmount, fromAmount;
        fromAmount = estimate = this.estimatedAmount;
        toAmount = amount = this.substrValue( (res.data.estimation.amount / Math.pow(10, 18)) );

        if (this.isReversed) {
          fromAmount = amount;
          toAmount = estimate;
          eth = res.data.estimation.amount;
        } else {
          eth = this.Web3.toWei(this.coinAmount);
        }

        this._translate.get('MessageBox.EthDeposit',
          {coinAmount: fromAmount, goldAmount: toAmount, ethRate: res.data.ethRate}
        ).subscribe(phrase => {
          this.loading = false;
          this._cdRef.markForCheck();
          this._messageBox.confirm(phrase).subscribe(ok => {
            if (ok) {
              this._apiService.goldBuyConfirm(res.data.requestId).subscribe(() => {

                this.subGetGas = this._ethService.getObservableGasPrice().subscribe((price) => {
                  if (price !== null && this.isFirstTransaction) {
                    this._ethService.sendBuyRequest(this.ethAddress, this.user.id, res.data.requestId, eth, +price);
                    this.isFirstTransaction = false;
                  }
                });

                this.sub1 = this._ethService.getSuccessBuyRequestLink$.subscribe(hash => {
                  if (hash) {
                    this._translate.get('PAGES.Buy.CtyptoCurrency.SuccessModal').subscribe(phrases => {
                      this._messageBox.alert(`
                            <div class="text-center">
                              <div class="font-weight-500 mb-2">${phrases.Heading}</div>
                              <div>${phrases.Steps}</div>
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

              });
            }
          });
        });
      });
  }

  ngOnDestroy() {
    this.destroy$.next(true);
    this.sub1 && this.sub1.unsubscribe();
    this.subGetGas && this.subGetGas.unsubscribe();
  }

}
