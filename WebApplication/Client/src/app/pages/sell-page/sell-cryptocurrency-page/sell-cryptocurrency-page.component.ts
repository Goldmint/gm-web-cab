import {
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

@Component({
  selector: 'app-sell-cryptocurrency-page',
  templateUrl: './sell-cryptocurrency-page.component.html',
  styleUrls: ['./sell-cryptocurrency-page.component.sass'],
  encapsulation: ViewEncapsulation.None
})
export class SellCryptocurrencyPageComponent implements OnInit, OnDestroy {
  @HostBinding('class') class = 'page';

  @ViewChild('sellForm') sellForm;
  @ViewChild('goldAmountInput') goldAmountInput;
  @ViewChild('coinAmountInput') coinAmountInput;

  public loading = false;
  public isFirstLoad = true;
  public isFirstTransaction = true;
  public invalidBalance = false;
  public isModalShow = false;
  public locale: string;

  public user: User;
  public tfaInfo: TFAInfo;

  public goldBalance: BigNumber = null;
  public goldRate: number = 0;
  public ethRate: number = 0;
  public hotGoldBalance: BigNumber = null;
  public mntpBalance: BigNumber = null;
  public ethAddress: string = '';
  public ethLimit: BigNumber = null;
  public goldLimit: number | null = null;
  public selectedWallet = 0;

  public coinList = ['BTC', 'ETH'];
  public currentCoin = this.coinList[1];
  public isReversed: boolean = true;
  public goldAmount: number = 0;
  public coinAmount: number = 0;
  public currentBalance: number;
  public coinAmountToUSD: number = 0;
  public estimatedAmount: BigNumber;
  public currentValue: number;
  private Web3 = new Web3();

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

    Observable.combineLatest(
      this._apiService.getTFAInfo(),
      this._userService.currentUser
    )
      .subscribe((res) => {
        this.tfaInfo = res[0].data;
        this.user = res[1];
        this.loading = false;
        this._cdRef.markForCheck();
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
          this.selectedWallet = this._userService.currentWallet.id === 'hot' ? 0 : 1;
        }
    });

    this._ethService.getObservableEthLimitBalance().takeUntil(this.destroy$).subscribe(eth => {
      if (eth !== null && (this.ethLimit === null || !this.ethLimit.eq(eth))) {
        this.ethLimit = eth;
        if (this.isFirstLoad) {
          this.calculateStartGoldValue(+this.ethLimit.decimalPlaces(6, BigNumber.ROUND_DOWN));
          this._cdRef.markForCheck();
        } else {
          this.getGoldLimit(+this.ethLimit.decimalPlaces(6, BigNumber.ROUND_DOWN));
        }
      }
    })

    this._userService.currentLocale.takeUntil(this.destroy$).subscribe(currentLocale => {
      this.locale = currentLocale;
    });

    this._goldrateService.getObservableRate().takeUntil(this.destroy$).subscribe(data => {
      data && (this.goldRate = data.gold) && (this.ethRate = data.eth);
      this._cdRef.markForCheck();
    });

    this._ethService.getObservableHotGoldBalance().takeUntil(this.destroy$).subscribe(data => {
      if (data !== null && (this.hotGoldBalance === null || !this.hotGoldBalance.eq(data))) {
        this.hotGoldBalance = data;
      }
    })

    this._ethService.getObservableEthAddress().takeUntil(this.destroy$).subscribe(ethAddr => {
      this.ethAddress = ethAddr;
      if (!this.ethAddress && this.goldBalance !== null && this.hotGoldBalance !== null) {
        this.selectedWallet = 0;
        this.router.navigate(['sell']);
      }
    });

    this._userService.onWalletSwitch$.takeUntil(this.destroy$).subscribe((wallet) => {
      this.selectedWallet = wallet['id'] === 'hot' ? 0 : 1;
      this.setGoldBalance(1);
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
      if (value > this.goldLimit && +this.ethLimit !== 0 && this.currentBalance) {
        this.isModalShow = true;
        this.loading = false;
        return
      }

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
          this.invalidBalance = false;
        }, () => {
          this.setError();
        });
      } else {
        this.setError();
      }
    }
    if (this.isReversed) {
      if (value > +this.ethLimit && +this.ethLimit !== 0 && this.currentBalance) {
        this.isModalShow = true;
        this.loading = false;
        return
      }

      if (value > 0 && value <= +this.ethLimit) {
        const wei = this.Web3.toWei(value);
        this.estimatedAmount = new BigNumber(value).decimalPlaces(6, BigNumber.ROUND_DOWN);

        this._apiService.goldSellEstimate(this.ethAddress, this.currentCoin, wei, true)
          .finally(() => {
            this.loading = false;
            this._cdRef.markForCheck();
          }).subscribe(data => {
            this.goldAmount = +this.substrValue(data.data.amount / Math.pow(10, 18));

            this.coinAmountToUSD = (this.coinAmount / this.ethRate) * this.goldRate;
            this.invalidBalance = (this.goldAmount > this.currentBalance) ? true : false;
        }, () => {
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
    event.target.setSelectionRange(event.target.value.length, event.target.value.length);

    status !== this.isReversed && (this.isReversed = status);
  }

  calculateStartGoldValue(value: number) {
    if (!value) {
      this.isFirstLoad = false;
      this.setError();
      return
    }

    this.loading = true;
    const wei = this.Web3.toWei(value);
    this._apiService.goldSellEstimate(this.ethAddress, this.currentCoin, wei, this.isReversed)
      .subscribe(data => {
        this.isReversed = false;
        this.goldLimit = +this.substrValue(data.data.amount / Math.pow(10, 18));
        if (this.selectedWallet === 0) {
          this.currentBalance = +this.hotGoldBalance.decimalPlaces(6, BigNumber.ROUND_DOWN);
        } else {
          this.currentBalance = +this.goldBalance.decimalPlaces(6, BigNumber.ROUND_DOWN);
        }
        this.goldAmount = this.currentValue = +this.substrValue((this.goldLimit < this.currentBalance) ? this.goldLimit : this.currentBalance);
        this.isFirstLoad = this.loading = false;
        this._cdRef.markForCheck();
      });
  }

  getGoldLimit(ethLimit: number) {
    const wei = this.Web3.toWei(ethLimit);
    this._apiService.goldSellEstimate(this.ethAddress, this.currentCoin, wei, this.isReversed)
      .subscribe(data => {
        this.goldLimit = +this.substrValue(data.data.amount / Math.pow(10, 18));
    });
  }

  substrValue(value: number|string) {
    return value.toString()
      .replace(',', '.')
      .replace(/([^\d.])|(^\.)/g, '')
      .replace(/^(\d+)(?:(\.\d{0,6})[\d.]*)?/, '$1$2')
      .replace(/^0+(\d)/, '$1');
  }

  setGoldBalance(percent) {
    this.isReversed = false;
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

  setError() {
    this.invalidBalance = true;
    this.loading = false;
    this._cdRef.markForCheck();
  }

  onSubmit() {
    this.loading = this.isFirstTransaction = true;
    this.sub1 && this.sub1.unsubscribe();
    this.subGetGas && this.subGetGas.unsubscribe();

    if (this.selectedWallet === 0) {

    } else {
      this._apiService.goldSellAsset(this.ethAddress, this.estimatedAmount)
        .finally(() => {
          this.loading = false;
          this._cdRef.markForCheck();
        })
        .subscribe(res => {
          const wei = this.Web3.toWei(this.estimatedAmount);
          this._apiService.goldSellEstimate(this.ethAddress, this.currentCoin, wei, this.isReversed)
            .subscribe(data => {
              let estimate, amount, toAmount, fromAmount;
              fromAmount = estimate = this.estimatedAmount;
              toAmount = amount = (data.data.amount / Math.pow(10, 18)).toFixed(6);
              this.isReversed && (fromAmount = amount) && (toAmount = estimate);

              this._translate.get('MessageBox.EthWithdraw',
                {coinAmount: fromAmount, goldAmount: toAmount, ethRate: res.data.ethRate}
              ).subscribe(phrase => {
                this._messageBox.confirm(phrase).subscribe(ok => {
                  if (ok) {
                    this._apiService.goldSellConfirm(res.data.requestId).subscribe(() => {

                      this.subGetGas = this._ethService.getObservableGasPrice().subscribe((price) => {
                        if (price !== null && this.isFirstTransaction) {
                          this._ethService.sendSellRequest(this.ethAddress, this.user.id, res.data.requestId, fromAmount, +price);
                          this.isFirstTransaction = false;
                        }
                      });

                      this.sub1 = this._ethService.getSuccessSellRequestLink$.subscribe(hash => {
                        if (hash) {
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

                    });
                  }
                });
              });
            });
        });
    }
  }

  ngOnDestroy() {
    this.destroy$.next(true);
    this.subGetGas && this.subGetGas.unsubscribe();
    this.sub1 && this.sub1.unsubscribe();
  }

}
