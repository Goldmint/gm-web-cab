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
  public invalidBalance = false;
  private isModalShow = false;
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

  public etherscanUrl = environment.etherscanUrl;
  public sub1: Subscription;
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
          this.onAmountChanged(value);
          this._cdRef.markForCheck();
        }
      });

    this.coinAmountInput.valueChanges
      .debounceTime(500)
      .distinctUntilChanged()
      .takeUntil(this.destroy$)
      .subscribe(value => {
        if (value && this.isReversed) {
          this.onAmountChanged(value);
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
        this._cdRef.detectChanges();
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

    this._ethService.getObservableEthLimitBalance().subscribe(eth => {
      if (eth !== null && (this.ethLimit === null || !this.ethLimit.eq(eth))) {
        this.ethLimit = eth;
        if (this.isFirstLoad) {
          this.coinAmount = +this.ethLimit.decimalPlaces(6, BigNumber.ROUND_DOWN);
          this.isFirstLoad = false;
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

    this._ethService.getObservableHotGoldBalance().subscribe(data => {
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

  onReversed(status: boolean) {
    status !== this.isReversed && (this.isReversed = status);
  }

  onAmountChanged(value: number) {
    this.loading = true;
    this.currentBalance = this.selectedWallet === 0 ? +this.hotGoldBalance : +this.goldBalance;

    if (!this.isReversed) {

      if (value > this.goldLimit) {
        this.isModalShow = true;
        this.loading = false;
        return
      }

      if (value > 0 && value <= this.currentBalance) {

        const wei = new BigNumber(value).times(new BigNumber(10).pow(18).decimalPlaces(0, BigNumber.ROUND_DOWN));
        this.estimatedAmount = new BigNumber(value).decimalPlaces(6, BigNumber.ROUND_DOWN);

        this._apiService.goldSellEstimate(this.ethAddress, this.currentCoin, wei.toString(), false)
          .finally(() => {
            this.loading = false;
            this._cdRef.markForCheck();
          }).subscribe(data => {
          this.coinAmount = this.substrValue(data.data.amount / Math.pow(10, 18));
          this.coinAmountToUSD = (this.coinAmount / this.ethRate) * this.goldRate;
          this.invalidBalance = false;
        });
      } else {
        this.invalidBalance = true;
        this.loading = false;
        this._cdRef.markForCheck();
      }
    }
    if (this.isReversed) {
      if (value > +this.ethLimit) {
        this.isModalShow = true;
        this.loading = false;
        return
      }

      if (value > 0 && value <= +this.ethLimit) {
        const wei = new BigNumber(value).times(new BigNumber(10).pow(18).decimalPlaces(0, BigNumber.ROUND_DOWN));
        this.estimatedAmount = new BigNumber(value).decimalPlaces(6, BigNumber.ROUND_DOWN);

        this._apiService.goldSellEstimate(this.ethAddress, this.currentCoin, wei.toString(), true)
          .finally(() => {
            this.loading = false;
            this._cdRef.markForCheck();
          }).subscribe(data => {
            this.goldAmount = this.substrValue(data.data.amount / Math.pow(10, 18));
            this.goldLimit === null && (this.goldLimit = this.goldAmount)

            this.coinAmountToUSD = (this.coinAmount / this.ethRate) * this.goldRate;
            this.invalidBalance = (this.goldAmount > this.currentBalance) ? true : false;
        });
      } else {
        this.invalidBalance = true;
        this.loading = false;
        this._cdRef.markForCheck();
      }
    }
  }

  getGoldLimit(ethLimit: number) {
    const wei = new BigNumber(ethLimit).times(new BigNumber(10).pow(18).decimalPlaces(0, BigNumber.ROUND_DOWN));
    this._apiService.goldSellEstimate(this.ethAddress, this.currentCoin, wei.toString(), this.isReversed)
      .subscribe(data => {
        this.goldLimit = this.substrValue(data.data.amount / Math.pow(10, 18));
    });
  }

  substrValue(value: number) {
    return +value.toString().replace(/^(\d+)(?:(\.\d{1,6})\d*)?$/, '$1$2');
  }

  setGoldBalance(percent) {
    this.isReversed = false;
    const value = this.substrValue(this.currentBalance * percent);
    this.goldAmount = value;
    this._cdRef.markForCheck();
  }

  setCorrectValue() {
    this.isReversed = false;
    if (this.goldAmount === this.goldLimit) {
      this.onAmountChanged(this.goldLimit);
    } else {
      this.goldAmount = this.goldLimit;
    }

    this.isModalShow = false;
    this._cdRef.markForCheck();
  }

  closeModal() {
    this.isModalShow = false;
    this.invalidBalance = true;
    this._cdRef.markForCheck();
  }

  onSubmit() {
    this.loading = true;
    this.sub1 && this.sub1.unsubscribe();
    if (this.selectedWallet === 0) {

    } else {
      this.loading = true;
      this._apiService.goldSellAsset(this.ethAddress, this.estimatedAmount)
        .finally(() => {
          this.loading = false;
          this._cdRef.markForCheck();
        })
        .subscribe(res => {
          const wei = new BigNumber(this.estimatedAmount).times(new BigNumber(10).pow(18).decimalPlaces(0, BigNumber.ROUND_DOWN));
          this._apiService.goldSellEstimate(this.ethAddress, this.currentCoin, wei.toString(), this.isReversed)
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
                      this._ethService.sendSellRequest(this.ethAddress, this.user.id, res.data.requestId, fromAmount);

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
                          `);
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
  }

}
